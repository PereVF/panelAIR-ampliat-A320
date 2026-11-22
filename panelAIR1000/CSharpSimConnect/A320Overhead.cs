using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace panelAIR1000
{
    // ═════════════════════════════════════════════════════════════════════════════════════════
    //  OVERHEAD A320 — dibuixat 100% vectorial (sense cap PNG).
    //
    //  Coordenades lògiques: 1250 x 1275 (proporció de la imatge de referència). Tot s'escala a
    //  l'amplada real del control. Estratègia per no gastar recursos:
    //   • Tot el panell es pinta UN cop en un Bitmap (mida = amplada pantalla x alçada escalada).
    //   • En tocar un element només es repinta el seu rectangle (base + serigrafia + elements que
    //     hi intersequen, filtrats per bounds) i s'invalida només aquella zona de la pantalla.
    //   • El desplaçament amunt/avall només copia píxels del Bitmap (sense escalat ni vectors).
    //   • El Timer d'animació només corre mentre hi ha alguna cosa animant-se; després s'atura.
    //   • En amagar el control (sortir del mode A320) s'allibera el Bitmap.
    // ═════════════════════════════════════════════════════════════════════════════════════════

    internal static class Oh
    {
        // To gris blavós mat (més proper a la fotografia de referència)
        // To de la planxa: barreja entre la paleta grisa (0) i la blava (1)
        public static float Tint = 0.30f;   // valor triat per en Pere (2026-11-20)
        public static Color T(int r0, int g0, int b0, int r1, int g1, int b1) => Color.FromArgb(
            Math.Clamp((int)Math.Round(r0 + (r1 - r0) * Tint), 0, 255),
            Math.Clamp((int)Math.Round(g0 + (g1 - g0) * Tint), 0, 255),
            Math.Clamp((int)Math.Round(b0 + (b1 - b0) * Tint), 0, 255));
        public static Color Frame   => T(46, 55, 64, 42, 58, 74);
        public static Color PlateA  => T(102, 115, 129, 104, 134, 158);
        public static Color PlateB  => T(86, 99, 113, 86, 115, 139);
        public static Color PlateHi => T(144, 156, 168, 150, 176, 196);
        public static Color PlateLo => T(36, 44, 52, 32, 46, 60);

        /// <summary>
        /// Fixació Dzus (quart de volta) del perímetre de cada panell: forat enfonsat a la planxa, anell
        /// metàl·lic, cúpula amb reflex i ranura ampla de tornavís pla.
        /// </summary>
        public static void Dzus(Graphics g, float cx, float cy, float r, float angleDeg)
        {
            var outer = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            // el forat a la planxa: vora fosca a dalt-esquerra, llum a baix-dreta
            var recess = RectangleF.Inflate(outer, 1.2f, 1.2f);
            using (var rb = new LinearGradientBrush(recess, Color.FromArgb(22, 26, 30), Color.FromArgb(150, 162, 174), 50f)) g.FillEllipse(rb, recess);
            // anell
            using (var lg = new LinearGradientBrush(outer, Color.FromArgb(70, 76, 82), Color.FromArgb(150, 156, 162), 50f)) g.FillEllipse(lg, outer);
            // cúpula
            var dome = RectangleF.Inflate(outer, -r * 0.16f, -r * 0.16f);
            SoftShadowEllipse(g, dome, 0.5f, 0.8f, 0.9f, 110);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(dome);
                using var pg = new PathGradientBrush(path)
                {
                    CenterColor = Color.FromArgb(214, 218, 222),
                    SurroundColors = new[] { Color.FromArgb(78, 84, 90) },
                    CenterPoint = new PointF(dome.X + dome.Width * 0.36f, dome.Y + dome.Height * 0.32f)
                };
                g.FillPath(pg, path);
            }
            using (var pe = new Pen(Color.FromArgb(40, 44, 48), 0.6f)) g.DrawEllipse(pe, dome);
            // ranura
            var st = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(angleDeg);
            using (var sb = new SolidBrush(Color.FromArgb(24, 26, 30))) g.FillRectangle(sb, -r * 0.74f, -r * 0.15f, r * 1.48f, r * 0.3f);
            using (var hp = new Pen(Color.FromArgb(150, 255, 255, 255), 0.45f)) g.DrawLine(hp, -r * 0.7f, r * 0.19f, r * 0.7f, r * 0.19f);
            using (var dp = new Pen(Color.FromArgb(120, 0, 0, 0), 0.45f)) g.DrawLine(dp, -r * 0.7f, -r * 0.19f, r * 0.7f, -r * 0.19f);
            g.Restore(st);
        }

        /// <summary>Escala lògic→píxel del dibuix actual (perquè el gra de pintura sigui d'1 píxel real).</summary>
        public static float PixelScale = 1f;

        /// <summary>Mida mínima de qualsevol serigrafia (no llegendes de polsadors): 25 px a 4K al 100% (25 / 3,072).</summary>
        public const float MinSilk = 8.15f;
        static TextureBrush? _grainFine, _grainCoarse;

        /// <summary>Soroll procedural (sense PNG): píxels clars/foscos aleatoris de baixa opacitat, llavor fixa.</summary>
        static Bitmap MakeNoise(int size, int seed, int amp)
        {
            var rnd = new Random(seed);
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    double v = rnd.NextDouble() * 2 - 1;
                    bmp.SetPixel(x, y, v >= 0
                        ? Color.FromArgb((int)(v * amp), 255, 255, 255)
                        : Color.FromArgb((int)(-v * amp * 1.3), 0, 0, 0));
                }
            return bmp;
        }

        /// <summary>Gra de pintura mat + taques suaus (desgast) dins el camí donat.</summary>
        public static void Grain(Graphics g, GraphicsPath area)
        {
            if (_grainFine == null || _grainCoarse == null) RebuildGrain();
            if (Texture < 0.98f)
            {
                _grainCoarse!.ResetTransform(); _grainCoarse.ScaleTransform(7f / PixelScale, 7f / PixelScale);
                g.FillPath(_grainCoarse, area);
            }
            _grainFine!.ResetTransform(); _grainFine.ScaleTransform(1f / PixelScale, 1f / PixelScale);
            g.FillPath(_grainFine, area);
        }

        /// <summary>
        /// Homogeneïtat de la textura de la planxa (provisional): 0 = granelluda (taques grosses + gra),
        /// 1 = molt fina (sense taques i gra gairebé imperceptible).
        /// </summary>
        public static float Texture = 0.75f;   // valor triat per en Pere (2026-11-20)

        /// <summary>Regenera els patrons de soroll segons Texture (cridar només des del fil de la UI).</summary>
        public static void RebuildGrain()
        {
            _grainFine?.Dispose(); _grainCoarse?.Dispose();
            float t = Math.Clamp(Texture, 0f, 1f);
            _grainFine = new TextureBrush(MakeNoise(128, 7, Math.Max(1, (int)Math.Round(20 * (1 - 0.75f * t)))), WrapMode.Tile);
            _grainCoarse = new TextureBrush(MakeNoise(64, 11, Math.Max(1, (int)Math.Round(16 * (1 - t)))), WrapMode.Tile);
        }

        /// <summary>Ombra difusa: el camí repetit amb traços cada cop més estrets i opacs (vora esvaïda).</summary>
        public static void SoftShadow(Graphics g, GraphicsPath path, float dx, float dy, float blur, int alpha)
        {
            var st = g.Save();
            g.TranslateTransform(dx, dy);
            const int n = 6;
            for (int i = n; i >= 1; i--)
            {
                using var p = new Pen(Color.FromArgb(Math.Max(1, alpha / n), 0, 0, 0), blur * 2f * i / n) { LineJoin = LineJoin.Round };
                g.DrawPath(p, path);
            }
            using (var b = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0))) g.FillPath(b, path);
            g.Restore(st);
        }

        public static void SoftShadowEllipse(Graphics g, RectangleF r, float dx, float dy, float blur, int alpha)
        {
            using var p = new GraphicsPath();
            p.AddEllipse(r);
            SoftShadow(g, p, dx, dy, blur, alpha);
        }

        /// <summary>Esfera/pom amb reflex especular (llum des de dalt a l'esquerra).</summary>
        public static void Specular(Graphics g, RectangleF r, Color baseLight, Color baseDark)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(r);
            using (var pg = new PathGradientBrush(path)
            {
                CenterColor = baseLight,
                SurroundColors = new[] { baseDark },
                CenterPoint = new PointF(r.X + r.Width * 0.38f, r.Y + r.Height * 0.32f)
            }) g.FillPath(pg, path);
            using (var hb = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                g.FillEllipse(hb, r.X + r.Width * 0.26f, r.Y + r.Height * 0.18f, r.Width * 0.24f, r.Height * 0.2f);
            using var pe = new Pen(Color.FromArgb(150, 20, 18, 16), 0.6f);
            g.DrawEllipse(pe, r);
        }
        public static readonly Color Silk    = Color.FromArgb(244, 243, 236);
        public static readonly Color SilkGreen = Color.FromArgb(118, 226, 146);
        public static readonly Color Amber   = Color.FromArgb(255, 158, 28);
        public static readonly Color White   = Color.FromArgb(255, 255, 255);
        public static readonly Color Blue    = Color.FromArgb(70, 196, 255);
        public static readonly Color Green   = Color.FromArgb(60, 240, 120);
        public static readonly Color Red     = Color.FromArgb(255, 48, 28);
        public static readonly Color Unlit   = Color.FromArgb(74, 78, 83);
        public static readonly Color SegAmber = Color.FromArgb(255, 150, 20);
        public static readonly Color GuardRed  = Color.FromArgb(226, 62, 40);
        public static readonly Color GuardGray = Color.FromArgb(150, 156, 162);

        static readonly Dictionary<int, Font> _fonts = new();
        public static Font F(float size, bool bold = true)
        {
            int key = (int)(size * 10) * 2 + (bold ? 1 : 0);
            if (!_fonts.TryGetValue(key, out var f))
            {
                f = new Font("Arial", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
                _fonts[key] = f;
            }
            return f;
        }

        public static readonly StringFormat Center = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
        };

        /// <summary>Compressió horitzontal de tota la tipografia (imita la lletra estreta d'Airbus).</summary>
        public const float Condense = 0.84f;

        public static void Text(Graphics g, string s, float cx, float cy, float size, Color c, bool bold = true)
        {
            using var b = new SolidBrush(c);
            var st = g.Save();
            g.TranslateTransform(cx, cy);
            g.ScaleTransform(Condense, 1f);
            // gruix de pintura: ombra mínima avall-dreta sota la lletra
            using (var sh = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.DrawString(s, F(size, bold), sh, 0.35f, 0.45f, Center);
            g.DrawString(s, F(size, bold), b, 0, 0, Center);
            g.Restore(st);
        }

        public static float TextW(string s, float size) => s.Length * size * 0.62f * Condense;

        public static Color Lerp(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        public static Color Shade(Color c, float f) =>
            Color.FromArgb(c.A, Math.Clamp((int)(c.R * f), 0, 255), Math.Clamp((int)(c.G * f), 0, 255), Math.Clamp((int)(c.B * f), 0, 255));

        public static GraphicsPath RoundRect(RectangleF r, float rad)
        {
            var p = new GraphicsPath();
            float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            if (d <= 0.2f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Screw(Graphics g, float cx, float cy, float r, float angleDeg)
        {
            var rect = new RectangleF(cx - r, cy - r, 2 * r, 2 * r);
            SoftShadowEllipse(g, rect, 0.7f, 1.1f, 1.6f, 110);
            using (var lg = new LinearGradientBrush(rect, Color.FromArgb(238, 242, 246), Color.FromArgb(112, 120, 128), 45f))
                g.FillEllipse(lg, rect);
            using (var p = new Pen(Color.FromArgb(70, 78, 86), 0.7f)) g.DrawEllipse(p, rect);
            var st = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(angleDeg);
            using (var b = new SolidBrush(Color.FromArgb(52, 58, 64)))
                g.FillRectangle(b, -r * 0.9f, -r * 0.17f, r * 1.8f, r * 0.34f);
            using (var p = new Pen(Color.FromArgb(170, 255, 255, 255), 0.45f))
                g.DrawLine(p, -r * 0.86f, r * 0.21f, r * 0.86f, r * 0.21f);
            g.Restore(st);
        }

        /// <summary>
        /// Botó/tecla del panell principal (model validat a la pàgina de botons): marc exterior tirant a negre amb fil
        /// de llum, cara fosca enfonsada (ombra a dalt, fil clar a baix). Retorna el rectangle de la cara.
        /// </summary>
        public static RectangleF MipFrame(Graphics g, RectangleF r)
        {
            const float U = 0.625f;
            float r0 = Math.Min(3.5f * U, r.Height * 0.2f);
            using (var sp = RoundRect(r, r0)) SoftShadow(g, sp, 1.2f * U, 2.2f * U, 2.2f * U, 140);
            using (var bp = RoundRect(r, r0))
            using (var bb = new LinearGradientBrush(RectangleF.Inflate(r, 0, 0.5f), Color.FromArgb(61, 65, 71), Color.FromArgb(20, 22, 25), 90f)) g.FillPath(bb, bp);
            using (var ip = RoundRect(RectangleF.Inflate(r, -0.25f, -0.25f), r0 * 0.9f))
            using (var pe = new Pen(Color.FromArgb(115, 150, 160, 170), 0.4f)) g.DrawPath(pe, ip);
            float rim = Math.Min(2.4f * U, Math.Min(r.Width, r.Height) * 0.14f);
            var f = RectangleF.Inflate(r, -rim, -rim);
            float r1 = Math.Min(2.2f * U, f.Height * 0.2f);
            using (var fp = RoundRect(f, r1))
            {
                using (var fb = new LinearGradientBrush(RectangleF.Inflate(f, 0, 0.5f), Color.FromArgb(62, 66, 72), Color.FromArgb(34, 37, 41), 90f)) g.FillPath(fb, fp);
                var st = g.Save();
                g.SetClip(fp, CombineMode.Intersect);
                float sh = Math.Min(3f * U, f.Height * 0.2f);
                using (var tb = new LinearGradientBrush(new RectangleF(f.X, f.Y - 0.1f, f.Width, sh + 0.2f), Color.FromArgb(153, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 90f))
                    g.FillRectangle(tb, f.X, f.Y, f.Width, sh);
                g.Restore(st);
            }
            return f;
        }

        /// <summary>Arrodoneix el vèrtex i d'una polilínia amb un arc de radi r (mostrejat amb 32 punts).</summary>
        public static PointF[] RoundAt(PointF[] pts, int i, float r)
        {
            if (r <= 0.3f || i <= 0 || i >= pts.Length - 1) return pts;
            var p = pts[i]; var a = pts[i - 1]; var b = pts[i + 1];
            float ux = a.X - p.X, uy = a.Y - p.Y, vx = b.X - p.X, vy = b.Y - p.Y;
            float lu = MathF.Sqrt(ux * ux + uy * uy), lv = MathF.Sqrt(vx * vx + vy * vy);
            ux /= lu; uy /= lu; vx /= lv; vy /= lv;
            float theta = MathF.Acos(Math.Clamp(ux * vx + uy * vy, -1f, 1f));
            float d = Math.Min(r / MathF.Tan(theta / 2), Math.Min(lu, lv) * 0.9f);
            float rr = d * MathF.Tan(theta / 2);
            var A = new PointF(p.X + ux * d, p.Y + uy * d); var B = new PointF(p.X + vx * d, p.Y + vy * d);
            float bx = ux + vx, by = uy + vy, lb = MathF.Sqrt(bx * bx + by * by); bx /= lb; by /= lb;
            float cd = rr / MathF.Sin(theta / 2);
            var C = new PointF(p.X + bx * cd, p.Y + by * cd);
            double a0 = Math.Atan2(A.Y - C.Y, A.X - C.X), a1 = Math.Atan2(B.Y - C.Y, B.X - C.X), da = a1 - a0;
            while (da > Math.PI) da -= 2 * Math.PI; while (da < -Math.PI) da += 2 * Math.PI;
            var arc = new List<PointF>();
            for (int k = 0; k <= 32; k++) { double t = a0 + da * k / 32; arc.Add(new PointF(C.X + (float)Math.Cos(t) * rr, C.Y + (float)Math.Sin(t) * rr)); }
            return pts.Take(i).Concat(arc).Concat(pts.Skip(i + 1)).ToArray();
        }

        public static bool Boxed(string s) => s is "OFF" or "ON" or "MAN" or "ALTN" or "OVRD" or "ALIGN";

        public static Color LegendColor(string s) => s switch
        {
            "FAULT" or "SMOKE" or "DISCH" or "CALL" => Amber,
            "AVAIL" or "OPEN" => Green,
            "ON" => Blue,
            "EVAC" => Red,
            _ => White
        };

        /// <summary>Llegenda d'un polsador (meitat superior o inferior). level 0 = apagada, 1 = plena.</summary>
        public static void Legend(Graphics g, RectangleF area, string text, Color lit, float level, bool boxed)
        {
            if (string.IsNullOrEmpty(text)) return;
            float cx = area.X + area.Width / 2f, cy = area.Y + area.Height / 2f;
            if (text == "••")
            {
                using var db = new SolidBrush(Lerp(Color.FromArgb(96, 100, 104), lit, level));
                float dr = Math.Max(1.2f, area.Height * 0.11f);
                g.FillEllipse(db, cx - 5.5f - dr, cy - dr, dr * 2, dr * 2);
                g.FillEllipse(db, cx + 5.5f - dr, cy - dr, dr * 2, dr * 2);
                return;
            }
            float size = Math.Min(area.Height * 0.55f, (area.Width - 5f) / (text.Length * 0.66f + 0.2f));
            size = Math.Min(size, 8.6f);
            if (level > 0.02f)
            {
                using var gb = new SolidBrush(Color.FromArgb((int)(48 * level), lit));
                g.FillRectangle(gb, RectangleF.Inflate(area, -1.5f, -1.2f));
            }
            var c = Lerp(Unlit, lit, level);
            Text(g, text, cx, cy + 0.3f, size, c);
            if (boxed)
            {
                float w = TextW(text, size) + 4f, h = size * 1.3f;
                using var p = new Pen(c, Math.Max(0.8f, size * 0.11f));
                g.DrawRectangle(p, cx - w / 2f, cy - h / 2f + 0.3f, w, h);
            }
        }
    }

    // ═══════════════════════ BASE: planxa, serigrafia (no interactiva) ═══════════════════════

    internal abstract class OhSilk
    {
        public RectangleF Bounds;
        public abstract void Draw(Graphics g);
    }

    internal sealed class OhPanel : OhSilk
    {
        readonly PointF[]? _poly;
        readonly RectangleF _rect;
        readonly PointF[] _screws;

        public OhPanel(RectangleF r)
        {
            _rect = r; Bounds = RectangleF.Inflate(r, 3, 3);
            // Dzus a les vores laterals, una mica per sota/sobre de les cantonades (com a l'avió real)
            const float ix = 8f, iy = 20f;
            _screws = new[] { new PointF(r.X + ix, r.Y + iy), new PointF(r.Right - ix, r.Y + iy),
                              new PointF(r.X + ix, r.Bottom - iy), new PointF(r.Right - ix, r.Bottom - iy) };
        }

        public OhPanel(PointF[] poly, PointF[] screws)
        {
            _poly = poly; _screws = screws;
            float l = poly.Min(p => p.X), t = poly.Min(p => p.Y), r = poly.Max(p => p.X), b = poly.Max(p => p.Y);
            _rect = RectangleF.FromLTRB(l, t, r, b);
            Bounds = RectangleF.Inflate(_rect, 3, 3);
        }

        GraphicsPath MakePath()
        {
            if (_poly == null) return Oh.RoundRect(_rect, 5f);
            var p = new GraphicsPath(); p.AddPolygon(_poly); return p;
        }

        public override void Draw(Graphics g)
        {
            using var path = MakePath();
            // relleu: ombra difosa avall-dreta, llum amunt-esquerra, i la planxa a sobre
            Oh.SoftShadow(g, path, 1.8f, 2.6f, 4f, 140);
            var st = g.Save();
            g.TranslateTransform(1.0f, 1.0f);
            using (var lo = new Pen(Oh.PlateLo, 1.4f)) g.DrawPath(lo, path);
            g.Restore(st); st = g.Save();
            g.TranslateTransform(-0.8f, -0.8f);
            using (var hi = new Pen(Oh.PlateHi, 1.4f)) g.DrawPath(hi, path);
            g.Restore(st);
            using (var fill = new LinearGradientBrush(new RectangleF(_rect.X, _rect.Y - 1, _rect.Width, _rect.Height + 2), Oh.PlateA, Oh.PlateB, 90f))
                g.FillPath(fill, path);
            Oh.Grain(g, path);
            foreach (var s in _screws)
                Oh.Dzus(g, s.X, s.Y, 5.6f, 20f + (s.X * 7.3f + s.Y * 3.1f) % 50f);
        }
    }

    internal sealed class OhLabel : OhSilk
    {
        readonly string _t; readonly float _x, _y, _size; readonly Color _c; readonly bool _bold;
        public OhLabel(string t, float x, float y, float size, Color c, bool bold)
        {
            size = Math.Max(size, Oh.MinSilk); _t = t; _x = x; _y = y; _size = size; _c = c; _bold = bold;
            float w = Oh.TextW(t, size) + 6, h = size * 1.5f;
            Bounds = new RectangleF(x - w / 2, y - h / 2, w, h);
        }
        public override void Draw(Graphics g) => Oh.Text(g, _t, _x, _y, _size, _c, _bold);
    }

    /// <summary>Text vertical (lletres apilades), p.ex. "HYD", "FUEL", "AUTO".</summary>
    internal sealed class OhVText : OhSilk
    {
        readonly string _t; readonly float _x, _y, _size;
        public OhVText(string t, float x, float y, float size)
        {
            size = Math.Max(size, Oh.MinSilk); _t = t; _x = x; _y = y; _size = size;
            float h = t.Length * size * 1.08f;
            Bounds = new RectangleF(x - size, y - h / 2 - 2, size * 2, h + 4);
        }
        public override void Draw(Graphics g)
        {
            float step = _size * 1.08f;
            float y0 = _y - (_t.Length - 1) * step / 2f;
            for (int i = 0; i < _t.Length; i++)
                Oh.Text(g, _t[i].ToString(), _x, y0 + i * step, _size, Oh.Silk);
        }
    }

    internal sealed class OhBoxLabel : OhSilk
    {
        readonly string _t; readonly float _x, _y, _size; readonly Color _c;
        public OhBoxLabel(string t, float x, float y, float size, Color c)
        {
            size = Math.Max(size, Oh.MinSilk); _t = t; _x = x; _y = y; _size = size; _c = c;
            float w = Oh.TextW(t, size) + 8, h = size * 1.75f;
            Bounds = new RectangleF(x - w / 2 - 2, y - h / 2 - 2, w + 4, h + 4);
        }
        public override void Draw(Graphics g)
        {
            float w = Oh.TextW(_t, _size) + 8, h = _size * 1.75f;
            using (var b = new SolidBrush(Color.FromArgb(40, 255, 255, 255))) g.FillRectangle(b, _x - w / 2, _y - h / 2, w, h);
            using (var p = new Pen(_c, 0.9f)) g.DrawRectangle(p, _x - w / 2, _y - h / 2, w, h);
            Oh.Text(g, _t, _x, _y + 0.2f, _size, _c);
        }
    }

    /// <summary>Títol amb línies a banda i banda: "——— GPWS ———".</summary>
    internal sealed class OhTitle : OhSilk
    {
        readonly string _t; readonly float _cx, _y, _x0, _x1, _size;
        public OhTitle(string t, float cx, float y, float x0, float x1, float size)
        {
            size = Math.Max(size, Oh.MinSilk); _t = t; _cx = cx; _y = y; _x0 = x0; _x1 = x1; _size = size;
            Bounds = RectangleF.FromLTRB(x0 - 2, y - size, x1 + 2, y + size);
        }
        public override void Draw(Graphics g)
        {
            float hw = Oh.TextW(_t, _size) / 2f + 5f;
            using var p = new Pen(Oh.Silk, 1.1f);
            if (_cx - hw > _x0) g.DrawLine(p, _x0, _y, _cx - hw, _y);
            if (_cx + hw < _x1) g.DrawLine(p, _cx + hw, _y, _x1, _y);
            Oh.Text(g, _t, _cx, _y, _size, Oh.Silk);
        }
    }

    internal sealed class OhLine : OhSilk
    {
        readonly PointF[] _pts; readonly Color _c; readonly float _w;
        public OhLine(Color c, float w, PointF[] pts)
        {
            _pts = pts; _c = c; _w = w;
            Bounds = RectangleF.Inflate(RectangleF.FromLTRB(pts.Min(p => p.X), pts.Min(p => p.Y), pts.Max(p => p.X), pts.Max(p => p.Y)), w + 2, w + 2);
        }
        public override void Draw(Graphics g)
        {
            using var p = new Pen(_c, _w) { LineJoin = LineJoin.Miter };
            g.DrawLines(p, _pts);
        }
    }

    internal sealed class OhArrow : OhSilk
    {
        readonly float _x, _y, _dir, _s; readonly Color _c;
        public OhArrow(float x, float y, float dirDeg, float size, Color c)
        {
            _x = x; _y = y; _dir = dirDeg; _s = size; _c = c;
            Bounds = new RectangleF(x - size - 2, y - size - 2, size * 2 + 4, size * 2 + 4);
        }
        public override void Draw(Graphics g)
        {
            var st = g.Save();
            g.TranslateTransform(_x, _y); g.RotateTransform(_dir);
            using var b = new SolidBrush(_c);
            g.FillPolygon(b, new[] { new PointF(_s, 0), new PointF(-_s * 0.8f, -_s * 0.8f), new PointF(-_s * 0.8f, _s * 0.8f) });
            g.Restore(st);
        }
    }

    /// <summary>Finestra del QNH a l'EFIS: mode (QNH/QFE) en petit a dalt i valor en 7 segments taronja; "Std" en estàndard.</summary>
    internal sealed class OhBaroWindow : OhElement
    {
        readonly RectangleF _r;
        bool _on; int _mode = 1; string _val = "";
        public OhBaroWindow(string id, RectangleF r) { Id = id; _r = r; Bounds = RectangleF.Inflate(r, 3, 3); }
        /// <summary>Actualitza el que mostra; retorna true si ha canviat.</summary>
        public bool Set(bool powered, int mode, double value, bool inHg)
        {
            // el format es tria pel valor (no per la unitat): en canviar d'unitat, la unitat i el valor poden arribar
            // en lectures diferents i, per un instant, es dibuixaria "1016.00" (desquadrat) en lloc de 1016
            string v = !powered ? "" : mode == 0 ? "Std" : value < 100 ? value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : Math.Round(value).ToString("0");
            if (powered == _on && mode == _mode && v == _val) return false;
            _on = powered; _mode = mode; _val = v;
            return true;
        }
        public override bool HitTest(PointF p) => false;
        public override void Draw(Graphics g)
        {
            using (var p = Oh.RoundRect(RectangleF.Inflate(_r, 2, 2), 2)) using (var b = new SolidBrush(Color.FromArgb(46, 50, 54))) g.FillPath(b, p);
            using (var b = new SolidBrush(Color.FromArgb(10, 12, 14))) g.FillRectangle(b, _r);
            if (!_on) return;
            // com a l'avió: QNH / QFE petit a la meitat dreta, a dalt; dígits baixos i amples, sense segments apagats
            if (_mode != 0) Oh.Text(g, _mode == 2 ? "QFE" : "QNH", _r.X + _r.Width * 0.70f, _r.Y + _r.Height * 0.2f, _r.Height * 0.24f, Color.FromArgb(240, 242, 244));
            var cells = new List<(char c, bool dp)>();
            foreach (char ch in _val)
            {
                if (ch == '.' && cells.Count > 0) cells[^1] = (cells[^1].c, true);
                else cells.Add((ch, false));
            }
            const int n = 4;
            if (cells.Count > n) cells.RemoveRange(0, cells.Count - n);
            var area = RectangleF.FromLTRB(_r.X + _r.Width * 0.035f, _r.Y + _r.Height * 0.40f, _r.Right - _r.Width * 0.02f, _r.Y + _r.Height * 0.90f);
            float cw = area.Width / n;
            int start = n - cells.Count;
            for (int i = 0; i < cells.Count; i++)
                OhSeg7.Digit(g, area.X + (start + i) * cw, area.Y, cw, area.Height, cells[i].c, cells[i].dp, thick: 0.22f, ghost: false, padF: 0.05f, color: Color.FromArgb(240, 242, 244));
        }
    }

    /// <summary>Estat de la pantalla del FCU (llegit del simulador).</summary>
    public sealed record FcuState(
        bool On,
        double Spd, bool SpdMach, bool SpdDashed, bool SpdManaged,
        double Hdg, bool HdgDashed, bool HdgManaged, bool TrkFpa,
        double Alt, bool AltManaged,
        double Vs, double Fpa, bool VsDashed,
        bool FbwStyle = false);   // true = dades del FlyByWire (MSFS): alguns formats propis (p.ex. FPA amb guions "--.-")

    /// <summary>
    /// Pantalla del FCU: dígits de 7 segments clars (amb els segments apagats lleugerament visibles) i rètols.
    /// Mode HDG-V/S o TRK-FPA canvia els rètols i el format del V/S (+12oo) o FPA (+0.8). Punt de "managed" a la dreta
    /// de SPD, HDG i ALT. Posicions preses de la pantalla del ToLiss (fraccions de l'amplada / alçada de la finestra).
    /// </summary>
    internal sealed class OhFcuDisplay : OhElement
    {
        readonly RectangleF _r;
        public FcuState? State0;
        static readonly Color Lit = Color.FromArgb(232, 234, 226);
        public OhFcuDisplay(string id, RectangleF r) { Id = id; _r = r; Bounds = RectangleF.Inflate(r, 3, 3); }
        public override bool HitTest(PointF p) => false;
        float X(float f) => _r.X + _r.Width * f;
        float Y(float f) => _r.Y + _r.Height * f;

        public override void Draw(Graphics g)
        {
            using (var p = Oh.RoundRect(RectangleF.Inflate(_r, 2, 2), 2)) using (var b = new SolidBrush(Color.FromArgb(46, 50, 54))) g.FillPath(b, p);
            using (var b = new SolidBrush(Color.FromArgb(10, 12, 14))) g.FillRectangle(b, _r);
            var s = State0;
            if (s == null || !s.On) return;
            float lh = _r.Height * 0.20f;              // mida dels rètols
            float ly = Y(0.24f);                       // fila dels rètols
            float dy = Y(0.40f), dh = _r.Height * 0.50f; // dígits
            float cw = _r.Width * 0.031f;              // amplada de cada dígit
            var ic = System.Globalization.CultureInfo.InvariantCulture;

            // ── rètols ──
            Oh.Text(g, s.SpdMach ? "MACH" : "SPD", X(0.040f), ly, lh, Lit);
            Oh.Text(g, s.TrkFpa ? "TRK" : "HDG", X(s.TrkFpa ? 0.268f : 0.235f), ly, lh, Lit);
            Oh.Text(g, "LAT", X(0.322f), ly, lh, Lit);   // (mesurat sobre la pantalla del ToLiss)
            // a sobre dels rètols HDG/TRK (x 1586) i V/S/FPA (x 1668) del botó que els commuta
            Oh.Text(g, s.TrkFpa ? "TRK" : "HDG", X((1586f - 1345f) / 605f), Y(0.80f), lh, Lit);
            Oh.Text(g, s.TrkFpa ? "FPA" : "V/S", X((1668f - 1345f) / 605f), Y(0.80f), lh, Lit);
            Oh.Text(g, "ALT", X(0.725f), ly, lh, Lit);
            // ┌── LVL/CH ──┐
            using (var lp = new Pen(Lit, Math.Max(0.5f, lh * 0.12f)))
            {
                float bx0 = X(0.757f), bx1 = X(0.906f), tw = _r.Width * 0.075f, cx = X(0.8315f), by = ly - lh * 0.05f;
                g.DrawLines(lp, new[] { new PointF(bx0, by + lh * 0.45f), new PointF(bx0, by), new PointF(cx - tw / 2, by) });
                g.DrawLines(lp, new[] { new PointF(cx + tw / 2, by), new PointF(bx1, by), new PointF(bx1, by + lh * 0.45f) });
            }
            Oh.Text(g, "LVL/CH", X(0.8315f), ly, lh, Lit);
            Oh.Text(g, s.TrkFpa ? "FPA" : "V/S", X(s.TrkFpa ? 0.962f : 0.950f), ly, lh, Lit);

            // ── valors ──
            string spd = s.SpdDashed ? "---" : s.SpdMach ? s.Spd.ToString("0.00", ic) : Math.Round(s.Spd).ToString("0", ic);
            Cells(g, X(0.043f), dy, cw, dh, 3, spd);   // 3 dígits (Mach: 0.78 amb el punt al primer)
            if (s.SpdManaged) Dot(g, X(0.150f), dy + dh * 0.5f, dh);
            string hdg = s.HdgDashed ? "---" : (((int)Math.Round(s.Hdg) % 360 + 360) % 360).ToString("000", ic);
            Cells(g, X(0.218f), dy, cw, dh, 3, hdg);
            if (s.HdgManaged) Dot(g, X(0.328f), dy + dh * 0.5f, dh);
            Cells(g, X(0.638f), dy, cw, dh, 5, Math.Max(0, Math.Round(s.Alt)).ToString("00000", ic));
            if (s.AltManaged) Dot(g, X(0.809f), dy + dh * 0.5f, dh);
            float vx = X(0.840f);
            if (s.VsDashed && s.TrkFpa && s.FbwStyle)
            {
                // FlyByWire en mode FPA: "--.-" (guió al lloc del signe i "-.-" al format de l'FPA)
                Sign(g, vx, dy, cw * 0.75f, dh, true);
                Cells(g, vx + cw * 0.8f, dy, cw, dh, 2, "-.-");
            }
            else if (s.VsDashed) Cells(g, vx, dy, cw, dh, 5, "-----");
            else
            {
                double v = s.TrkFpa ? s.Fpa : s.Vs;
                Sign(g, vx, dy, cw * 0.75f, dh, v < -0.0001);
                string body = s.TrkFpa ? Math.Min(9.9, Math.Abs(s.Fpa)).ToString("0.0", ic)
                                       : Math.Min(99, Math.Round(Math.Abs(s.Vs) / 100.0)).ToString("00", ic) + "oo";
                Cells(g, vx + cw * 0.8f, dy, cw, dh, s.TrkFpa ? 2 : 4, body);
            }
        }

        static void Cells(Graphics g, float x, float y, float cw, float h, int n, string text)
        {
            var cells = new List<(char c, bool dp)>();
            foreach (char ch in text)
            {
                if (ch == '.' && cells.Count > 0) cells[^1] = (cells[^1].c, true);
                else cells.Add((ch, false));
            }
            if (cells.Count > n) cells.RemoveRange(0, cells.Count - n);
            int start = n - cells.Count;
            for (int i = 0; i < n; i++)
            {
                var c = i >= start ? cells[i - start] : (' ', false);
                OhSeg7.Digit(g, x + i * cw, y, cw, h, c.Item1, c.Item2, thick: 0.2f, ghost: true, padF: 0.08f, color: Lit);
            }
        }
        static void Dot(Graphics g, float x, float cy, float h)
        {
            float d = h * 0.32f;   // punt de managed ben visible
            using var b = new SolidBrush(Lit);
            g.FillEllipse(b, x - d / 2, cy - d / 2, d, d);
        }
        static void Sign(Graphics g, float x, float y, float w, float h, bool minus)
        {
            float t = Math.Max(0.5f, w * 0.17f), cy = y + h * 0.5f, cx = x + w * 0.5f, a = w * 0.36f;
            using var b = new SolidBrush(Lit);
            g.FillRectangle(b, cx - a, cy - t / 2, a * 2, t);
            if (!minus) g.FillRectangle(b, cx - t / 2, cy - a, t, a * 2);
        }
    }

    /// <summary>
    /// Quadrant de gas (mòdul opcional del pedestal, es mostra al panell principal amb el botó TH).
    /// Dibuix vectorial de la maqueta "Marques del quadrant A320": canals, escala 0-45, TO GA, FLX MCT, CL, A/THR, 0,
    /// REV / FULL i les dues palanques (pom cilíndric + base plana amb la mosca). Coordenades internes: 451 x 830
    /// unitats (de y = -110 a 720), escalades al rectangle de l'element.
    /// </summary>
    internal sealed class OhThrottle : OhElement
    {
        public bool Shown;
        /// <summary>Posició de cada palanca: -1 REV FULL … 0 ralentí … 1 TO GA.</summary>
        public float PosL, PosR;
        public OhThrottle(string id, RectangleF r) { Id = id; Bounds = r; }
        public override bool HitTest(PointF p) => false;

        /// <summary>Rectangle (coordenades del panell) que ocupa una palanca a la posició p: pom, base, llum i ombres.</summary>
        public RectangleF LeverRect(float p, int side)
        {
            float sc = Bounds.Height / 830f, ox = Bounds.X + (Bounds.Width - 451 * sc) / 2f, oy = Bounds.Y + 110 * sc;
            float y = YOf(p);
            float u0 = side < 0 ? 36 : 225, u1 = side < 0 ? 226 : 418;
            float v0 = y - 26 - 150 - 8, v1 = y + 50 + 12;
            return RectangleF.FromLTRB(ox + u0 * sc - 2, oy + v0 * sc - 2, ox + u1 * sc + 2, oy + v1 * sc + 2);
        }
        public override IEnumerable<RectangleF> HitAreas { get { yield break; } }

        static readonly Color Yel = Color.FromArgb(242, 210, 27), Red = Color.FromArgb(232, 67, 47);
        static readonly (float y, float p)[] Det = { (72, 1f), (150, 0.8f), (236, 0.58f), (433, 0f), (628, -1f) };
        static float YOf(float p)
        {
            for (int i = 0; i < Det.Length - 1; i++)
                if (p <= Det[i].p && p >= Det[i + 1].p)
                {
                    float t = (Det[i].p - p) / (Det[i].p - Det[i + 1].p);
                    return Det[i].y + t * (Det[i + 1].y - Det[i].y);
                }
            return p > 0 ? Det[0].y : Det[^1].y;
        }

        static LinearGradientBrush Grad(float x0, float y0, float x1, float y1, params (float o, Color c)[] st)
        {
            var b = new LinearGradientBrush(new PointF(x0, y0), new PointF(x1, y1), st[0].c, st[^1].c);
            var cb = new ColorBlend(st.Length) { Positions = st.Select(s => s.o).ToArray(), Colors = st.Select(s => s.c).ToArray() };
            b.InterpolationColors = cb;
            b.WrapMode = WrapMode.TileFlipXY;
            return b;
        }
        static LinearGradientBrush VG(float y0, float y1, params (float, Color)[] st) => Grad(0, y0, 0, y1, st);
        static LinearGradientBrush HG(float x0, float x1, params (float, Color)[] st) => Grad(x0, 0, x1, 0, st);
        static Color A(int a, Color c) => Color.FromArgb(a, c);
        static void Line(Graphics g, float x1, float y1, float x2, float y2, float w, Color c) { using var p = new Pen(c, w); g.DrawLine(p, x1, y1, x2, y2); }
        static void Tri(Graphics g, Color c, params PointF[] pts) { using var b = new SolidBrush(c); g.FillPolygon(b, pts); }

        public override void Draw(Graphics g)
        {
            if (!Shown) return;
            var st = g.Save();
            float sc = Bounds.Height / 830f;
            g.TranslateTransform(Bounds.X + (Bounds.Width - 451 * sc) / 2f, Bounds.Y + 110 * sc);
            g.ScaleTransform(sc, sc);
            Housing(g);
            Channel(g, 90, 190); Channel(g, 258, 358);
            Scale(g);
            Marks(g, -1); Marks(g, 1);
            FootBar(g);
            Lever(g, 140, PosL, -1); Lever(g, 308, PosR, 1);
            g.Restore(st);
        }

        void Housing(Graphics g)
        {
            using (var fp = Oh.RoundRect(new RectangleF(5, 10, 441, 708), 24)) using (var b = new SolidBrush(Oh.Frame)) g.FillPath(b, fp);
            using var pl = Oh.RoundRect(new RectangleF(12, 14, 427, 700), 18);
            using (var b = VG(14, 714, (0, Oh.PlateA), (1, Oh.PlateB))) g.FillPath(b, pl);
            using (var b = VG(14, 714, (0, A(0, Oh.PlateHi)), (0.22f, A(0, Oh.PlateHi)), (0.36f, A(90, Oh.PlateHi)), (0.5f, A(0, Oh.PlateHi)), (1, A(0, Oh.PlateHi)))) g.FillPath(b, pl);
            using (var b = HG(12, 439, (0, Color.FromArgb(72, 0, 0, 0)), (0.05f, Color.FromArgb(0, 0, 0, 0)), (0.95f, Color.FromArgb(0, 0, 0, 0)), (1, Color.FromArgb(100, 0, 0, 0)))) g.FillPath(b, pl);
            using (var p = new Pen(Oh.PlateLo, 2f)) g.DrawPath(p, pl);
            Line(g, 30, 15.5f, 421, 15.5f, 1.2f, A(140, Oh.PlateHi));
        }

        static void Channel(Graphics g, float x0, float x1)
        {
            using var outer = Oh.RoundRect(new RectangleF(x0, 12, x1 - x0, 668), 10);
            using (var b = VG(12, 680, (0, Oh.PlateB), (1, Oh.PlateA))) g.FillPath(b, outer);
            using (var b = HG(x0, x1, (0, Color.FromArgb(90, 0, 0, 0)), (0.15f, Color.FromArgb(0, 0, 0, 0)), (0.85f, A(0, Oh.PlateHi)), (1, A(64, Oh.PlateHi)))) g.FillPath(b, outer);
            using (var p = new Pen(Oh.PlateLo, 2.4f)) g.DrawPath(p, outer);
            using var inner = Oh.RoundRect(new RectangleF(x0 + 10, 26, x1 - x0 - 20, 640), 7);
            using (var b = VG(26, 666, (0, Color.FromArgb(52, 65, 78)), (1, Color.FromArgb(70, 84, 98)))) g.FillPath(b, inner);
            using (var p = new Pen(Oh.PlateLo, 1.6f)) g.DrawPath(p, inner);
            Line(g, x1 - 11.5f, 34, x1 - 11.5f, 660, 1, A(115, Oh.PlateHi));
            Line(g, x0 + 14, 665.5f, x1 - 14, 665.5f, 1, A(115, Oh.PlateHi));
        }

        static void Scale(Graphics g)
        {
            const float y0 = 604, y45 = 228;
            for (int v = 0; v <= 45; v++)
            {
                float y = y0 + (y45 - y0) * v / 45f; bool major = v % 5 == 0; float w = major ? 13 : 9;
                Line(g, 197, y, 197 + w, y, major ? 2.4f : 2f, Oh.Silk);
                Line(g, 250 - w, y, 250, y, major ? 2.4f : 2f, Oh.Silk);
                if (major) Oh.Text(g, v.ToString(), 223, y, 16, Oh.Silk);
            }
        }

        static void Marks(Graphics g, int side)
        {
            float X(float x) => side < 0 ? x : 451 - x;
            int dir = side < 0 ? 1 : -1;
            float lx = X(70);
            Line(g, lx, 142, lx, 433, 2.2f, Oh.Silk);
            Line(g, lx, 433, X(84), 433, 2.2f, Oh.Silk);
            foreach (float yy in new float[] { 72, 150, 236 })
                Tri(g, Oh.Silk, new PointF(lx, yy - 8), new PointF(lx, yy + 8), new PointF(X(86), yy));
            Oh.Text(g, "TO", X(42), 66, 12, Oh.Silk); Oh.Text(g, "GA", X(42), 79, 12, Oh.Silk);
            Oh.Text(g, "FLX", X(40), 143, 13, Oh.Silk); Oh.Text(g, "MCT", X(40), 158, 13, Oh.Silk);
            Oh.Text(g, "CL", X(44), 236, 15, Oh.Silk);
            string[] athr = { "A", "/", "T", "H", "R" };
            for (int i = 0; i < athr.Length; i++) Oh.Text(g, athr[i], X(46), 284 + i * 30, 26, Oh.Silk);
            Oh.Text(g, "0", X(48), 438, 13, Oh.Silk);
            Tri(g, Oh.Silk, new PointF(X(6), 432), new PointF(X(24), 421), new PointF(X(24), 443));

            // REV: ratlles només al costat exterior i per sobre de les lletres
            var st = g.Save();
            using (var clip = new Region(new RectangleF(Math.Min(X(20), X(52)), 462, 32, 150)))
            {
                clip.Union(new RectangleF(Math.Min(X(20), X(86)), 462, 66, 22));
                g.SetClip(clip, CombineMode.Intersect);
                using var hp = new Pen(Yel, 2.4f);
                for (int k = -6; k < 16; k++) { float yy = 448 + k * 14; g.DrawLine(hp, X(16), yy, X(90), yy + 70); }
            }
            g.Restore(st);
            string[] rev = { "R", "E", "V" };
            for (int i = 0; i < 3; i++) Oh.Text(g, rev[i], X(60), 506 + i * 34, 26, Yel);
            float bx = X(80), yFull = YOf(-1);
            using (var yb = new SolidBrush(Yel)) g.FillRectangle(yb, bx - 2.5f, 488, 5, yFull - 488);
            Line(g, bx + 2.5f * dir, 489, X(90), 489, 3, Yel);
            Tri(g, Yel, new PointF(bx - 2.5f * dir, yFull - 9), new PointF(X(92), yFull), new PointF(bx - 2.5f * dir, yFull + 9));
            Oh.Text(g, "FULL", X(56), 648, 13, Yel);
        }

        static void FootBar(Graphics g)
        {
            using (var b = VG(680, 700, (0, Oh.PlateHi), (1, Oh.PlateB))) g.FillRectangle(b, 76, 680, 300, 20);
            Line(g, 76, 680.5f, 376, 680.5f, 1, Color.FromArgb(90, 255, 255, 255));
            Line(g, 76, 699.5f, 376, 699.5f, 1, Oh.PlateLo);
        }

        static void Lever(Graphics g, float xc, float p, int side)
        {
            float y = YOf(p);
            // base plana amb la mosca (osca en V a la vora exterior, a mitja fletxa lateral)
            const float OUT = 78, n = 16, h = 11, rad = 10;
            float pl = side < 0 ? OUT : xc - 42, pr = side < 0 ? xc + 42 : 451 - OUT, pt = y - 50, pb = y + 50;
            using var plate = new GraphicsPath();
            plate.AddLine(pl + rad, pt, pr - rad, pt);
            plate.AddArc(pr - 2 * rad, pt, 2 * rad, 2 * rad, 270, 90);
            if (side > 0) plate.AddLines(new[] { new PointF(pr, y - h), new PointF(pr - n, y), new PointF(pr, y + h) });
            plate.AddArc(pr - 2 * rad, pb - 2 * rad, 2 * rad, 2 * rad, 0, 90);
            plate.AddArc(pl, pb - 2 * rad, 2 * rad, 2 * rad, 90, 90);
            if (side < 0) plate.AddLines(new[] { new PointF(pl, y + h), new PointF(pl + n, y), new PointF(pl, y - h) });
            plate.AddArc(pl, pt, 2 * rad, 2 * rad, 180, 90);
            plate.CloseFigure();
            var s0 = g.Save();
            g.TranslateTransform(3, 5);
            using (var sb = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillPath(sb, plate);
            g.Restore(s0);
            using (var b = new SolidBrush(Color.FromArgb(22, 25, 29))) g.FillPath(b, plate);
            using (var b = VG(pt, pb, (0, Color.FromArgb(26, 255, 255, 255)), (0.25f, Color.FromArgb(0, 255, 255, 255)), (0.8f, Color.FromArgb(0, 0, 0, 0)), (1, Color.FromArgb(90, 0, 0, 0)))) g.FillPath(b, plate);
            using (var pe = new Pen(Color.FromArgb(77, 160, 175, 190), 1.2f)) g.DrawPath(pe, plate);
            float ox = side < 0 ? pl : pr, d = side < 0 ? 1 : -1;
            using (var pe = new Pen(Color.FromArgb(190, 230, 236, 242), 1.3f))
                g.DrawLines(pe, new[] { new PointF(ox, y - h), new PointF(ox + d * n, y), new PointF(ox, y + h) });

            // pom: cilindre negre brillant amb caps als extrems i llum vermella al costat exterior
            float hx = side < 0 ? xc - 90 : xc - 77, hw = 172, hh = 150, hy = y - 26 - hh;
            (float, Color)[] cyl = { (0, Color.FromArgb(20, 21, 24)), (0.18f, Color.FromArgb(42, 44, 48)), (0.3f, Color.FromArgb(141, 145, 150)),
                                    (0.36f, Color.FromArgb(201, 205, 209)), (0.42f, Color.FromArgb(85, 89, 95)), (0.6f, Color.FromArgb(35, 37, 40)),
                                    (0.85f, Color.FromArgb(18, 19, 21)), (1, Color.FromArgb(6, 6, 7)) };
            using (var body = Oh.RoundRect(new RectangleF(hx + 22, hy + 4, hw - 44, hh - 8), 6))
            {
                var s1 = g.Save(); g.TranslateTransform(5, 10);
                using (var sb = new SolidBrush(Color.FromArgb(110, 0, 0, 0))) g.FillPath(sb, body);
                g.Restore(s1);
                using var b = VG(hy, hy + hh, cyl); g.FillPath(b, body);
            }
            foreach (float cx in new[] { hx + 15, hx + hw - 15 })
            {
                using var cap = Oh.RoundRect(new RectangleF(cx - 15, hy - 2, 30, hh + 4), 10);
                using (var b = VG(hy, hy + hh, cyl)) g.FillPath(b, cap);
                using (var b = HG(cx - 15, cx + 15, (0, Color.FromArgb(140, 0, 0, 0)), (0.3f, Color.FromArgb(0, 0, 0, 0)), (0.7f, Color.FromArgb(0, 0, 0, 0)), (1, Color.FromArgb(153, 0, 0, 0)))) g.FillPath(b, cap);
            }
            Line(g, hx + 30, hy + 2, hx + 30, hy + hh - 2, 1.5f, Color.FromArgb(153, 0, 0, 0));
            Line(g, hx + hw - 30, hy + 2, hx + hw - 30, hy + hh - 2, 1.5f, Color.FromArgb(153, 0, 0, 0));
            float rx = side < 0 ? hx - 9 : hx + hw - 1;
            using (var rl = Oh.RoundRect(new RectangleF(rx, hy + 50, 10, 46), 2))
            using (var b = VG(hy + 50, hy + 96, (0, Color.FromArgb(255, 138, 106)), (0.4f, Red), (1, Color.FromArgb(158, 36, 20)))) g.FillPath(b, rl);
        }
    }

    internal sealed class OhDarkWindow : OhSilk
    {
        readonly RectangleF _r;
        public OhDarkWindow(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 3, 3); }
        public override void Draw(Graphics g)
        {
            using (var p = Oh.RoundRect(RectangleF.Inflate(_r, 2, 2), 2)) using (var b = new SolidBrush(Color.FromArgb(46, 50, 54))) g.FillPath(b, p);
            using (var b = new SolidBrush(Color.FromArgb(10, 12, 14))) g.FillRectangle(b, _r);
        }
    }

    /// <summary>Marques d'escala al voltant d'un rotatiu (amb etiquetes opcionals).</summary>
    internal sealed class OhDial : OhSilk
    {
        readonly float _cx, _cy, _r; readonly float[] _angles; readonly string[]? _labels; readonly float _size;
        public OhDial(float cx, float cy, float r, float[] angles, string[]? labels, float size)
        {
            size = Math.Max(size, Oh.MinSilk); _cx = cx; _cy = cy; _r = r; _angles = angles; _labels = labels; _size = size;
            float ext = r + (labels != null ? size * 2.2f : 4f);
            Bounds = new RectangleF(cx - ext, cy - ext, ext * 2, ext * 2);
        }
        public override void Draw(Graphics g)
        {
            using var p = new Pen(Oh.Silk, 1.0f);
            for (int i = 0; i < _angles.Length; i++)
            {
                double a = (_angles[i] - 90) * Math.PI / 180.0;
                float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
                g.DrawLine(p, _cx + c * _r, _cy + s * _r, _cx + c * (_r + 3.2f), _cy + s * (_r + 3.2f));
                if (_labels != null && i < _labels.Length && _labels[i] != "")
                {
                    float lr = _r + 4.5f + _size * 0.9f;
                    Oh.Text(g, _labels[i], _cx + c * lr, _cy + s * lr, _size, Oh.Silk);
                }
            }
        }
    }

    /// <summary>Escala en arc blanc al voltant d'un rotatiu, amb marques i (opcional) triangle verd a dalt.</summary>
    internal sealed class OhArcDial : OhSilk
    {
        readonly float _cx, _cy, _r, _a0, _a1; readonly float[] _ticks; readonly bool _tri;
        public OhArcDial(float cx, float cy, float r, float a0, float a1, float[] ticks, bool triangle)
        {
            _cx = cx; _cy = cy; _r = r; _a0 = a0; _a1 = a1; _ticks = ticks; _tri = triangle;
            Bounds = new RectangleF(cx - r - 12, cy - r - 12, (r + 12) * 2, (r + 12) * 2);
        }
        public override void Draw(Graphics g)
        {
            var rect = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            using (var sh = new Pen(Color.FromArgb(80, 0, 0, 0), 2.2f)) { var st = g.Save(); g.TranslateTransform(0.4f, 0.5f); g.DrawArc(sh, rect, _a0 - 90, _a1 - _a0); g.Restore(st); }
            using (var p = new Pen(Oh.Silk, 2.0f)) g.DrawArc(p, rect, _a0 - 90, _a1 - _a0);
            using (var tp = new Pen(Oh.Silk, 1.4f))
                foreach (var a in _ticks)
                {
                    double ra = (a - 90) * Math.PI / 180.0;
                    float c = (float)Math.Cos(ra), s = (float)Math.Sin(ra);
                    g.DrawLine(tp, _cx + c * (_r - 0.5f), _cy + s * (_r - 0.5f), _cx + c * (_r + 4.5f), _cy + s * (_r + 4.5f));
                }
            if (_tri)
            {
                float y0 = _cy - _r - 2.5f;
                using var b = new SolidBrush(Oh.SilkGreen);
                g.FillPolygon(b, new[] { new PointF(_cx - 4f, y0 - 6f), new PointF(_cx + 4f, y0 - 6f), new PointF(_cx, y0) });
            }
        }
    }

    /// <summary>Arc blanc amb punta de fletxa al final (p.ex. OFF → BRT de l'OVHD INTEG LT). Angles des de dalt, en sentit horari.</summary>
    internal sealed class OhArrowArc : OhSilk
    {
        readonly float _cx, _cy, _r, _a0, _a1;
        public OhArrowArc(float cx, float cy, float r, float a0, float a1)
        {
            _cx = cx; _cy = cy; _r = r; _a0 = a0; _a1 = a1;
            Bounds = new RectangleF(cx - r - 6, cy - r - 6, (r + 6) * 2, (r + 6) * 2);
        }
        public override void Draw(Graphics g)
        {
            const float head = 3.2f;
            float dir = _a1 >= _a0 ? 1f : -1f;                     // sentit de la fletxa (horari / antihorari)
            float aStop = _a1 - dir * head / _r * 180f / MathF.PI;      // l'arc acaba on comença la punta
            var rect = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            using (var p = new Pen(Oh.Silk, 2.2f)) g.DrawArc(p, rect, _a0 - 90, aStop - _a0);
            double ae = (_a1 - 90) * Math.PI / 180.0;
            float tx = -(float)Math.Sin(ae) * dir, ty = (float)Math.Cos(ae) * dir;   // tangent en el sentit de la fletxa
            float nx = (float)Math.Cos(ae), ny = (float)Math.Sin(ae);
            float px = _cx + nx * _r, py = _cy + ny * _r;
            using var b = new SolidBrush(Oh.Silk);
            g.FillPolygon(b, new[]
            {
                new PointF(px + tx * 2.2f, py + ty * 2.2f),
                new PointF(px - tx * head + nx * 2.8f, py - ty * head + ny * 2.8f),
                new PointF(px - tx * head - nx * 2.8f, py - ty * head - ny * 2.8f)
            });
        }
    }

    /// <summary>Cercle buit verd (vàlvula) sobre una línia del sinòptic.</summary>
    internal sealed class OhValve : OhSilk
    {
        readonly float _cx, _cy, _r;
        public OhValve(float cx, float cy, float r) { _cx = cx; _cy = cy; _r = r; Bounds = new RectangleF(cx - r - 3, cy - r - 3, r * 2 + 6, r * 2 + 6); }
        public override void Draw(Graphics g)
        {
            using var p = new Pen(Oh.SilkGreen, 2.0f);
            g.DrawEllipse(p, _cx - _r, _cy - _r, _r * 2, _r * 2);
        }
    }

    /// <summary>Separador gravat (línia fosca + línia clara) per dividir zones dins una planxa.</summary>
    internal sealed class OhGroove : OhSilk
    {
        readonly float _x0, _y0, _x1, _y1;
        public OhGroove(float x0, float y0, float x1, float y1)
        {
            _x0 = x0; _y0 = y0; _x1 = x1; _y1 = y1;
            Bounds = RectangleF.Inflate(RectangleF.FromLTRB(Math.Min(x0, x1), Math.Min(y0, y1), Math.Max(x0, x1), Math.Max(y0, y1)), 3, 3);
        }
        public override void Draw(Graphics g)
        {
            using (var d = new Pen(Oh.PlateLo, 1.2f)) g.DrawLine(d, _x0, _y0, _x1, _y1);
            using (var l = new Pen(Oh.PlateHi, 0.9f)) g.DrawLine(l, _x0 + 1.1f, _y0 + 1.1f, _x1 + 1.1f, _y1 + 1.1f);
        }
    }

    // ═══════════════════════════════ ELEMENTS INTERACTIUS ═══════════════════════════════════

    internal interface IOhTestable { bool Test { get; set; } }

    internal abstract class OhElement
    {
        public string Id = "";
        public RectangleF Bounds;
        public int Z;
        public A320OverheadControl Owner = null!;
        public Action<OhElement>? OnChange;
        public virtual RectangleF DirtyBounds => RectangleF.Inflate(Bounds, 4, 4);
        public virtual bool HitTest(PointF p) => RectangleF.Inflate(Bounds, 3, 3).Contains(p);
        public abstract void Draw(Graphics g);
        public virtual void Tap(PointF p) { }
        /// <summary>El dit es posa a sobre de l'element (abans del Tap) / s'aixeca o comença a arrossegar.</summary>
        public virtual void PressStart(PointF p) { }
        public virtual void PressEnd() { }
        /// <summary>Zones clicables (coordenades del panell) que es ressalten amb el botó "mostrar àrees".</summary>
        public virtual IEnumerable<RectangleF> HitAreas { get { yield return RectangleF.Inflate(Bounds, 3, 3); } }
        public virtual bool Animating => false;
        public virtual void Tick(float dt) { }
        public virtual int State => 0;
        protected void Changed() { OnChange?.Invoke(this); Owner.NotifyChanged(this); }
        protected static float Approach(float v, float target, float maxStep) =>
            v < target ? Math.Min(target, v + maxStep) : Math.Max(target, v - maxStep);
    }

    /// <summary>Polsador A320 amb dues llegendes (superior/inferior) d'intensitat regulable.</summary>
    internal sealed class OhPushButton : OhElement, IOhTestable
    {
        public string Upper, Lower;
        public Color UpperColor, LowerColor;
        public bool Momentary;
        public bool LowerLitWhenIn;
        public bool LowerFollowsState = true;
        public bool In;
        public float UpperLevel, LowerLevel;
        public bool Test { get; set; }
        float _depth, _hold;

        public OhPushButton(string id, RectangleF r, string upper, string lower, bool momentary)
        {
            Id = id; Bounds = r; Upper = upper; Lower = lower; Momentary = momentary;
            UpperColor = Oh.LegendColor(upper); LowerColor = Oh.LegendColor(lower);
            // Cockpit fosc: OFF s'il·lumina amb el polsador fora; ON/MAN/ALTN/OVRD amb el polsador dins.
            LowerLitWhenIn = lower != "OFF";
            In = !momentary && lower == "OFF";
            _depth = In ? 1 : 0;
        }

        public override int State => In ? 1 : 0;
        float TargetDepth => (In || _hold > 0) ? 1f : 0f;
        public override bool Animating => Math.Abs(_depth - TargetDepth) > 0.001f || _hold > 0;

        public override void Tick(float dt)
        {
            if (_hold > 0) _hold = Math.Max(0, _hold - dt);
            _depth = Approach(_depth, TargetDepth, dt * 14f);
        }

        public override void Tap(PointF p)
        {
            Owner.RaiseCommand(Id, "PUSH");
            if (Momentary || Owner.SimDriven) { _hold = 0.16f; Changed(); return; }
            In = !In;
            if (LowerFollowsState) LowerLevel = (In == LowerLitWhenIn) ? 1f : 0f;
            Changed();
        }

        /// <summary>Estil del panell principal (marc fosc, cara enfonsada).</summary>
        public bool Mip;

        public override void Draw(Graphics g)
        {
            var r = Bounds;
            if (Mip)
            {
                var mf = Oh.MipFrame(g, r);
                if (_depth > 0.01f) using (var db = new SolidBrush(Color.FromArgb((int)(40 * _depth), 0, 0, 0))) g.FillRectangle(db, mf);
                float mh = mf.Height / 2f;
                using (var sep = new Pen(Color.FromArgb(140, 0, 0, 0), 0.35f)) g.DrawLine(sep, mf.X + mf.Width * 0.12f, mf.Y + mh, mf.Right - mf.Width * 0.12f, mf.Y + mh);
                Oh.Legend(g, new RectangleF(mf.X, mf.Y + mf.Height * 0.05f, mf.Width, mh), Upper, UpperColor, Owner.Level(UpperLevel, Test), false);
                Oh.Legend(g, new RectangleF(mf.X, mf.Y + mh - mf.Height * 0.05f, mf.Width, mh), Lower, LowerColor, Owner.Level(LowerLevel, Test), Oh.Boxed(Lower));
                return;
            }
            using (var sp = Oh.RoundRect(r, 3f)) Oh.SoftShadow(g, sp, 1.2f, 2f, 2.6f, 120);
            using (var bp = Oh.RoundRect(r, 3f))
            {
                using (var bb = new LinearGradientBrush(r, Color.FromArgb(104, 110, 116), Color.FromArgb(28, 31, 34), 90f)) g.FillPath(bb, bp);
                using (var pe = new Pen(Color.FromArgb(24, 26, 28), 0.8f)) g.DrawPath(pe, bp);
            }
            float inset = 3.2f + _depth * 0.7f;
            var f = RectangleF.Inflate(r, -inset, -inset);
            f.Offset(0, _depth * 0.7f);
            using (var fp = Oh.RoundRect(f, 2f))
            {
                using (var fb = new SolidBrush(Color.FromArgb(18, 20, 22))) g.FillPath(fb, fp);
                using (var pe = new Pen(Color.FromArgb((int)(80 - 60 * _depth), 255, 255, 255), 0.6f)) g.DrawPath(pe, fp);
            }
            float half = f.Height / 2f;
            var up = new RectangleF(f.X, f.Y, f.Width, half);
            var lo = new RectangleF(f.X, f.Y + half, f.Width, half);
            using (var sep = new Pen(Color.FromArgb(40, 44, 48), 0.6f)) g.DrawLine(sep, f.X + 2, f.Y + half, f.Right - 2, f.Y + half);
            Oh.Legend(g, up, Upper, UpperColor, Owner.Level(UpperLevel, Test), false);
            Oh.Legend(g, lo, Lower, LowerColor, Owner.Level(LowerLevel, Test), Oh.Boxed(Lower));
        }
    }

    /// <summary>Polsador gran de FIRE (motor/APU).</summary>
    internal sealed class OhFireButton : OhElement, IOhTestable
    {
        public bool In;
        public float Lit;
        public bool Test { get; set; }
        float _depth;
        public OhFireButton(string id, RectangleF r) { Id = id; Bounds = r; }
        public override int State => In ? 1 : 0;
        public override bool Animating => Math.Abs(_depth - (In ? 1 : 0)) > 0.001f;
        public override void Tick(float dt) => _depth = Approach(_depth, In ? 1 : 0, dt * 10f);
        public override void Tap(PointF p) { In = !In; Changed(); }

        public override void Draw(Graphics g)
        {
            var r = Bounds;
            using (var sp = Oh.RoundRect(new RectangleF(r.X + 1.5f, r.Y + 2.2f, r.Width, r.Height), 4f))
            using (var sb = new SolidBrush(Color.FromArgb(120, 15, 25, 35))) g.FillPath(sb, sp);
            using (var bp = Oh.RoundRect(r, 4f))
            using (var bb = new LinearGradientBrush(r, Color.FromArgb(96, 100, 104), Color.FromArgb(26, 28, 30), 90f)) g.FillPath(bb, bp);
            float lvl = Owner.Level(Lit, Test);
            var f = RectangleF.Inflate(r, -5f - _depth, -5f - _depth);
            f.Offset(0, _depth * 1.2f);
            using (var fp = Oh.RoundRect(f, 3f))
            {
                using (var fb = new LinearGradientBrush(f, Oh.Lerp(Color.FromArgb(92, 18, 14), Color.FromArgb(255, 70, 40), lvl),
                                                          Oh.Lerp(Color.FromArgb(52, 8, 6), Color.FromArgb(200, 20, 10), lvl), 90f))
                    g.FillPath(fb, fp);
                using var pe = new Pen(Color.FromArgb(30, 6, 4), 0.8f); g.DrawPath(pe, fp);
            }
            var tc = Oh.Lerp(Color.FromArgb(150, 44, 34), Color.FromArgb(255, 236, 220), lvl);
            Oh.Text(g, "FIRE", f.X + f.Width / 2, f.Y + f.Height * 0.43f, f.Height * 0.42f, tc);
            Oh.Text(g, "PUSH", f.X + f.Width / 2, f.Y + f.Height * 0.82f, f.Height * 0.16f, tc);
        }
    }

    /// <summary>Guarda abatible (vermella/grisa) sobre un altre element. S'obre i es tanca animada.</summary>
    internal sealed class OhGuard : OhElement
    {
        public readonly OhElement Inner;
        readonly Color _col; readonly int _centerAlpha;
        public bool Open;
        float _a;

        public OhGuard(OhElement inner, Color col, int centerAlpha)
        {
            Inner = inner; _col = col; _centerAlpha = centerAlpha;
            Id = inner.Id + "_GUARD"; Z = 10; Bounds = G;
        }

        RectangleF G => RectangleF.Inflate(Inner.Bounds, 3.5f, 3.5f);
        RectangleF OpenRect { get { var g = G; return RectangleF.FromLTRB(g.Left - 2, g.Top - g.Height, g.Right + 2, g.Top); } }

        public override RectangleF DirtyBounds
        {
            get
            {
                var g = G; float m = g.Width * 0.14f + 4f;
                return RectangleF.FromLTRB(g.Left - m, g.Top - g.Height - 6, g.Right + m, g.Bottom + 5);
            }
        }
        public override int State => Open ? 1 : 0;
        public override bool HitTest(PointF p) => Inner.HitTest(p) || (_a > 0.5f && OpenRect.Contains(p));
        public override bool Animating => Math.Abs(_a - (Open ? 1 : 0)) > 0.001f;
        public override void Tick(float dt) => _a = Approach(_a, Open ? 1 : 0, dt * 3.4f);

        public override void Tap(PointF p)
        {
            if (!Open) { Open = true; Changed(); return; }
            if (OpenRect.Contains(p) && !Inner.HitTest(p)) { Open = false; Changed(); return; }
            Inner.Tap(p);
        }

        public override void Draw(Graphics g)
        {
            Inner.Draw(g);
            var gr = G;
            double th = _a * 168.0 * Math.PI / 180.0;
            float c = (float)Math.Cos(th), s = (float)Math.Sin(th);
            float w = gr.Width * (1f + 0.12f * s);
            float x = gr.X + (gr.Width - w) / 2f;
            RectangleF cover = c >= 0
                ? new RectangleF(x, gr.Top, w, Math.Max(0.6f, gr.Height * c))
                : new RectangleF(x, gr.Top + gr.Height * c, w, -gr.Height * c);
            bool underside = c < 0;
            var col = underside ? Oh.Shade(_col, 0.62f) : _col;

            // ombra projectada
            using (var sp = Oh.RoundRect(new RectangleF(cover.X + 1.5f + 2.5f * s, cover.Y + 2f, cover.Width, cover.Height), 3f))
            using (var sb = new SolidBrush(Color.FromArgb(70, 10, 15, 20))) g.FillPath(sb, sp);

            using (var path = Oh.RoundRect(cover, 3f))
            {
                using (var cb = new SolidBrush(Color.FromArgb(_centerAlpha, col))) g.FillPath(cb, path);
                float fw = Math.Min(3.6f, Math.Min(cover.Width, cover.Height) / 3f);
                using (var fp = new Pen(col, fw) { Alignment = PenAlignment.Inset }) g.DrawPath(fp, path);
                using (var hp = new Pen(Color.FromArgb(120, 255, 255, 255), 0.7f)) g.DrawLine(hp, cover.X + 3, cover.Y + 1.2f, cover.Right - 3, cover.Y + 1.2f);
            }
            // pestanya per aixecar-la
            float tabW = gr.Width * 0.34f, tabH = 4f;
            float tabY = c >= 0 ? cover.Bottom - 1 : cover.Top - tabH + 1;
            if (Math.Abs(c) > 0.25f)
                using (var tb = new SolidBrush(col)) g.FillRectangle(tb, gr.X + (gr.Width - tabW) / 2f, tabY, tabW, tabH);
            // frontissa
            using (var hb = new SolidBrush(Color.FromArgb(48, 52, 56))) g.FillRectangle(hb, gr.X + 1, gr.Top - 1.6f, gr.Width - 2, 3.2f);
        }
    }

    internal enum KnobStyle { Pointer, Round, Fluted }

    /// <summary>Rotatiu amb posicions discretes. Tocar la meitat esquerra = gira a l'esquerra; dreta = a la dreta.</summary>
    internal sealed class OhKnob : OhElement
    {
        readonly float _cx, _cy, _r; readonly float[] _angles;
        public int Index; public bool Wrap; public KnobStyle Style;
        float _ang;
        /// <summary>
        /// Zones de toc grans (en radis; 0 = només el rotatiu): mig ample, alçada per sobre i per sota del centre.
        /// Meitat esquerra = un pas enrere, meitat dreta = un pas endavant.
        /// </summary>
        public float ZoneHalfWidth, ZoneUp, ZoneDown;
        RectangleF Zone => RectangleF.FromLTRB(_cx - _r * ZoneHalfWidth, _cy - _r * ZoneUp, _cx + _r * ZoneHalfWidth, _cy + _r * ZoneDown);
        public override bool HitTest(PointF p) => ZoneHalfWidth > 0 ? Zone.Contains(p) : base.HitTest(p);
        public override IEnumerable<RectangleF> HitAreas
        {
            get
            {
                if (ZoneHalfWidth <= 0) { yield return RectangleF.Inflate(Bounds, 3, 3); yield break; }
                var z = Zone;
                yield return RectangleF.FromLTRB(z.Left, z.Top, _cx, z.Bottom);    // −
                yield return RectangleF.FromLTRB(_cx, z.Top, z.Right, z.Bottom);   // +
            }
        }
        public OhKnob(string id, float cx, float cy, float r, int index, float[] angles, KnobStyle style)
        {
            Id = id; _cx = cx; _cy = cy; _r = r; _angles = angles; Index = index; Style = style;
            _ang = angles[index];
            Bounds = new RectangleF(cx - r, cy - r, r * 2, r * 2);
        }
        public override int State => Index;
        public override bool Animating => Math.Abs(_ang - _angles[Index]) > 0.05f;
        public override void Tick(float dt) => _ang = Approach(_ang, _angles[Index], dt * 600f);
        // la cua del rotatiu pot sobresortir de la base (fins a Outer radis): cal repintar aquesta zona en girar
        public override RectangleF DirtyBounds => RectangleF.Inflate(Bounds, _r * (Outer - 1f) + 5, _r * (Outer - 1f) + 5);

        public override void Tap(PointF p)
        {
            int dir = p.X < _cx ? -1 : 1;
            int ni = Index + dir;
            if (Wrap) ni = (ni + _angles.Length) % _angles.Length;
            else ni = Math.Clamp(ni, 0, _angles.Length - 1);
            if (ni == Index) return;
            if (Wrap && Math.Abs(_angles[ni] - _ang) > 180) _ang += _angles[ni] > _ang ? 360 : -360;
            Index = ni;
            Owner.RaiseCommand(Id, "POS:" + ni);   // posició nova cap a l'avió
            Changed();
        }
        /// <summary>Posició llegida del simulador (sense enviar cap ordre). Retorna true si ha canviat.</summary>
        public bool SetIndex(int ni)
        {
            ni = Math.Clamp(ni, 0, _angles.Length - 1);
            if (ni == Index) return false;
            if (Wrap && Math.Abs(_angles[ni] - _ang) > 180) _ang += _angles[ni] > _ang ? 360 : -360;
            Index = ni;
            return true;
        }

        // ─── Rotatiu 3D calculat per píxel (sense PNG) ───
        // Model validat a la pàgina "Rotatiu A320": base rodona plana de plàstic blanc trencat (cantell recte
        // amb un arrodoniment mínim) + cos rectangular de cantells arrodonits que acaba en fletxa cap a la
        // selecció. La fletxa baixa en rampa i té els flancs inclinats; la cua és un arc concèntric amb la base
        // que en sobresurt una mica. Indicador blanc amb contorn negre pintat sobre la superfície fins a la
        // punta. Per a cada píxel: alçada → normal → llum (difusa + especular suau), llum fixa a dalt-esquerra.
        Bitmap? _cache;
        float _cacheAng = float.NaN, _cacheScale;

        // Paràmetres del model (els mateixos noms i valors que el requadre de la pàgina web)
        const float KAmplada = 0.44f;     // amplada (mitja amplada del cos)
        const float KFletxa = -0.28f;     // espatlla on comença la fletxa
        const float KPunta = -0.87f;      // punta de la fletxa (abans d'arrodonir)
        const float KPendent = 0.60f;     // pendent de la rampa de la fletxa
        const float KCua = 1.10f;         // radi de l'arc de la cua
        const float KVoraBase = 0.03f;    // arrodoniment de la vora de la base
        const float KArrodon = 0.13f;     // arrodoniment dels cantells del cos
        const float KBlanc = 0.35f;       // 0 = blanc trencat agrisat, 1 = blanc
        const float KBrillantor = 1.00f;  // brillantor general del plàstic

        const float Outer = 1.32f;        // zona dibuixada en radis de la base (la cua en pot sobresortir)
        const float BaseH = 0.20f;
        static readonly PointF[] BodyPoly =
        {
            new(-KAmplada, 1.4f), new(KAmplada, 1.4f), new(KAmplada, KFletxa), new(0f, KPunta), new(-KAmplada, KFletxa)
        };

        static float Smooth(float e0, float e1, float x)
        {
            float t = Math.Clamp((x - e0) / (e1 - e0), 0f, 1f);
            return t * t * (3 - 2 * t);
        }

        // Distància amb signe a un polígon (negatiu a dins)
        static float SdPoly(float px, float py, PointF[] V)
        {
            float d = (px - V[0].X) * (px - V[0].X) + (py - V[0].Y) * (py - V[0].Y), s = 1f;
            for (int i = 0, j = V.Length - 1; i < V.Length; j = i, i++)
            {
                float ex = V[j].X - V[i].X, ey = V[j].Y - V[i].Y;
                float wx = px - V[i].X, wy = py - V[i].Y;
                float t = Math.Clamp((wx * ex + wy * ey) / (ex * ex + ey * ey), 0f, 1f);
                float bx = wx - ex * t, by = wy - ey * t;
                d = Math.Min(d, bx * bx + by * by);
                bool c1 = py >= V[i].Y, c2 = py < V[j].Y, c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * MathF.Sqrt(d);
        }

        // Intersecció suau (cantells arrodonits també on els costats es troben amb l'arc de la cua)
        static float SMax(float a, float b, float k)
        {
            float h = Math.Clamp(0.5f - 0.5f * (b - a) / k, 0f, 1f);
            return a * h + b * (1 - h) + k * h * (1 - h);
        }

        static float SdBody(float u, float v) =>
            SMax(SdPoly(u, v, BodyPoly) - KArrodon, MathF.Sqrt(u * u + v * v) - KCua, Math.Max(0.02f, KArrodon));

        /// <summary>Alçada (unitats del radi) i material. mat: 0 fora, 1 base, 2 cos, 3 indicador blanc, 4 contorn negre.</summary>
        static float KnobHeight(float u, float v, out int mat)
        {
            float d = MathF.Sqrt(u * u + v * v);
            mat = 0;
            float h = 0f;
            if (d <= 1.0f)
            {
                const float be = KVoraBase;
                if (be > 0.001f && d > 1 - be) { float x = d - (1 - be); h = BaseH - be + MathF.Sqrt(Math.Max(0f, be * be - x * x)); }
                else h = BaseH;
                mat = 1;
            }
            float sd = SdBody(u, v);
            if (sd < 0)
            {
                float tipExt = KPunta - KArrodon;
                float f = Smooth(0f, 1f, (KFletxa + 0.10f - v) / Math.Max(0.05f, KFletxa + 0.10f - tipExt));
                float bevel = 0.16f + KPendent * 0.34f * f;
                float t = Math.Min(1f, -sd / bevel);
                float hb = BaseH + 0.42f * MathF.Sqrt(1f - (1f - t) * (1f - t)) * (1f - KPendent * 0.65f * f);
                if (hb > h) { h = hb; mat = 2; }
                if (mat == 2 && v < -0.18f)
                {
                    float au = Math.Abs(u);
                    if (au < 0.026f && v < -0.21f) mat = 3;
                    else if (au < 0.05f) mat = 4;
                }
            }
            return h;
        }

        Bitmap RenderKnob(float pxScale)
        {
            int R = Math.Max(4, (int)MathF.Ceiling(_r * pxScale));
            int size = (int)MathF.Ceiling(R * Outer * 2 + 2);
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
            var px = new int[size * size];
            double ar = _ang * Math.PI / 180.0;
            float ca = (float)Math.Cos(ar), sa = (float)Math.Sin(ar);
            float inv = 1f / R;
            float lx = -0.48f, ly = -0.62f, lz = 0.62f;
            float ln = MathF.Sqrt(lx * lx + ly * ly + lz * lz); lx /= ln; ly /= ln; lz /= ln;
            float hx = lx, hy = ly, hz = lz + 1f;
            float hn = MathF.Sqrt(hx * hx + hy * hy + hz * hz); hx /= hn; hy /= hn; hz /= hn;
            float lu = ca * lx + sa * ly, lv = -sa * lx + ca * ly;

            static float White(float c) => (c + (252f - c) * KBlanc) * KBrillantor;
            float bR = White(206), bG = White(203), bB = White(195);     // cos
            float sR = White(192), sG = White(190), sB = White(184);     // base

            float H(float wx, float wy, out int m)
            {
                float u = (ca * wx + sa * wy) * inv, v = (-sa * wx + ca * wy) * inv;
                return KnobHeight(u, v, out m);
            }

            float c0 = size / 2f;
            // files en paral·lel (cada fila escriu píxels diferents)
            Parallel.For(0, size, y => {
                for (int x = 0; x < size; x++)
                {
                    float wx = x + 0.5f - c0, wy = y + 0.5f - c0;
                    float dist = MathF.Sqrt(wx * wx + wy * wy) * inv;
                    if (dist > Outer) continue;
                    H(wx, wy, out int mat);
                    if (mat == 0) continue;
                    float gx = (H(wx + 1, wy, out _) - H(wx - 1, wy, out _)) * 0.5f * R;
                    float gy = (H(wx, wy + 1, out _) - H(wx, wy - 1, out _)) * 0.5f * R;
                    float nx = -gx, ny = -gy, nz = 1f;
                    float nn = MathF.Sqrt(nx * nx + ny * ny + nz * nz); nx /= nn; ny /= nn; nz /= nn;
                    float diff = Math.Max(0f, nx * lx + ny * ly + nz * lz);
                    float spec = MathF.Pow(Math.Max(0f, nx * hx + ny * hy + nz * hz), 24f);

                    float ar0, ag0, ab0, specK;
                    if (mat == 3) { ar0 = 250; ag0 = 250; ab0 = 246; specK = 0.10f; }
                    else if (mat == 4) { ar0 = 22; ag0 = 22; ab0 = 22; specK = 0.04f; }
                    else if (mat == 2) { ar0 = bR; ag0 = bG; ab0 = bB; specK = 0.20f; }
                    else { ar0 = sR; ag0 = sG; ab0 = sB; specK = 0.10f; }

                    float u = (ca * wx + sa * wy) * inv, v = (-sa * wx + ca * wy) * inv;
                    float sdHere = SdBody(u, v);
                    float shade = 0.36f + 0.78f * diff;
                    if (mat == 1)
                    {
                        shade *= 0.55f + 0.45f * Smooth(0f, 0.14f, sdHere);
                        if (SdBody(u + lu * 0.16f, v + lv * 0.16f) < 0) shade *= 0.62f;
                    }
                    float r = ar0 * shade + 255f * specK * spec;
                    float gg = ag0 * shade + 255f * specK * spec;
                    float bb = ab0 * shade + 255f * specK * spec;
                    // vora antialiasada: la de la base o la del cos (on la cua sobresurt)
                    float alpha = Math.Max(Math.Clamp((1.0f - dist) * R + 0.5f, 0f, 1f), Math.Clamp(-sdHere * R + 0.5f, 0f, 1f));
                    int A = (int)(alpha * 255f);
                    int Rr = (int)(Math.Clamp(r, 0, 255) * alpha), Gg = (int)(Math.Clamp(gg, 0, 255) * alpha), Bb = (int)(Math.Clamp(bb, 0, 255) * alpha);
                    px[y * size + x] = (A << 24) | (Rr << 16) | (Gg << 8) | Bb;
                }
            });
            var data = bmp.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            Marshal.Copy(px, 0, data.Scan0, px.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        // ─── Rotatiu estriat (OVHD INTEG LT), model validat a la pàgina de proposta ───
        // Base cilíndrica plana blanca (FlBase × el radi nominal, arriba a tocar de l'arc amb fletxa) + cos
        // estriat lleugerament cònic amb les estries fins a dalt + línia del dial blanca amb contorn negre del
        // centre de la cara del davant fins a la vora de la base.
        const float FlBase = 1.16f;       // diàmetre de la base respecte al radi nominal del rotatiu
        const float FlBodyR = 0.80f / FlBase, FlBaseH = 0.20f, FlTopH = 0.64f, FlDepth = 0.075f, FlEdge = 0.025f;
        const int FlFlutes = 16;          // estries
        const float FlConic = 0.38f;      // conicitat
        const float FlBright = 1.00f;     // brillantor

        static float FlutedHeight(float u, float v, out int mat, out float r)
        {
            r = MathF.Sqrt(u * u + v * v);
            mat = 0;
            if (r > 1.0f) return 0f;
            float h = r > 1 - FlEdge ? FlBaseH - FlEdge + MathF.Sqrt(Math.Max(0f, FlEdge * FlEdge - (r - 1 + FlEdge) * (r - 1 + FlEdge))) : FlBaseH;
            mat = 1;
            float phi = MathF.Atan2(u, -v);
            float groove = MathF.Pow(0.5f + 0.5f * MathF.Cos(FlFlutes * phi), 3f);
            float q = r / (1 - FlDepth * groove);
            float rt = FlBodyR * (1 - FlConic * 0.45f);
            float hb = -1f;
            if (q <= rt) hb = FlTopH;
            else if (q <= FlBodyR) hb = FlTopH - (FlTopH - FlBaseH) * ((q - rt) / Math.Max(0.005f, FlBodyR - rt));
            if (hb > 0 && q > rt - 0.03f && q <= rt) hb -= 0.03f * Smooth(rt - 0.03f, rt, q);
            if (hb > h) { h = hb; mat = 2; }
            if (v < 0.015f)
            {
                float au = Math.Abs(u);
                if (au < 0.028f) mat = 3; else if (au < 0.05f) mat = 4;
            }
            return h;
        }

        Bitmap RenderFluted(float pxScale)
        {
            int R = Math.Max(4, (int)MathF.Ceiling(_r * FlBase * pxScale));
            int size = R * 2 + 4;
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
            var px = new int[size * size];
            double ar = _ang * Math.PI / 180.0;
            float ca = (float)Math.Cos(ar), sa = (float)Math.Sin(ar), inv = 1f / R;
            float lx = -0.48f, ly = -0.62f, lz = 0.62f;
            float ln = MathF.Sqrt(lx * lx + ly * ly + lz * lz); lx /= ln; ly /= ln; lz /= ln;
            float hx = lx, hy = ly, hz = lz + 1f;
            float hn = MathF.Sqrt(hx * hx + hy * hy + hz * hz); hx /= hn; hy /= hn; hz /= hn;
            static float White(float c) => (c + (252f - c) * 0.35f) * FlBright;
            float bR = White(206), bG = White(203), bB = White(195), sR = White(192), sG = White(190), sB = White(184);
            float H(float wx, float wy, out int m, out float rr) =>
                FlutedHeight((ca * wx + sa * wy) * inv, (-sa * wx + ca * wy) * inv, out m, out rr);
            float c0 = size / 2f;
            // files en paral·lel (cada fila escriu píxels diferents)
            Parallel.For(0, size, y => {
                for (int x = 0; x < size; x++)
                {
                    float wx = x + 0.5f - c0, wy = y + 0.5f - c0;
                    if (MathF.Sqrt(wx * wx + wy * wy) > R * 1.02f) continue;
                    H(wx, wy, out int mat, out float r);
                    if (mat == 0) continue;
                    float gx = (H(wx + 1, wy, out _, out _) - H(wx - 1, wy, out _, out _)) * 0.5f * R;
                    float gy = (H(wx, wy + 1, out _, out _) - H(wx, wy - 1, out _, out _)) * 0.5f * R;
                    float nx = -gx, ny = -gy, nz = 1f;
                    float nn = MathF.Sqrt(nx * nx + ny * ny + nz * nz); nx /= nn; ny /= nn; nz /= nn;
                    float diff = Math.Max(0f, nx * lx + ny * ly + nz * lz);
                    float spec = MathF.Pow(Math.Max(0f, nx * hx + ny * hy + nz * hz), 20f);
                    float cr, cg, cb, sk;
                    if (mat == 3) { cr = 250; cg = 250; cb = 246; sk = 0.18f; }
                    else if (mat == 4) { cr = 22; cg = 22; cb = 22; sk = 0.04f; }
                    else if (mat == 2) { cr = bR; cg = bG; cb = bB; sk = 0.18f; }
                    else { cr = sR; cg = sG; cb = sB; sk = 0.10f; }
                    float sh = 0.32f + 0.8f * diff;
                    if (mat == 1 || (mat >= 3 && r > FlBodyR))
                    {
                        // ombra del cos sobre la base (cap a baix-dreta) i foscor al peu del cos
                        float lr = MathF.Sqrt((wx - R * 0.10f) * (wx - R * 0.10f) + (wy - R * 0.14f) * (wy - R * 0.14f)) * inv;
                        if (lr < FlBodyR * 0.98f) sh *= 0.70f;
                        sh *= 0.7f + 0.3f * Smooth(FlBodyR, FlBodyR + 0.12f, r);
                    }
                    float alpha = Math.Clamp((1.0f - r) * R + 0.5f, 0f, 1f);
                    float sp = 255f * sk * spec;
                    int A = (int)(alpha * 255f);
                    int Rr = (int)(Math.Clamp(cr * sh + sp, 0, 255) * alpha), Gg = (int)(Math.Clamp(cg * sh + sp, 0, 255) * alpha), Bb = (int)(Math.Clamp(cb * sh + sp, 0, 255) * alpha);
                    px[y * size + x] = (A << 24) | (Rr << 16) | (Gg << 8) | Bb;
                }
            });
            var data = bmp.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            Marshal.Copy(px, 0, data.Scan0, px.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        public override void Draw(Graphics g)
        {
            var rect = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            if (Style == KnobStyle.Fluted)
            {
                float rb = _r * FlBase;
                Oh.SoftShadowEllipse(g, new RectangleF(_cx - rb, _cy - rb, rb * 2, rb * 2), 1.6f, 2.4f, 2.8f, 130);
            }
            else Oh.SoftShadowEllipse(g, rect, 1.2f, 1.8f, 2.2f, 110);
            if (Style == KnobStyle.Pointer || Style == KnobStyle.Fluted)
            {
                // Rotatiu calculat en 3D (es regenera només si canvia l'angle o l'escala)
                float ps = Oh.PixelScale;
                if (_cache == null || _cacheAng != _ang || _cacheScale != ps)
                {
                    _cache?.Dispose();
                    _cache = Style == KnobStyle.Fluted ? RenderFluted(ps) : RenderKnob(ps);
                    _cacheAng = _ang; _cacheScale = ps;
                }
                float half = (_cache.Width / ps) / 2f;
                var oldIm = g.InterpolationMode; var oldPo = g.PixelOffsetMode;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(_cache, new RectangleF(_cx - half, _cy - half, half * 2, half * 2));
                g.InterpolationMode = oldIm; g.PixelOffsetMode = oldPo;
            }
            else
            {
                using (var lg = new LinearGradientBrush(rect, Color.FromArgb(246, 248, 250), Color.FromArgb(150, 156, 162), 50f)) g.FillEllipse(lg, rect);
                using (var p = new Pen(Color.FromArgb(96, 102, 108), 0.8f)) g.DrawEllipse(p, rect);
                var st = g.Save();
                g.TranslateTransform(_cx, _cy); g.RotateTransform(_ang);
                using (var rp = new Pen(Color.FromArgb(120, 126, 132), 0.6f))
                    for (int i = 0; i < 24; i++)
                    {
                        g.RotateTransform(15);
                        g.DrawLine(rp, 0, -_r * 0.98f, 0, -_r * 0.84f);
                    }
                using (var mb = new SolidBrush(Color.FromArgb(40, 44, 48))) g.FillEllipse(mb, -_r * 0.08f, -_r * 0.74f, _r * 0.16f, _r * 0.16f);
                g.Restore(st);
            }
        }
    }

    internal enum ToggleStyle { Bat, Paddle, Land, Metal }

    /// <summary>
    /// Interruptor dins un sòcol fosc. 2 posicions: qualsevol toc el commuta.
    /// 3 posicions: tocar a sobre del centre = puja un pas; a sota = baixa un pas.
    /// Estils: Bat (palanca amb pom), Paddle (pala plana, llums exteriors), Land (càpsula de 3 punts).
    /// </summary>
    internal sealed class OhToggle : OhElement
    {
        readonly float _cx, _cy, _sz; readonly int _n;
        public int Index; public ToggleStyle Style; public bool SpringToCenter;
        float _pos, _spring;
        public OhToggle(string id, float cx, float cy, int positions, int index, ToggleStyle style, float size)
        {
            Id = id; _cx = cx; _cy = cy; _n = positions; Index = index; Style = style; _sz = size;
            _pos = TargetPos;
            float hw = style == ToggleStyle.Land ? size * 1.25f : size * 1.1f;
            Bounds = new RectangleF(cx - hw, cy - size * 1.7f, hw * 2, size * 3.4f);
        }
        /// <summary>
        /// Zona de toc gran (unitats del panell; 0 = la de sempre): amplada total i alçada, centrades a la palanca.
        /// En una palanca horitzontal, meitat esquerra = un pas a l'esquerra i meitat dreta = un pas a la dreta.
        /// </summary>
        public float ZoneWidth, ZoneHeight;
        RectangleF Zone => new(_cx - ZoneWidth / 2, _cy - ZoneHeight / 2, ZoneWidth, ZoneHeight);
        public override bool HitTest(PointF p) => ZoneWidth > 0 ? Zone.Contains(p) : base.HitTest(p);
        public override IEnumerable<RectangleF> HitAreas
        {
            get
            {
                if (ZoneWidth <= 0) { yield return RectangleF.Inflate(Bounds, 3, 3); yield break; }
                var z = Zone;
                if (!Horizontal) { yield return z; yield break; }
                yield return RectangleF.FromLTRB(z.Left, z.Top, _cx, z.Bottom);
                yield return RectangleF.FromLTRB(_cx, z.Top, z.Right, z.Bottom);
            }
        }
        /// <summary>Palanca horitzontal: posició 0 a l'esquerra, última a la dreta (p.ex. ADF / OFF / VOR).</summary>
        public bool Horizontal { get; private set; }
        public OhToggle MakeHorizontal()
        {
            Horizontal = true;
            var b = Bounds; float cx = b.X + b.Width / 2, cy = b.Y + b.Height / 2;
            Bounds = new RectangleF(cx - b.Height / 2, cy - b.Width / 2, b.Height, b.Width);
            return this;
        }
        float TargetPos => _n == 2 ? (Index == 0 ? -1f : 1f) : Index - 1f;
        public override int State => Index;
        public override bool Animating => Math.Abs(_pos - TargetPos) > 0.001f || _spring > 0;
        public override RectangleF DirtyBounds => RectangleF.Inflate(Bounds, 4, 4);

        public override void Tick(float dt)
        {
            _pos = Approach(_pos, TargetPos, dt * 10f);
            if (_spring > 0)
            {
                _spring -= dt;
                if (_spring <= 0) { _spring = 0; Index = 1; Changed(); }
            }
        }

        public override void Tap(PointF p)
        {
            bool first = Horizontal ? p.X < _cx : p.Y < _cy;
            int ni = _n == 2 ? 1 - Index : Math.Clamp(Index + (first ? -1 : 1), 0, _n - 1);
            if (ni == Index) return;
            Index = ni;
            if (SpringToCenter && _n == 3 && ni != 1) _spring = 0.45f;
            Owner.RaiseCommand(Id, "POS:" + ni);   // posició nova cap a l'avió
            Changed();
        }
        /// <summary>Posició llegida del simulador (sense enviar cap ordre). Retorna true si ha canviat.</summary>
        public bool SetIndex(int ni)
        {
            ni = Math.Clamp(ni, 0, _n - 1);
            if (ni == Index) return false;
            Index = ni;
            return true;
        }

        public override void Draw(Graphics g)
        {
            if (Horizontal)
            {
                // es dibuixa la palanca vertical girada 90° (amunt → esquerra)
                var st = g.Save();
                g.TranslateTransform(_cx, _cy); g.RotateTransform(-90); g.TranslateTransform(-_cx, -_cy);
                DrawVertical(g);
                g.Restore(st);
            }
            else DrawVertical(g);
        }

        void DrawVertical(Graphics g)
        {
            float R = _sz * 1.02f;
            DrawSocket(g, R);
            float ext = 1f - Math.Abs(_pos);             // 0 = palanca estirada, 1 = apuntant a l'observador
            float dir = _pos >= 0 ? 1f : -1f;
            switch (Style)
            {
                case ToggleStyle.Metal:
                {
                    // palanca metàl·lica cromada (ADF/VOR de l'EFIS)
                    float L = R * 1.15f;
                    float tipY = _cy + _pos * L;
                    // doble de gruix i cònica: estreta a la base, ampla a la punta
                    float bw = R * 0.20f, tw = R * 0.44f;
                    Collar(g, R);
                    Shaft(g, bw, tw, tipY, Color.FromArgb(70, 74, 80), Color.FromArgb(232, 236, 240));
                    float br = Math.Max(R * (0.30f + 0.10f * ext), tw * 1.12f);
                    var ball = new RectangleF(_cx - br, tipY - br * (0.9f + 0.1f * ext), br * 2, br * (1.8f + 0.2f * ext));
                    Oh.SoftShadowEllipse(g, ball, 1.2f, 1.8f, 2f, 120);
                    Oh.Specular(g, ball, Color.FromArgb(250, 252, 255), Color.FromArgb(96, 102, 110));
                    break;
                }
                case ToggleStyle.Bat:
                {
                    float L = R * 1.05f;
                    float tipY = _cy + _pos * L;
                    // cònica: base ampla i la punta segons BatConic (valors de la pàgina de proposta)
                    float bw = R * 0.17f, tw = bw * BatConic;
                    Collar(g, R);
                    Shaft(g, bw, tw, tipY, Color.FromArgb(14, 14, 16), Color.FromArgb(118, 118, 122));
                    float br = Math.Max(R * (0.25f + 0.12f * ext), tw * 1.25f);
                    var ball = new RectangleF(_cx - br, tipY - br * (0.85f + 0.15f * ext), br * 2, br * (1.7f + 0.3f * ext));
                    Oh.SoftShadowEllipse(g, ball, 1.4f, 2.0f, 2.2f, 120);
                    Oh.Specular(g, ball, Color.FromArgb(240, 236, 226), Color.FromArgb(78, 74, 68));
                    break;
                }
                case ToggleStyle.Paddle:
                {
                    float L = R * 0.95f;
                    float tipY = _cy + _pos * L;
                    float bw = R * 0.36f * PaddleWidth, tw = R * 0.44f * PaddleWidth;
                    Collar(g, R);
                    Shaft(g, bw, tw, tipY, Color.FromArgb(10, 10, 12), Color.FromArgb(70, 70, 74));
                    // cara frontal plana de la pala (clara), més alta com més apunta a l'observador
                    // PaddleCenterH només actua amb la pala centrada (ext = 1) i s'esvaeix en estirar-la
                    float fh = R * (0.24f + 0.55f * ext) * (1f + (PaddleCenterH - 1f) * ext);
                    float fy = tipY - fh * (0.5f - 0.3f * dir * (1 - ext));
                    var face = new RectangleF(_cx - tw - 0.6f, fy, (tw + 0.6f) * 2, fh);
                    using (var fp = Oh.RoundRect(face, 2f))
                    {
                        Oh.SoftShadow(g, fp, 1.3f, 2.0f, 2.4f, 130);
                        using (var fb = new LinearGradientBrush(RectangleF.Inflate(face, 0.5f, 0.5f), Color.FromArgb(226, 220, 204), Color.FromArgb(118, 112, 100), 90f)) g.FillPath(fb, fp);
                        // lleuger desgast/brut de la cara (gra) i aresta superior brillant
                        Oh.Grain(g, fp);
                        using (var hp = new Pen(Color.FromArgb(190, 255, 252, 240), 0.7f)) g.DrawLine(hp, face.X + 1.5f, face.Y + 0.7f, face.Right - 1.5f, face.Y + 0.7f);
                        using var pe = new Pen(Color.FromArgb(36, 34, 30), 0.7f); g.DrawPath(pe, fp);
                    }
                    break;
                }
                case ToggleStyle.Land:
                {
                    float L = R * 0.8f;
                    float tipY = _cy + _pos * L;
                    Shaft(g, R * 0.3f, R * 0.34f, tipY, Color.FromArgb(10, 10, 12), Color.FromArgb(70, 70, 74));
                    float pw = R * 1.9f, ph = R * (0.62f + 0.25f * ext);
                    var pill = new RectangleF(_cx - pw / 2, tipY - ph / 2, pw, ph);
                    using (var pp = Oh.RoundRect(pill, ph / 2))
                    {
                        Oh.SoftShadow(g, pp, 1.5f, 2.3f, 2.8f, 140);
                        using (var pb = new LinearGradientBrush(RectangleF.Inflate(pill, 0.5f, 0.5f), Color.FromArgb(118, 100, 84), Color.FromArgb(36, 30, 26), 90f)) g.FillPath(pb, pp);
                        Oh.Grain(g, pp);
                        using (var hp = new Pen(Color.FromArgb(110, 255, 230, 200), 0.7f))
                            g.DrawLine(hp, pill.X + ph * 0.5f, pill.Y + 0.8f, pill.Right - ph * 0.5f, pill.Y + 0.8f);
                        using var pe = new Pen(Color.FromArgb(22, 18, 16), 0.8f); g.DrawPath(pe, pp);
                    }
                    float dr = ph * 0.27f;
                    for (int i = -1; i <= 1; i++)
                    {
                        var dot = new RectangleF(_cx + i * pw * 0.27f - dr, tipY - dr, dr * 2, dr * 2);
                        // vidre del llum: anell fosc enfonsat + lent amb reflex
                        using (var rb = new SolidBrush(Color.FromArgb(24, 20, 18))) g.FillEllipse(rb, RectangleF.Inflate(dot, 0.6f, 0.6f));
                        Oh.Specular(g, dot, Color.FromArgb(206, 196, 178), Color.FromArgb(84, 76, 66));
                    }
                    break;
                }
            }
        }

        /// <summary>Sòcol rodó enfonsat: llavi del forat (fosc a dalt, clar a baix), anell coure fosc i forat negre.</summary>
        void DrawSocket(Graphics g, float R)
        {
            var lip = new RectangleF(_cx - R - 1.3f, _cy - R - 1.3f, (R + 1.3f) * 2, (R + 1.3f) * 2);
            // oclusió ambiental: la planxa s'enfosqueix suaument al voltant del forat
            Oh.SoftShadowEllipse(g, RectangleF.Inflate(lip, 0.5f, 0.5f), 0.4f, -0.6f, 2.6f, 90);
            using (var lb = new LinearGradientBrush(lip, Color.FromArgb(30, 34, 40), Color.FromArgb(176, 186, 196), 90f)) g.FillEllipse(lb, lip);
            using (var lh = new Pen(Color.FromArgb(150, 236, 240, 244), 0.7f)) g.DrawArc(lh, RectangleF.Inflate(lip, -0.4f, -0.4f), 25, 130);
            var outer = new RectangleF(_cx - R, _cy - R, R * 2, R * 2);
            using (var rb = new LinearGradientBrush(outer, Color.FromArgb(30, 22, 18), Color.FromArgb(128, 92, 68), 90f)) g.FillEllipse(rb, outer);
            float r2 = R * 0.8f;
            var hole = new RectangleF(_cx - r2, _cy - r2, r2 * 2, r2 * 2);
            using var path = new GraphicsPath();
            path.AddEllipse(hole);
            using (var pg = new PathGradientBrush(path)
            {
                CenterColor = Color.FromArgb(52, 50, 52),
                SurroundColors = new[] { Color.FromArgb(4, 4, 6) },
                CenterPoint = new PointF(_cx, _cy + r2 * 0.3f)
            }) g.FillEllipse(pg, hole);
            using (var hp = new Pen(Color.FromArgb(90, 210, 170, 140), 0.8f))
                g.DrawArc(hp, RectangleF.Inflate(outer, -0.8f, -0.8f), 30, 120);
        }

        // Valors de la pàgina de proposta d'interruptors (per defecte fins que es passin els definitius)
        const float BatConic = 2.05f;     // conicitat palanca (punta / base)
        const float PaddleWidth = 1.15f;  // amplada de la pala
        const float PaddleCenterH = 0.50f; // alçada del rectangle de la pala, només en posició central
        const float CollarSize = 1.00f;   // mida del cilindre de la base

        /// <summary>
        /// Base comuna de pala i palanca: cilindre fosc dins el forat, amb anelles de rosca a la vora i un
        /// trau allargat al llarg de l'eix de basculament per on surt la palanca.
        /// </summary>
        void Collar(Graphics g, float R)
        {
            if (CollarSize <= 0.01f) return;
            float rc = R * 0.62f * CollarSize;
            var cr = new RectangleF(_cx - rc, _cy - rc, rc * 2, rc * 2);
            Oh.SoftShadowEllipse(g, cr, 0.8f, 1.2f, 1.6f, 120);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(cr);
                using var pg = new PathGradientBrush(path)
                {
                    CenterColor = Color.FromArgb(72, 74, 80),
                    SurroundColors = new[] { Color.FromArgb(12, 12, 14) },
                    CenterPoint = new PointF(_cx - rc * 0.3f, _cy - rc * 0.35f)
                };
                g.FillPath(pg, path);
            }
            for (int i = 0; i < 5; i++)
            {
                float r = rc * (0.97f - i * 0.065f);
                using var p = new Pen(i % 2 == 1 ? Color.FromArgb(150, 0, 0, 0) : Color.FromArgb(40, 255, 255, 255), 0.45f);
                g.DrawEllipse(p, _cx - r, _cy - r, r * 2, r * 2);
            }
            using (var hp = new Pen(Color.FromArgb(110, 255, 255, 255), 0.6f))
                g.DrawArc(hp, RectangleF.Inflate(cr, -rc * 0.04f, -rc * 0.04f), 200, 100);
            float sw = R * 0.36f, sl = rc * 1.45f;
            var slot = new RectangleF(_cx - sw / 2, _cy - sl / 2, sw, sl);
            using (var sp = Oh.RoundRect(slot, sw / 2))
            {
                using (var sb = new SolidBrush(Color.FromArgb(4, 4, 5))) g.FillPath(sb, sp);
                using var lb = new LinearGradientBrush(RectangleF.Inflate(slot, 0.5f, 0.5f), Color.FromArgb(230, 0, 0, 0), Color.FromArgb(140, 200, 205, 210), 90f);
                using var pe = new Pen(lb, 0.6f);
                g.DrawPath(pe, sp);
            }
        }

        /// <summary>Tija de la palanca (trapezi del centre del sòcol fins a la punta) amb reflex metàl·lic.</summary>
        void Shaft(Graphics g, float bw, float tw, float tipY, Color dark, Color light)
        {
            if (Math.Abs(tipY - _cy) < 0.6f) return;
            var poly = new[] { new PointF(_cx - bw, _cy), new PointF(_cx + bw, _cy), new PointF(_cx + tw, tipY), new PointF(_cx - tw, tipY) };
            using (var sp = new GraphicsPath()) { sp.AddPolygon(poly); Oh.SoftShadow(g, sp, 1.5f, 2.0f, 2.2f, 120); }
            float hw = Math.Max(bw, tw);
            var lrect = new RectangleF(_cx - hw - 0.5f, Math.Min(_cy, tipY) - 1, hw * 2 + 1, Math.Abs(tipY - _cy) + 2);
            using var lb = new LinearGradientBrush(lrect, dark, dark, 0f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { dark, light, Oh.Shade(light, 0.55f), dark },
                    Positions = new[] { 0f, 0.35f, 0.6f, 1f }
                }
            };
            g.FillPolygon(lb, poly);
        }
    }

    /// <summary>Polsador rodó momentani (TEST, CALLS, RAIN RPLNT...).</summary>
    internal sealed class OhRoundButton : OhElement
    {
        readonly float _cx, _cy, _r; float _hold, _depth;
        /// <summary>Marge de la zona de toc al voltant del botó (0 = just el voltant negre).</summary>
        public float HitPad = 3f;
        public override bool HitTest(PointF p) => RectangleF.Inflate(Bounds, HitPad, HitPad).Contains(p);
        public override IEnumerable<RectangleF> HitAreas { get { yield return RectangleF.Inflate(Bounds, HitPad, HitPad); } }
        public OhRoundButton(string id, float cx, float cy, float r)
        {
            Id = id; _cx = cx; _cy = cy; _r = r; Bounds = new RectangleF(cx - r, cy - r, r * 2, r * 2);
        }
        public override bool Animating => _hold > 0 || _depth > 0.001f;
        public override void Tick(float dt)
        {
            if (_hold > 0) _hold = Math.Max(0, _hold - dt);
            _depth = Approach(_depth, _hold > 0 ? 1 : 0, dt * 14f);
        }
        public override void Tap(PointF p) { _hold = 0.18f; Owner.RaiseCommand(Id, "PUSH"); Changed(); }
        public override void Draw(Graphics g)
        {
            var o = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            using (var sb = new SolidBrush(Color.FromArgb(110, 15, 25, 35))) g.FillEllipse(sb, o.X + 1.3f, o.Y + 2, o.Width, o.Height);
            using (var lg = new LinearGradientBrush(o, Color.FromArgb(70, 74, 78), Color.FromArgb(18, 20, 22), 60f)) g.FillEllipse(lg, o);
            float ir = _r * (0.66f - 0.05f * _depth);
            var i = new RectangleF(_cx - ir, _cy - ir + _depth * 0.6f, ir * 2, ir * 2);
            using (var ib = new LinearGradientBrush(i, Oh.Shade(Color.FromArgb(206, 210, 214), 1f - 0.25f * _depth), Color.FromArgb(84, 90, 96), 50f)) g.FillEllipse(ib, i);
            using (var p = new Pen(Color.FromArgb(40, 44, 48), 0.6f)) g.DrawEllipse(p, i);
        }
    }

    /// <summary>Pantalla de 7 segments (voltatge bateria, freqüències RMP).</summary>
    internal sealed class OhSeg7 : OhElement
    {
        public string Text;
        readonly int _cells; readonly string _suffix;
        static readonly Dictionary<char, string> Map = new()
        {
            ['0'] = "abcdef", ['1'] = "bc", ['2'] = "abged", ['3'] = "abgcd", ['4'] = "fgbc", ['5'] = "afgcd",
            ['6'] = "afgedc", ['7'] = "abc", ['8'] = "abcdefg", ['9'] = "abcdfg", ['-'] = "g", [' '] = "",
            ['S'] = "afgcd", ['t'] = "fged", ['d'] = "bcdeg", ['o'] = "cdeg"
        };
        public OhSeg7(string id, RectangleF r, string text, int cells, string suffix)
        {
            Id = id; Bounds = r; Text = text; _cells = cells; _suffix = suffix;
        }
        public override void Draw(Graphics g)
        {
            var r = Bounds;
            using (var bp = Oh.RoundRect(r, 3f))
            using (var bb = new LinearGradientBrush(r, Color.FromArgb(70, 74, 78), Color.FromArgb(22, 24, 26), 90f)) g.FillPath(bb, bp);
            var win = RectangleF.Inflate(r, -3.5f, -3.5f);
            using (var wb = new SolidBrush(Color.FromArgb(10, 8, 6))) g.FillRectangle(wb, win);
            float sw = _suffix.Length > 0 ? win.Height * 0.42f : 0f;
            var area = RectangleF.Inflate(new RectangleF(win.X, win.Y, win.Width - sw, win.Height), -2f, -2f);
            var cells = new List<(char c, bool dp)>();
            foreach (char ch in Text)
            {
                if (ch == '.' && cells.Count > 0) cells[^1] = (cells[^1].c, true);
                else cells.Add((ch, false));
            }
            int start = _cells - cells.Count;
            float cw = area.Width / _cells;
            float hgt = area.Height * 0.86f;
            float y0 = area.Y + (area.Height - hgt) / 2f;
            for (int i = 0; i < _cells; i++)
            {
                var cell = i >= start && i - start < cells.Count ? cells[i - start] : (' ', false);
                Digit(g, area.X + i * cw, y0, cw, hgt, cell.Item1, cell.Item2);
            }
            if (sw > 0) Oh.Text(g, _suffix, win.Right - sw / 2f, win.Y + win.Height * 0.3f, win.Height * 0.36f, Oh.SegAmber);
        }

        /// <param name="thick">gruix dels segments respecte de l'amplada del dígit</param>
        /// <param name="ghost">dibuixa els segments apagats (lleugerament visibles)</param>
        internal static void Digit(Graphics g, float x, float y, float w, float h, char c, bool dp, float thick = 0.19f, bool ghost = true, float padF = 0.12f, Color? color = null)
        {
            float pad = w * padF;
            float sx = x + pad, swd = w - pad * 2 - w * 0.1f;
            float t = Math.Max(0.9f, swd * thick);
            float hh = h / 2f;
            Map.TryGetValue(c, out var segs);
            segs ??= "";
            var rects = new (char n, RectangleF r)[]
            {
                ('a', new RectangleF(sx + t * 0.6f, y, swd - t * 1.2f, t)),
                ('g', new RectangleF(sx + t * 0.6f, y + hh - t / 2, swd - t * 1.2f, t)),
                ('d', new RectangleF(sx + t * 0.6f, y + h - t, swd - t * 1.2f, t)),
                ('f', new RectangleF(sx, y + t * 0.6f, t, hh - t * 0.9f)),
                // l'1 (només b i c) ocupa tota l'alçada, igual que la resta de dígits
                ('b', c == '1' ? new RectangleF(sx + swd - t, y, t, hh - t * 0.15f) : new RectangleF(sx + swd - t, y + t * 0.6f, t, hh - t * 0.9f)),
                ('e', new RectangleF(sx, y + hh + t * 0.3f, t, hh - t * 0.9f)),
                ('c', c == '1' ? new RectangleF(sx + swd - t, y + hh + t * 0.15f, t, hh - t * 0.15f) : new RectangleF(sx + swd - t, y + hh + t * 0.3f, t, hh - t * 0.9f)),
            };
            var col = color ?? Oh.SegAmber;
            using var on = new SolidBrush(col);
            using var off = new SolidBrush(Color.FromArgb(ghost ? 26 : 0, col));
            foreach (var (n, rr) in rects) if (ghost || segs.IndexOf(n) >= 0) g.FillRectangle(segs.IndexOf(n) >= 0 ? on : off, rr);
            if (ghost || dp) g.FillRectangle(dp ? on : off, sx + swd + w * 0.03f, y + h - t, t, t);
        }
    }

    /// <summary>Tecla negra petita (RMP/ACP) amb llum de selecció o text il·luminable.</summary>
    internal sealed class OhKey : OhElement
    {
        public string Label; public bool Led; public bool LedWide; public bool On; public string? Group; public bool LitText; public bool Arrows; public bool Momentary;
        float _hold;
        public OhKey(string id, RectangleF r, string label) { Id = id; Bounds = r; Label = label; }
        public override int State => On ? 1 : 0;
        public override bool Animating => _hold > 0;
        public override void Tick(float dt) => _hold = Math.Max(0, _hold - dt);
        public override void Tap(PointF p)
        {
            // ordre cap a l'avió; amb el simulador connectat, el llum el decideix l'avió (aquí només l'efecte de prémer)
            Owner.RaiseCommand(Id, "PUSH");
            if (Momentary || Owner.SimDriven) { _hold = 0.16f; Changed(); return; }
            if (Group != null) { if (On) return; On = true; Owner.ClearGroup(Group, this); }
            else On = !On;
            Changed();
        }
        /// <summary>Estil del panell principal (marc fosc, cara enfonsada, barra de llum centrada).</summary>
        public bool Mip;

        public override void Draw(Graphics g)
        {
            var r = Bounds;
            bool pressed = _hold > 0;
            if (Mip)
            {
                var mf = Oh.MipFrame(g, r);
                if (pressed) using (var db = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillRectangle(db, mf);
                float ty = mf.Y + mf.Height / 2f;
                if (Led)
                {
                    float bw = mf.Width * 0.62f, bh = Math.Max(1.1f, mf.Height * 0.09f);
                    var bar = new RectangleF(mf.X + (mf.Width - bw) / 2, mf.Y + mf.Height * 0.14f, bw, bh);
                    using (var bk = new SolidBrush(Color.FromArgb(18, 20, 22))) g.FillRectangle(bk, RectangleF.Inflate(bar, 0.35f, 0.35f));
                    if (On)
                    {
                        using (var gl = new SolidBrush(Color.FromArgb(70, 92, 240, 138))) g.FillRectangle(gl, RectangleF.Inflate(bar, 0.9f, 0.9f));
                        using (var lb = new SolidBrush(Color.FromArgb(92, 240, 138))) g.FillRectangle(lb, bar);
                    }
                    else using (var lb = new SolidBrush(Color.FromArgb(42, 51, 45))) g.FillRectangle(lb, bar);
                    ty = mf.Y + mf.Height * 0.62f;
                }
                if (Label.Length > 0)
                {
                    float avail = Led ? mf.Height * 0.5f : mf.Height * 0.62f;
                    float size = Math.Min(avail, mf.Width / (Label.Length * 0.6f + 0.5f)) * 0.85f;
                    Oh.Text(g, Label, mf.X + mf.Width / 2, ty, size, Color.FromArgb(240, 242, 244));
                }
                return;
            }
            using (var sp = Oh.RoundRect(new RectangleF(r.X + 1, r.Y + 1.5f, r.Width, r.Height), 2.5f))
            using (var sb = new SolidBrush(Color.FromArgb(100, 15, 25, 35))) g.FillPath(sb, sp);
            var f = pressed ? RectangleF.Inflate(r, -0.6f, -0.6f) : r;
            using (var bp = Oh.RoundRect(f, 2.5f))
            {
                using (var bb = new LinearGradientBrush(f, Color.FromArgb(70, 74, 78), Color.FromArgb(20, 22, 24), 90f)) g.FillPath(bb, bp);
                using var pe = new Pen(Color.FromArgb(12, 12, 12), 0.6f); g.DrawPath(pe, bp);
            }
            if (Led)
            {
                // LedWide: barra de llum a tota l'amplada de dalt (tecles del FCU / EFIS)
                var led = LedWide ? new RectangleF(f.X + 4, f.Y + 2.4f, f.Width - 8, 2.4f)
                                  : new RectangleF(f.X + 3, f.Y + 2.2f, Math.Min(7f, f.Width * 0.25f), 2.6f);
                using var lb = new SolidBrush(On ? Oh.Green : Color.FromArgb(40, 70, 50));
                g.FillRectangle(lb, led);
            }
            if (Arrows)
            {
                using var ap = new Pen(Oh.Green, 1.6f);
                float cx = f.X + f.Width / 2, cy = f.Y + f.Height / 2, hw = f.Width * 0.3f;
                using var cap = new AdjustableArrowCap(2.6f, 2.6f);
                ap.CustomEndCap = cap;
                g.DrawLine(ap, cx - hw, cy - 3, cx + hw, cy - 3);
                g.DrawLine(ap, cx + hw, cy + 3, cx - hw, cy + 3);
            }
            if (Label.Length > 0)
            {
                Color tc = LitText ? Oh.Lerp(Color.FromArgb(96, 100, 104), Oh.Amber, Owner.Level(On ? 1 : 0, false)) : Oh.Silk;
                float size = Math.Min(f.Height * 0.48f, (f.Width - 4) / (Label.Length * 0.64f + 0.2f));
                Oh.Text(g, Label, f.X + f.Width / 2, f.Y + f.Height / 2 + (Led ? 1.2f : 0.2f), Math.Min(size, 7.2f), tc);
            }
        }
    }

    /// <summary>Potenciòmetre de volum ACP: es prem per treure'l (anell il·luminat) o tornar-lo a ficar.</summary>
    internal sealed class OhVolKnob : OhElement
    {
        readonly float _cx, _cy, _r; public bool Out; float _o;
        public OhVolKnob(string id, float cx, float cy, float r)
        {
            Id = id; _cx = cx; _cy = cy; _r = r; Bounds = new RectangleF(cx - r - 2, cy - r - 2, r * 2 + 4, r * 2 + 4);
        }
        public override int State => Out ? 1 : 0;
        public override bool Animating => Math.Abs(_o - (Out ? 1 : 0)) > 0.001f;
        public override void Tick(float dt) => _o = Approach(_o, Out ? 1 : 0, dt * 9f);
        public override void Tap(PointF p) { Out = !Out; Changed(); }
        public override void Draw(Graphics g)
        {
            float r = _r * (1f + 0.08f * _o);
            var o = new RectangleF(_cx - r, _cy - r, r * 2, r * 2);
            using (var sb = new SolidBrush(Color.FromArgb((int)(90 + 40 * _o), 15, 25, 35))) g.FillEllipse(sb, o.X + 1.2f + _o, o.Y + 1.8f + _o, o.Width, o.Height);
            using (var lg = new LinearGradientBrush(o, Color.FromArgb(230, 234, 238), Color.FromArgb(110, 116, 122), 50f)) g.FillEllipse(lg, o);
            using (var p = new Pen(Color.FromArgb(70, 76, 82), 0.7f)) g.DrawEllipse(p, o);
            var ring = RectangleF.Inflate(o, -r * 0.3f, -r * 0.3f);
            using (var rp = new Pen(Oh.Lerp(Color.FromArgb(70, 74, 78), Color.FromArgb(255, 255, 255), Owner.Level(_o, false)), Math.Max(1f, r * 0.16f))) g.DrawEllipse(rp, ring);
        }
    }

    // ═══════════════════════════ ELEMENTS DEL PANELL PRINCIPAL ═══════════════════════════════

    /// <summary>Pantalla apagada (PFD, ND, ECAM, ISIS, DCDU…): marc metàl·lic amb relleu i vidre negre.</summary>
    internal sealed class OhScreen : OhSilk
    {
        readonly RectangleF _outer, _glass; readonly float _radius; readonly bool _screws;
        public OhScreen(RectangleF outer, RectangleF glass, float radius, bool screws)
        {
            _outer = outer; _glass = glass; _radius = radius; _screws = screws;
            Bounds = RectangleF.Inflate(outer, 5, 5);
        }
        public override void Draw(Graphics g)
        {
            using (var op = Oh.RoundRect(_outer, _radius))
            {
                Oh.SoftShadow(g, op, 1.6f, 2.4f, 3f, 130);
                using (var ob = new LinearGradientBrush(_outer, Oh.T(132, 146, 160, 128, 156, 178), Oh.T(74, 86, 98, 68, 92, 112), 60f)) g.FillPath(ob, op);
                using var pe = new Pen(Color.FromArgb(40, 48, 56), 0.8f); g.DrawPath(pe, op);
            }
            // llavi interior fosc al voltant del vidre
            var lip = RectangleF.Inflate(_glass, 2.2f, 2.2f);
            using (var lp = Oh.RoundRect(lip, 2.5f))
            using (var lb = new LinearGradientBrush(lip, Oh.T(24, 28, 32, 22, 30, 38), Oh.T(96, 108, 120, 90, 114, 134), 90f)) g.FillPath(lb, lp);
            using (var gb = new SolidBrush(Color.FromArgb(6, 7, 8))) g.FillRectangle(gb, _glass);
            // reflex molt suau del vidre
            using (var rb = new LinearGradientBrush(_glass, Color.FromArgb(22, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 35f))
                g.FillRectangle(rb, new RectangleF(_glass.X, _glass.Y, _glass.Width, _glass.Height * 0.45f));
            if (_screws)
                foreach (var p in new[] { new PointF(_outer.X + 6, _outer.Y + 6), new PointF(_outer.Right - 6, _outer.Y + 6),
                                          new PointF(_outer.X + 6, _outer.Bottom - 6), new PointF(_outer.Right - 6, _outer.Bottom - 6) })
                    Oh.Dzus(g, p.X, p.Y, 3.6f, 30f + (p.X * 3.1f + p.Y * 1.7f) % 40f);
        }
    }

    /// <summary>
    /// Marc de les pantalles grans (PFD, ND, ECAM), model validat a la pàgina "Marcs de pantalla": visera superior
    /// que sobresurt i fa ombra sobre el vidre, muntants laterals amb relleu, vidre enfonsat, topalls amb cargol a
    /// les cantonades de baix. El vidre manté exactament la mida de sempre. Mides de la maqueta en unitats de
    /// referència (×U per passar a unitats del panell).
    /// </summary>
    internal sealed class OhBezel : OhSilk
    {
        // valors triats per en Pere (2026-11-20)
        const float Hood = 2.00f, HoodShadow = 1.00f, Pillars = 1.35f, GlassDepth = 1.00f, Dark = 0.90f, Boss = 1.50f;
        const float U = 0.625f;
        readonly RectangleF _o, _g;

        public OhBezel(RectangleF outer, RectangleF glass)
        {
            _o = outer; _g = glass;
            float m = (2 * Hood + 8) * U;
            Bounds = RectangleF.FromLTRB(outer.Left - m, outer.Top - m, outer.Right + m, outer.Bottom + m + 6);
        }

        static Color C(float r, float g, float b, float f, int a = 255) =>
            Color.FromArgb(a, Math.Clamp((int)(r * f), 0, 255), Math.Clamp((int)(g * f), 0, 255), Math.Clamp((int)(b * f), 0, 255));

        static void VFill(Graphics g, RectangleF r, Color c0, Color c1)
        {
            if (r.Height <= 0.01f || r.Width <= 0.01f) return;
            using var b = new LinearGradientBrush(RectangleF.Inflate(r, 0, 0.5f), c0, c1, 90f);
            g.FillRectangle(b, r);
        }

        public override void Draw(Graphics g)
        {
            const float fr = 92 * Dark, fg = 104 * Dark, fb = 116 * Dark;   // color del marc
            Color F(float f, int a = 255) => C(fr, fg, fb, f, a);
            float ox0 = _o.Left, oy0 = _o.Top, ox1 = _o.Right, oy1 = _o.Bottom;
            float gx0 = _g.Left, gy0 = _g.Top, gx1 = _g.Right, gy1 = _g.Bottom;

            // 1) cos del marc
            using (var body = Oh.RoundRect(_o, 5 * U))
            {
                Oh.SoftShadow(g, body, 1.5f * U, 3f * U, 4.5f * U, 140);
                using (var bb = new LinearGradientBrush(RectangleF.Inflate(_o, 0, 0.5f), F(1.08f), F(0.90f), 90f)) g.FillPath(bb, body);
                Oh.Grain(g, body);
            }
            using (var ip = Oh.RoundRect(RectangleF.Inflate(_o, -0.6f * U, -0.6f * U), 5 * U))
            using (var pe = new Pen(F(1.32f, 178), 0.9f * U)) g.DrawPath(pe, ip);

            // 2) muntants laterals amb relleu
            foreach (var (x0, x1) in new[] { (ox0 + 1.5f * U, gx0 - 1 * U), (gx1 + 1 * U, ox1 - 1.5f * U) })
            {
                var pr = RectangleF.FromLTRB(x0, gy0 - 2 * U, x1, gy1 + 2 * U);
                if (pr.Width <= 0.1f) continue;
                using var lb = new LinearGradientBrush(RectangleF.Inflate(pr, 0.5f, 0), Color.Black, Color.Black, 0f)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { F(1 + 0.30f * Pillars), F(1f), F(1 - 0.38f * Pillars) },
                        Positions = new[] { 0f, 0.45f, 1f }
                    }
                };
                g.FillRectangle(lb, pr);
            }

            // 3) vidre enfonsat
            float st = 4 * GlassDepth * U;
            var step = RectangleF.FromLTRB(gx0 - st, gy0 - st, gx1 + st, gy1 + st);
            using (var sb = new SolidBrush(Color.FromArgb(13, 17, 21))) g.FillRectangle(sb, step);
            VFill(g, step, Color.FromArgb(153, 0, 0, 0), Color.FromArgb(89, 110, 128, 146));
            using (var gb = new SolidBrush(Color.FromArgb(5, 7, 10))) g.FillRectangle(gb, _g);
            using (var lp = new Pen(F(1.32f, 204), 0.8f * U)) g.DrawLine(lp, gx0 - st, gy1 + st - 0.4f * U, gx1 + st, gy1 + st - 0.4f * U);

            // 4) visera superior
            float hy0 = oy0 + 1 * U, hy1 = gy0 - st + 1 * U, hx0 = ox0 - 2 * Hood * U, hx1 = ox1 + 2 * Hood * U;
            var hood = RectangleF.FromLTRB(hx0, hy0, hx1, hy1);
            if (hood.Height > 0.3f)
            {
                using (var hp = Oh.RoundRect(hood, 3 * U))
                {
                    Oh.SoftShadow(g, hp, 0, 2.5f * Hood * U, 3f * Hood * U, 150);
                    using (var hb = new LinearGradientBrush(RectangleF.Inflate(hood, 0, 0.5f), F(1.35f), F(0.82f), 90f)) g.FillPath(hb, hp);
                    Oh.Grain(g, hp);
                }
                using (var tl = new Pen(Color.FromArgb(166, 220, 232, 242), 0.9f * U)) g.DrawLine(tl, hx0 + 3 * U, hy0 + 0.7f * U, hx1 - 3 * U, hy0 + 0.7f * U);
                VFill(g, RectangleF.FromLTRB(hx0 + 1 * U, hy1 - 1.6f * U, hx1 - 1 * U, hy1 + 0.6f * U), F(0.62f), Color.FromArgb(230, 0, 0, 0));
            }

            // 5) ombra de la visera sobre el vidre i reflex suau
            float shh = (gy1 - gy0) * 0.09f * HoodShadow * Math.Max(0.4f, Hood);
            VFill(g, RectangleF.FromLTRB(gx0, gy0, gx1, gy0 + shh), Color.FromArgb((int)(255 * Math.Min(0.85f, 0.7f * HoodShadow)), 0, 0, 0), Color.FromArgb(0, 0, 0, 0));
            VFill(g, RectangleF.FromLTRB(gx0, gy0, gx1, gy0 + (gy1 - gy0) * 0.5f), Color.FromArgb(9, 255, 255, 255), Color.FromArgb(0, 255, 255, 255));

            // 6) topalls quadrats amb cargol a les cantonades de baix
            float bsz = 13 * Boss * U, pw = gx0 - ox0;
            foreach (float cx in new[] { ox0 + pw / 2, ox1 - pw / 2 })
            {
                var br = new RectangleF(cx - bsz / 2, oy1 - bsz - 1.5f * U, bsz, bsz);
                using (var bp = Oh.RoundRect(br, 2 * U))
                {
                    Oh.SoftShadow(g, bp, 0.8f * U, 1.4f * U, 1.6f * U, 150);
                    using (var bb = new LinearGradientBrush(RectangleF.Inflate(br, 0, 0.5f), F(1.20f), F(0.78f), 90f)) g.FillPath(bb, bp);
                }
                using (var bp2 = Oh.RoundRect(RectangleF.Inflate(br, -0.5f * U, -0.5f * U), 2 * U))
                using (var pe = new Pen(F(1.32f, 153), 0.6f * U)) g.DrawPath(pe, bp2);
                float r = bsz * 0.26f, cy = br.Y + bsz / 2;
                using (var rb = new SolidBrush(Color.FromArgb(18, 24, 30))) g.FillEllipse(rb, cx - r - 0.9f * U, cy - r - 0.9f * U, (r + 0.9f * U) * 2, (r + 0.9f * U) * 2);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(cx - r, cy - r, r * 2, r * 2);
                    using var pgb = new PathGradientBrush(path)
                    {
                        CenterColor = Color.FromArgb(201, 208, 214),
                        SurroundColors = new[] { Color.FromArgb(62, 70, 78) },
                        CenterPoint = new PointF(cx - r * 0.35f, cy - r * 0.4f)
                    };
                    g.FillPath(pgb, path);
                }
                using (var hb = new SolidBrush(Color.FromArgb(27, 32, 38))) g.FillEllipse(hb, cx - r * 0.42f, cy - r * 0.42f, r * 0.84f, r * 0.84f);
            }
        }
    }

    /// <summary>Peça fosca genèrica (zona dels genolls, safates, tubs): degradat vertical i opcionalment reixeta.</summary>
    internal sealed class OhBlock : OhSilk
    {
        readonly RectangleF _r; readonly Color _top, _bottom; readonly float _radius; readonly int _grill;
        public OhBlock(RectangleF r, Color top, Color bottom, float radius, int grill = 0)
        {
            _r = r; _top = top; _bottom = bottom; _radius = radius; _grill = grill;
            Bounds = RectangleF.Inflate(r, 5, 5);
        }
        public override void Draw(Graphics g)
        {
            using var p = Oh.RoundRect(_r, _radius);
            Oh.SoftShadow(g, p, 1.4f, 2.2f, 2.6f, 120);
            using (var b = new LinearGradientBrush(RectangleF.Inflate(_r, 0.5f, 0.5f), _top, _bottom, 90f)) g.FillPath(b, p);
            using (var hp = new Pen(Color.FromArgb(70, 255, 255, 255), 0.6f)) g.DrawLine(hp, _r.X + _radius, _r.Y + 0.6f, _r.Right - _radius, _r.Y + 0.6f);
            for (int i = 0; i < _grill; i++)
            {
                float y = _r.Y + _r.Height * 0.12f + i * 2.4f;
                using var gp = new Pen(Color.FromArgb(150, 150, 154, 160), 0.8f);
                g.DrawLine(gp, _r.X + 3, y, _r.Right - 3, y);
            }
        }
    }

    /// <summary>Polígon fosc amb degradat vertical (vora del glareshield, llavi inferior, etc.).</summary>
    internal sealed class OhPoly : OhSilk
    {
        readonly PointF[] _pts; readonly Color _top, _bottom; readonly bool _shadow;
        public OhPoly(PointF[] pts, Color top, Color bottom, bool shadow)
        {
            _pts = pts; _top = top; _bottom = bottom; _shadow = shadow;
            Bounds = RectangleF.Inflate(RectangleF.FromLTRB(pts.Min(p => p.X), pts.Min(p => p.Y), pts.Max(p => p.X), pts.Max(p => p.Y)), 5, 5);
        }
        public override void Draw(Graphics g)
        {
            using var path = new GraphicsPath();
            path.AddPolygon(_pts);
            if (_shadow) Oh.SoftShadow(g, path, 1.2f, 2.4f, 3f, 120);
            var b = path.GetBounds();
            using var br = new LinearGradientBrush(RectangleF.Inflate(b, 0.5f, 0.5f), _top, _bottom, 90f);
            g.FillPath(br, path);
        }
    }

    /// <summary>
    /// Visera del glareshield de pell negra: segueix una línia trencada (tram inclinat + horitzontal) amb un gruix
    /// vertical; el perfil és arrodonit (com un tub entapissat): reflex de llum a la part alta, fosca a les vores, i
    /// textura de pell (gra irregular). Es dibuixa per franges paral·leles a la línia, perquè el degradat del perfil
    /// segueixi també el tram inclinat.
    /// </summary>
    internal sealed class OhVisor : OhSilk
    {
        readonly PointF[] _top; readonly float _th;
        static TextureBrush? _leather;

        public OhVisor(PointF[] top, float thickness)
        {
            _top = top; _th = thickness;
            Bounds = RectangleF.Inflate(RectangleF.FromLTRB(top.Min(p => p.X), top.Min(p => p.Y), top.Max(p => p.X), top.Max(p => p.Y) + thickness), 6, 8);
        }

        /// <summary>
        /// Color del perfil per il·luminació real d'una secció arrodonida (mitja canya): t = 0 (dalt) .. 1 (baix).
        /// Pell negra setinada: difusa baixa + especular ampla i suau (no un reflex dur de plàstic).
        /// </summary>
        static (float r, float g, float b) Shade(float t)
        {
            float a = MathF.PI * (Math.Clamp(t, 0f, 1f) - 0.5f);       // -90° (mira amunt) .. +90° (mira avall)
            float ny = MathF.Sin(a), nz = MathF.Cos(a);                  // normal (y avall, z cap a l'observador)
            const float lyy = -0.62f, lz = 0.78f;                         // llum de dalt i de davant
            float diff = Math.Max(0f, ny * lyy + nz * lz);
            float hy = lyy, hz = lz + 1f; float hn = MathF.Sqrt(hy * hy + hz * hz); hy /= hn; hz /= hn;
            float nh = Math.Max(0f, ny * hy + nz * hz);
            float spec = 0.55f * MathF.Pow(nh, 9f) + 0.25f * MathF.Pow(nh, 40f);   // lluentor setinada
            float fres = MathF.Pow(1f - nz, 3f) * 0.18f;                 // vores una mica més clares (llum rasant)
            float baseL = 0.12f + 0.55f * diff;
            float r = 26f * baseL * 2.2f + 175f * spec + 40f * fres;
            float gg = 26f * baseL * 2.2f + 178f * spec + 42f * fres;
            float b = 29f * baseL * 2.2f + 186f * spec + 46f * fres;
            return (r, gg, b);
        }

        /// <summary>Gra de pell: soroll de dues octaves (cèl·lules irregulars) convertit en relleu amb llum de dalt-esquerra.</summary>
        static TextureBrush Leather()
        {
            if (_leather != null) return _leather;
            const int n = 128;
            var rnd = new Random(23);
            float[] Blur(float[] src, int rad)
            {
                var o = new float[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float sum = 0; int c = 0;
                        for (int dy = -rad; dy <= rad; dy++) for (int dx = -rad; dx <= rad; dx++) { sum += src[((y + dy + n) % n) * n + (x + dx + n) % n]; c++; }
                        o[y * n + x] = sum / c;
                    }
                return o;
            }
            var w = new float[n * n];
            for (int i = 0; i < w.Length; i++) w[i] = (float)rnd.NextDouble();
            var lo = Blur(w, 3); var hi = Blur(w, 1);
            var h = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                // "porus" de pell: valls estretes entre petits coixinets
                float v = 0.65f * lo[i] + 0.35f * hi[i];
                h[i] = MathF.Pow(Math.Clamp((v - 0.35f) * 3.3f, 0f, 1f), 0.7f);
            }
            var bmp = new Bitmap(n, n, PixelFormat.Format32bppArgb);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // relleu isòtrop (sense direcció preferent): el punt respecte a la mitjana dels 4 veïns
                    float avg = (h[((y - 1 + n) % n) * n + x] + h[((y + 1) % n) * n + x] + h[y * n + (x - 1 + n) % n] + h[y * n + (x + 1) % n]) / 4f;
                    float d = h[y * n + x] - avg;
                    float cav = 1f - h[y * n + x];
                    int aLight = (int)Math.Clamp(d * 260f, 0f, 34f);
                    int aDark = (int)Math.Clamp(-d * 300f + cav * 26f, 0f, 60f);
                    bmp.SetPixel(x, y, aLight > aDark ? Color.FromArgb(aLight, 255, 255, 255) : Color.FromArgb(aDark, 0, 0, 0));
                }
            _leather = new TextureBrush(bmp, WrapMode.Tile);
            return _leather;
        }

        static TextureBrush? _mottle;
        /// <summary>Variació de to molt suau i gran (la pell no és mai d'un negre uniforme).</summary>
        static TextureBrush Mottle()
        {
            if (_mottle != null) return _mottle;
            var rnd = new Random(5);
            const int n = 32;
            var bmp = new Bitmap(n, n, PixelFormat.Format32bppArgb);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = (float)rnd.NextDouble() * 2 - 1;
                    bmp.SetPixel(x, y, v > 0 ? Color.FromArgb((int)(v * 5), 255, 255, 255) : Color.FromArgb((int)(-v * 8), 0, 0, 0));
                }
            _mottle = new TextureBrush(bmp, WrapMode.Tile);
            return _mottle;
        }

        public override void Draw(Graphics g)
        {
            var bottom = _top.Select(p => new PointF(p.X, p.Y + _th)).ToArray();
            var outline = _top.Concat(bottom.Reverse()).ToArray();
            using var path = new GraphicsPath();
            path.AddPolygon(outline);
            // ombra suau i ampla sobre els panells de sota (la visera sobresurt)
            Oh.SoftShadow(g, path, 0.6f, 3.4f, 4.5f, 170);
            var st = g.Save();
            g.SetClip(path, CombineMode.Intersect);
            var oldIm = g.InterpolationMode;
            // perfil arrodonit il·luminat, en franges paral·leles a la línia trencada
            const int N = 64;
            for (int i = 0; i < N; i++)
            {
                float t = (i + 0.5f) / N;
                var (r, gg, b) = Shade(t);
                var line = _top.Select(p => new PointF(p.X, p.Y + _th * t)).ToArray();
                using var pen = new Pen(Color.FromArgb(Math.Clamp((int)r, 0, 255), Math.Clamp((int)gg, 0, 255), Math.Clamp((int)b, 0, 255)), _th / N + 0.5f) { LineJoin = LineJoin.Round };
                g.DrawLines(pen, line);
            }
            // variació de to gran i suau
            var mb = Mottle();
            mb.ResetTransform(); mb.ScaleTransform(14f, 14f);
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.FillPath(mb, path);
            // gra de pell fi, escalat a píxels reals
            var lb = Leather();
            lb.ResetTransform(); lb.ScaleTransform(0.9f / Oh.PixelScale, 0.9f / Oh.PixelScale);
            g.FillPath(lb, path);
            g.InterpolationMode = oldIm;
            // costura: punts de fil fosc a prop de la vora de baix, amb el seu petit relleu
            float ty = _th * 0.80f;
            var seam = _top.Select(p => new PointF(p.X, p.Y + ty)).ToArray();
            using (var groove = new Pen(Color.FromArgb(120, 0, 0, 0), _th * 0.10f) { LineJoin = LineJoin.Round })
                g.DrawLines(groove, seam);
            float stitch = Math.Max(0.9f, _th * 0.09f);
            using (var thread = new Pen(Color.FromArgb(150, 92, 92, 96), Math.Max(0.35f, _th * 0.045f)) { DashPattern = new[] { 2.2f, 1.6f }, LineJoin = LineJoin.Round })
                g.DrawLines(thread, seam.Select(p => new PointF(p.X, p.Y - stitch * 0.15f)).ToArray());
            g.Restore(st);
            // vora de dalt: fil de llum rasant; vora de baix: plec fosc on la pell es gira cap a dins
            using (var tp = new Pen(Color.FromArgb(70, 170, 176, 186), 0.6f) { LineJoin = LineJoin.Round })
                g.DrawLines(tp, _top.Select(p => new PointF(p.X, p.Y + 0.4f)).ToArray());
            using (var ep = new Pen(Color.FromArgb(220, 3, 3, 4), 1.0f) { LineJoin = LineJoin.Round })
                g.DrawLines(ep, bottom);
        }
    }

    /// <summary>
    /// Ombra que la visera projecta sobre el mòdul esquerre, l'EFIS i el FCU (model validat a la pàgina "Ombra de la
    /// visera"). És un element per sobre dels botons (no rep tocs), perquè l'ombra també els cobreixi.
    /// </summary>
    internal sealed class OhVisorShadow : OhElement
    {
        // valors triats per en Pere (2026-11-20), en unitats de la imatge de referència
        const float Len = 60f, Softness = 1.45f, Offset = 12f;
        readonly float Darkness;   // 0.60 (franja); la visera +25% = 0.75
        readonly PointF[] _edge;   // vora de baix de la visera (unitats del panell)
        readonly float _k;

        public OhVisorShadow(PointF[] edge, float k, string id, float darkness = 0.60f)
        {
            Id = id; Z = 50; _edge = edge; _k = k; Darkness = darkness;
            float ext = (Len + Offset) * k + 2;
            Bounds = RectangleF.FromLTRB(edge.Min(p => p.X), edge.Min(p => p.Y) - 1, edge.Max(p => p.X), edge.Max(p => p.Y) + ext);
        }
        public override bool HitTest(PointF p) => false;
        public override RectangleF DirtyBounds => Bounds;

        PointF[] Strip(float y0, float y1) =>
            _edge.Select(p => new PointF(p.X, p.Y + y0)).Concat(_edge.Reverse().Select(p => new PointF(p.X, p.Y + y1))).ToArray();

        public override void Draw(Graphics g)
        {
            // Degradat continu (sense franges): per a cada tram de la vora, un rectangle amb degradat vertical
            // deformat (shear) perquè segueixi la inclinació del tram.
            float off = Offset * _k, total = (Offset + Len) * _k;
            const int S = 24;
            var cols = new Color[S + 2]; var pos = new float[S + 2];
            int near = (int)(255 * Math.Min(1f, Darkness * 1.1f));
            cols[0] = Color.FromArgb(near, 0, 0, 0); pos[0] = 0f;
            for (int i = 0; i <= S; i++)
            {
                float t = i / (float)S;
                int a = (int)Math.Round(255 * Math.Min(1f, Darkness * MathF.Pow(1 - t, Softness)));
                cols[i + 1] = Color.FromArgb(a, 0, 0, 0);
                pos[i + 1] = (off + Len * _k * t) / total;
            }
            pos[1] = Math.Max(pos[1], 0.0001f);
            // sense suavitzat de vores: els trams s'ajunten píxel a píxel (sense costura vertical), i el patró del
            // degradat en mirall (TileFlipXY) evita que la vora de baix torni a començar fosca (costura horitzontal)
            var old = g.SmoothingMode; var oldPo = g.PixelOffsetMode;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            for (int i = 0; i < _edge.Length - 1; i++)
            {
                var p0 = _edge[i]; var p1 = _edge[i + 1];
                if (p1.X - p0.X < 0.01f) continue;
                float slope = (p1.Y - p0.Y) / (p1.X - p0.X);
                var st = g.Save();
                using (var m = new Matrix(1, slope, 0, 1, 0, p0.Y - slope * p0.X)) g.MultiplyTransform(m);
                var rect = new RectangleF(p0.X, 0, p1.X - p0.X, total);
                using (var br = new LinearGradientBrush(new RectangleF(rect.X, 0, rect.Width, total), Color.Black, Color.Transparent, 90f)
                       { InterpolationColors = new ColorBlend { Colors = cols, Positions = pos }, WrapMode = WrapMode.TileFlipXY })
                    g.FillRectangle(br, rect);
                g.Restore(st);
            }
            g.SmoothingMode = old; g.PixelOffsetMode = oldPo;
        }
    }

    /// <summary>
    /// Cantell de la carcassa on van encastats els mòduls del glareshield (model validat a la pàgina "Cantell de la
    /// carcassa"): banda blau fosc, fosca a dalt (a l'ombra dels mòduls, no sobresurt), s'aclareix cap a baix i acaba
    /// amb un plec fosc sobre la franja negra.
    /// </summary>
    internal sealed class OhCasingEdge : OhSilk
    {
        // valors per defecte de la maqueta (2026-11-20)
        const float Bright = 0.62f, TopDark = 0.70f, Fold = 0.80f;
        readonly RectangleF _r;
        public OhCasingEdge(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 1, 3); }
        static Color S(float f) => Color.FromArgb(Math.Clamp((int)(78 * Bright * f), 0, 255), Math.Clamp((int)(100 * Bright * f), 0, 255), Math.Clamp((int)(122 * Bright * f), 0, 255));
        public override void Draw(Graphics g)
        {
            using (var b = new LinearGradientBrush(RectangleF.Inflate(_r, 0, 0.5f), Color.Black, Color.Black, 90f)
            {
                InterpolationColors = new ColorBlend { Colors = new[] { S(1 - 0.8f * TopDark), S(1f), S(0.85f) }, Positions = new[] { 0f, 0.55f, 1f } }
            }) g.FillRectangle(b, _r);
            using (var path = new GraphicsPath()) { path.AddRectangle(_r); Oh.Grain(g, path); }
            float fh = 4.5f * 0.625f;
            var fold = new RectangleF(_r.X, _r.Bottom - 1.5f * 0.625f, _r.Width, fh);
            using (var fb = new LinearGradientBrush(RectangleF.Inflate(fold, 0, 0.3f), Color.FromArgb((int)(255 * 0.85f * Fold), 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 90f))
                g.FillRectangle(fb, fold);
        }
    }

    /// <summary>Cargol Dzus aïllat (fora dels angles d'una planxa).</summary>
    internal sealed class OhScrewSilk : OhSilk
    {
        readonly float _x, _y, _r;
        public OhScrewSilk(float x, float y, float r) { _x = x; _y = y; _r = r; Bounds = new RectangleF(x - r - 4, y - r - 4, r * 2 + 8, r * 2 + 8); }
        public override void Draw(Graphics g) => Oh.Dzus(g, _x, _y, _r, 35f);
    }

    /// <summary>Sortida d'aire del glareshield (petit difusor fosc amb reixeta).</summary>
    internal sealed class OhVent : OhSilk
    {
        readonly float _cx, _cy, _w, _h;
        public OhVent(float cx, float cy, float w, float h) { _cx = cx; _cy = cy; _w = w; _h = h; Bounds = new RectangleF(cx - w / 2 - 4, cy - h / 2 - 4, w + 8, h + 8); }
        public override void Draw(Graphics g)
        {
            var poly = new[] { new PointF(_cx - _w / 2, _cy - _h / 2), new PointF(_cx + _w / 2, _cy - _h / 2), new PointF(_cx + _w * 0.32f, _cy + _h / 2), new PointF(_cx - _w * 0.32f, _cy + _h / 2) };
            using (var sp = new GraphicsPath()) { sp.AddPolygon(poly); Oh.SoftShadow(g, sp, 1f, 1.6f, 1.8f, 120); }
            using (var b = new LinearGradientBrush(new RectangleF(_cx - _w / 2, _cy - _h / 2, _w, _h), Color.FromArgb(70, 72, 76), Color.FromArgb(18, 18, 20), 90f)) g.FillPolygon(b, poly);
            using var p = new Pen(Color.FromArgb(120, 150, 152, 156), 0.6f);
            for (int i = 1; i < 4; i++) { float y = _cy - _h / 2 + _h * i / 4f; float hw = _w / 2 - (_w * 0.18f) * i / 4f; g.DrawLine(p, _cx - hw + 1, y, _cx + hw - 1, y); }
        }
    }

    /// <summary>
    /// Rotatiu doble del QNH: base negra estriada de 2 posicions (índex blanc a "in Hg" o "hPa") i, al mig, un pom
    /// blanc "PULL STD" (es prem/estira). Tocar l'anell exterior canvia la unitat; tocar el pom, PULL STD.
    /// </summary>
    internal enum KnobFace { Baro, Alt, Spd, Hdg }

    internal sealed class OhBaroKnob : OhElement
    {
        readonly float _cx, _cy, _r;
        readonly float _zw;     // mig ample de les zones de toc (en radis)
        readonly bool _alt;     // variant ALT del FCU: pom sense lletres amb estriat de 8 ones; anell 100 / 1000
        readonly KnobFace _face;
        /// <summary>Rotatiu simple (SPD / HDG): sense anell negre de selecció; només 4 zones de toc.</summary>
        bool Simple => _face == KnobFace.Spd || _face == KnobFace.Hdg;
        /// <summary>
        /// PUSH i PULL comparteixen una sola zona (la columna del mig). Toc curt = PUSH;
        /// dit quiet més d'1 s = PULL (en complir-se el segon, sense esperar a aixecar el dit). Si el dit es mou
        /// és un arrossegament: no fa res. Les zones de gir (esquerra / dreta) es mantenen igual.
        /// </summary>
        public bool SingleZone = true;
        const float PullHold = 1f;
        bool _pullArmed, _pullDone; double _pullStart;
        RectangleF ZOne => RectangleF.FromLTRB(_cx - _r * (_zw / 3f), _cy - _r * 1.45f, _cx + _r * (_zw / 3f), _cy + _r * 1.31f);
        public bool Hpa = true, Std;
        // pom momentani: repòs al mig (0); PUSH → endins (-1) 0,25 s; PULL → enfora (+1) 0,25 s
        float _ang, _push, _pushTarget, _pushHold;
        const float HoldTime = 0.25f;
        // gir del pom PULL STD: cada toc a les zones de sota in Hg / hPa el gira 10º (esquerra / dreta)
        float _knob, _knobTarget;
        const float KnobStep = 10f;
        // mantenint el dit a les zones de gir més de 0,6 s: gir continu a 50º/s fins que s'aixeca
        // (temps real, no dt: el tic d'animació limita dt i el gir quedaria curt)
        static readonly System.Diagnostics.Stopwatch s_clock = System.Diagnostics.Stopwatch.StartNew();
        int _holdDir; float _holdT, _holdBase; double _holdStart; bool _skipTap;
        int _stepsSent;   // passos de 10º ja enviats a l'avió (cada pas = 1 INC / DEC)

        /// <summary>Envia un INC / DEC per cada pas de 10º que el pom hagi girat des de l'últim enviament.</summary>
        void SendSteps()
        {
            int steps = (int)Math.Floor(_knobTarget / KnobStep + 1e-4f);
            while (_stepsSent < steps) { _stepsSent++; Owner.RaiseCommand(Id, "INC"); }
            while (_stepsSent > steps) { _stepsSent--; Owner.RaiseCommand(Id, "DEC"); }
        }
        const float HoldDelay = 0.6f, HoldSpeed = 100f;
        // zones de toc: "in Hg" i "hPa" (rètols de dalt) giren l'anell; entre els dos rètols = PUSH; meitat de baix del rotatiu = PULL
        // tres franges d'amplada igual (4r / 3 cadascuna)
        // in Hg i hPa: pujades un 50% de la seva alçada (1,45r) respecte de PUSH
        RectangleF ZInHg => RectangleF.FromLTRB(_cx - _r * _zw, _cy - _r * 2.175f, _cx - _r * (_zw / 3f), _cy - _r * 0.725f);
        RectangleF ZHpa => RectangleF.FromLTRB(_cx + _r * (_zw / 3f), _cy - _r * 2.175f, _cx + _r * _zw, _cy - _r * 0.725f);
        RectangleF ZPush => RectangleF.FromLTRB(_cx - _r * (_zw / 3f), _cy - _r * 1.45f, _cx + _r * (_zw / 3f), _cy);
        RectangleF ZPull => Simple ? RectangleF.FromLTRB(_cx - _r * (_zw / 3f), _cy, _cx + _r * (_zw / 3f), _cy + _r * 1.31f)
                                    : RectangleF.FromLTRB(_cx - _r * 0.7f, _cy, _cx + _r * 0.7f, _cy + _r * 1.31f);   // baixa fins al mateix nivell que les zones de gir   // 30% més estreta
        // sota in Hg / hPa (mateixa amplada), fins a les tecles FD / LS: giren el pom 10º a l'esquerra / a la dreta
        // (rotatiu simple: les zones de gir ocupen tota l'alçada, de dalt de PUSH a baix de PULL)
        RectangleF ZLeft => RectangleF.FromLTRB(_cx - _r * _zw, _cy - _r * (Simple ? 1.45f : 0.725f), _cx - _r * (_zw / 3f), _cy + _r * 1.31f);
        RectangleF ZRight => RectangleF.FromLTRB(_cx + _r * (_zw / 3f), _cy - _r * (Simple ? 1.45f : 0.725f), _cx + _r * _zw, _cy + _r * 1.31f);
        public override IEnumerable<RectangleF> HitAreas
        {
            get
            {
                if (SingleZone)
                {
                    if (!Simple) { yield return ZInHg; yield return ZHpa; }
                    yield return ZOne; yield return ZLeft; yield return ZRight; yield break;
                }
                if (!Simple) { yield return ZInHg; yield return ZHpa; }
                yield return ZPush; yield return ZLeft; yield return ZRight; yield return ZPull;
            }
        }
        public override bool HitTest(PointF p) => SingleZone ? (!Simple && (ZInHg.Contains(p) || ZHpa.Contains(p))) || ZOne.Contains(p) || ZLeft.Contains(p) || ZRight.Contains(p) : (!Simple && (ZInHg.Contains(p) || ZHpa.Contains(p))) || ZPush.Contains(p) || ZPull.Contains(p) || ZLeft.Contains(p) || ZRight.Contains(p);
        const float InHgAng = -38f, HpaAng = 38f;
        public OhBaroKnob(string id, float cx, float cy, float r, bool alt = false, float zoneHalfWidth = 2f, KnobFace face = KnobFace.Baro)
        {
            _face = alt ? KnobFace.Alt : face;
            Id = id; _cx = cx; _cy = cy; _r = r; _ang = HpaAng; _alt = _face == KnobFace.Alt; _zw = zoneHalfWidth;
            Bounds = new RectangleF(cx - r, cy - r, r * 2, r * 2);
        }
        public override int State => (Hpa ? 1 : 0) + (Std ? 2 : 0);
        float TargetAng => Hpa ? HpaAng : InHgAng;
        public override bool Animating => Math.Abs(_ang - TargetAng) > 0.05f || _pushHold > 0 || Math.Abs(_push - _pushTarget) > 0.001f || Math.Abs(_knob - _knobTarget) > 0.05f || _holdDir != 0 || _pullArmed;
        public override void Tick(float dt)
        {
            _ang = Approach(_ang, TargetAng, dt * 400f);
            if (_pushHold > 0) { _pushHold -= dt; if (_pushHold <= 0) { _pushHold = 0; _pushTarget = 0; } }
            if (_pullArmed && s_clock.Elapsed.TotalSeconds - _pullStart >= PullHold)
            {
                _pullArmed = false; _pullDone = true;
                Std = true; _pushTarget = 1; _pushHold = HoldTime;
                Owner.RaiseCommand(Id, "PULL");
            }
            _push = Approach(_push, _pushTarget, dt * 8f);
            if (_holdDir != 0)
            {
                _holdT = (float)(s_clock.Elapsed.TotalSeconds - _holdStart);
                if (_holdT > HoldDelay)
                {
                    _knobTarget = _holdBase + _holdDir * HoldSpeed * (_holdT - HoldDelay);   // només el temps passat d'1 s
                    _knob = _knobTarget;                                                      // continu, sense retard
                    SendSteps();                                                              // 50º/s = 5 passos/s
                }
            }
            _knob = Approach(_knob, _knobTarget, dt * 200f);
        }
        public override void PressStart(PointF p)
        {
            _pullArmed = SingleZone && ZOne.Contains(p); _pullDone = false;
            _pullStart = s_clock.Elapsed.TotalSeconds;
            _holdDir = ZLeft.Contains(p) ? -1 : ZRight.Contains(p) ? 1 : 0;
            _holdT = 0; _skipTap = false;
            _holdStart = s_clock.Elapsed.TotalSeconds; _holdBase = _knobTarget;
        }
        public override void PressEnd()
        {
            _pullArmed = false;
            // si ja ha girat de manera contínua, en aixecar el dit no s'hi suma el pas de 10º
            if (_holdDir != 0 && s_clock.Elapsed.TotalSeconds - _holdStart > HoldDelay)
            {
                _skipTap = true;
                _knobTarget = _holdBase + _holdDir * HoldSpeed * (float)(s_clock.Elapsed.TotalSeconds - _holdStart - HoldDelay);
                _knob = _knobTarget;
                SendSteps();
            }
            _holdDir = 0;
        }
        public override RectangleF DirtyBounds => RectangleF.Inflate(Bounds, 4 + _r * 0.25f, 4 + _r * 0.25f);   // (simple: anell a 1,18r)
        public override void Tap(PointF p)
        {
            if (SingleZone && ZOne.Contains(p))
            {
                if (_pullDone) { _pullDone = false; return; }   // ja ha fet PULL mentre es mantenia el dit
                Std = false; _pushTarget = -1; _pushHold = HoldTime;
                Owner.RaiseCommand(Id, "PUSH");
                Changed();
                return;
            }
            if (!Simple && ZInHg.Contains(p)) { if (!Hpa) return; Hpa = false; Owner.RaiseCommand(Id, _alt ? "STEP100" : "INHG"); }
            else if (!Simple && ZHpa.Contains(p)) { if (Hpa) return; Hpa = true; Owner.RaiseCommand(Id, _alt ? "STEP1000" : "HPA"); }
            else if (ZPull.Contains(p) || ZPush.Contains(p))
            {
                bool pull = ZPull.Contains(p);
                Std = pull;                       // estirar = STD; prémer = torna a QNH
                _pushTarget = pull ? 1 : -1; _pushHold = HoldTime;
                Owner.RaiseCommand(Id, pull ? "PULL" : "PUSH");
            }
            else if (_skipTap) { _skipTap = false; return; }
            else if (ZLeft.Contains(p)) { _knobTarget -= KnobStep; SendSteps(); }   // gira 10º a l'esquerra = 1 DEC
            else if (ZRight.Contains(p)) { _knobTarget += KnobStep; SendSteps(); }  // gira 10º a la dreta = 1 INC
            Changed();
        }
        /// <summary>Pom de l'ALT: sense lletres, contorn amb estriat de 8 ones (gira amb el pom; la llum queda fixa).</summary>
        void DrawLobed(Graphics g, float ir)
        {
            const int N = 128;
            var pts = new PointF[N];
            double rot = _knob * Math.PI / 180.0;
            for (int i = 0; i < N; i++)
            {
                double a = i * 2 * Math.PI / N;
                float rr = ir * (0.93f + 0.07f * (float)Math.Cos(8 * a));
                pts[i] = new PointF(_cx + rr * (float)Math.Sin(a + rot), _cy - rr * (float)Math.Cos(a + rot));
            }
            using var p = new GraphicsPath();
            p.AddPolygon(pts);
            float dk = 1f - 0.18f * Math.Max(0, -_push);
            Color C(int r, int gg, int b) => Color.FromArgb((int)(r * dk), (int)(gg * dk), (int)(b * dk));
            using (var pg = new PathGradientBrush(p) { CenterColor = C(206, 214, 224), SurroundColors = new[] { C(112, 122, 134) }, CenterPoint = new PointF(_cx - ir * 0.3f, _cy - ir * 0.35f) })
                g.FillPath(pg, p);
            // relleu de les ones: llum a les crestes de dalt a l'esquerra i ombra a les de baix a la dreta
            using (var hl = new Pen(Color.FromArgb(90, 255, 255, 255), Math.Max(0.4f, ir * 0.05f)))
            using (var dl = new Pen(Color.FromArgb(80, 20, 24, 30), Math.Max(0.4f, ir * 0.05f)))
            {
                var st = g.Save();
                g.SetClip(p, CombineMode.Intersect);
                g.DrawArc(hl, _cx - ir * 0.9f, _cy - ir * 0.9f, ir * 1.8f, ir * 1.8f, 180, 110);
                g.DrawArc(dl, _cx - ir * 0.9f, _cy - ir * 0.9f, ir * 1.8f, ir * 1.8f, 0, 110);
                g.Restore(st);
            }
            using (var op = new Pen(Color.FromArgb(120, 70, 78, 88), 0.5f)) g.DrawPath(op, p);
        }

        /// <summary>Rotatiu simple del FCU (SPD / HDG): anell clar fi al voltant, base metàl·lica i el pom a sobre.</summary>
        void DrawSimple(Graphics g)
        {
            float rr = _r * 1.18f;
            var ring = new RectangleF(_cx - rr, _cy - rr, rr * 2, rr * 2);
            using (var db = new SolidBrush(Color.FromArgb(70, 20, 24, 30))) g.FillEllipse(db, ring);
            using (var rp = new Pen(Color.FromArgb(196, 202, 210), Math.Max(0.5f, _r * 0.055f))) g.DrawEllipse(rp, ring);
            var o = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            Oh.SoftShadowEllipse(g, o, 1.2f, 1.8f, 2.2f, 130);
            using (var lg = new LinearGradientBrush(o, Color.FromArgb(200, 206, 214), Color.FromArgb(84, 90, 100), 55f)) g.FillEllipse(lg, o);
            using (var ep = new Pen(Color.FromArgb(60, 66, 74), 0.6f)) g.DrawEllipse(ep, o);
            // pom: estirat (+1) més gran i amb més ombra; endinsat (-1) més petit i més fosc. Tot el pom (ombra, cara,
            // estries, triangle) s'escala junt al voltant del centre, perquè l'efecte sigui uniforme
            float ir = _r * 0.72f;
            var i0 = new RectangleF(_cx - ir, _cy - ir, ir * 2, ir * 2);
            var gs = g.Save();
            float sc = 1f + 0.065f * _push;
            g.TranslateTransform(_cx, _cy); g.ScaleTransform(sc, sc); g.TranslateTransform(-_cx, -_cy);
            float sh = Math.Max(0f, 1f + _push);   // 0 endinsat, 1 repòs, 2 estirat
            Oh.SoftShadowEllipse(g, i0, 0.25f + 0.55f * sh, 0.35f + 0.85f * sh, 1.2f + 0.4f * sh, (int)(60 + 80 * Math.Min(1f, sh)));
            if (_push < 0)
            {
                // endinsat: ombra fosca que envolta l'estriat (el pom queda dins el forat)
                float P = -_push;
                using var hp = new GraphicsPath();
                hp.AddPolygon(FacePts(ir));
                using var dp = new Pen(Color.FromArgb((int)(190 * P), 4, 5, 7), ir * 0.16f) { LineJoin = LineJoin.Round };
                g.DrawPath(dp, hp);
            }
            try { DrawSimpleFace(g, ir); } finally { g.Restore(gs); }
        }

        /// <summary>Contorn de la cara del pom simple: 8 ones (HDG) o 40 estries (SPD), girat amb el pom.</summary>
        PointF[] FacePts(float ir)
        {
            const int N = 240;
            var pts = new PointF[N];
            double rot = _knob * Math.PI / 180.0;
            bool hdg = _face == KnobFace.Hdg;
            for (int i = 0; i < N; i++)
            {
                double a = i * 2 * Math.PI / N;
                float q = hdg ? ir * (0.93f + 0.07f * (float)Math.Cos(8 * a)) : ir * (0.975f + 0.025f * (float)Math.Cos(40 * a));
                pts[i] = new PointF(_cx + q * (float)Math.Sin(a + rot), _cy - q * (float)Math.Cos(a + rot));
            }
            return pts;
        }

        void DrawSimpleFace(Graphics g, float ir)
        {
            if (_face == KnobFace.Hdg)
            {
                DrawLobed(g, ir);
                // triangle blau dibuixat a la cara (gira amb el pom)
                var st = g.Save();
                g.TranslateTransform(_cx, _cy); g.RotateTransform(_knob);
                float tr = ir * 0.5f;
                var tri = new[] { new PointF(0, -tr), new PointF(tr * 0.866f, tr * 0.5f), new PointF(-tr * 0.866f, tr * 0.5f) };
                float tdk = 1f - 0.18f * Math.Max(0, -_push);
                using (var tp = new Pen(Color.FromArgb((int)(40 * tdk), (int)(96 * tdk), (int)(210 * tdk)), Math.Max(0.5f, ir * 0.09f)) { LineJoin = LineJoin.Round }) g.DrawPolygon(tp, tri);
                g.Restore(st);
                return;
            }
            // SPD: unes 40 estries al voltant i un cercle blanc molt a tocar del perímetre
            const int N = 240;
            var pts = new PointF[N];
            double rot = _knob * Math.PI / 180.0;
            for (int i = 0; i < N; i++)
            {
                double a = i * 2 * Math.PI / N;
                float q = ir * (0.975f + 0.025f * (float)Math.Cos(40 * a));
                pts[i] = new PointF(_cx + q * (float)Math.Sin(a + rot), _cy - q * (float)Math.Cos(a + rot));
            }
            float dk = 1f - 0.18f * Math.Max(0, -_push);
            Color C(int r, int gg, int b) => Color.FromArgb((int)(r * dk), (int)(gg * dk), (int)(b * dk));
            using (var p = new GraphicsPath())
            {
                p.AddPolygon(pts);
                using (var pg = new PathGradientBrush(p) { CenterColor = C(214, 220, 228), SurroundColors = new[] { C(110, 118, 130) }, CenterPoint = new PointF(_cx - ir * 0.3f, _cy - ir * 0.35f) })
                    g.FillPath(pg, p);
                using (var op = new Pen(Color.FromArgb(120, 60, 66, 76), 0.4f)) g.DrawPath(op, p);
            }
            float wr = ir * 0.86f;
            using (var wp = new Pen(Color.FromArgb(240, 242, 244), Math.Max(0.5f, ir * 0.07f))) g.DrawEllipse(wp, _cx - wr, _cy - wr, wr * 2, wr * 2);
        }

        public override void Draw(Graphics g)
        {
            if (Simple) { DrawSimple(g); return; }
            var o = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            Oh.SoftShadowEllipse(g, o, 1.2f, 1.8f, 2.2f, 130);
            // anell exterior negre estriat
            using (var p = new GraphicsPath())
            {
                p.AddEllipse(o);
                using var pg = new PathGradientBrush(p) { CenterColor = Color.FromArgb(58, 60, 64), SurroundColors = new[] { Color.FromArgb(10, 10, 12) }, CenterPoint = new PointF(_cx - _r * 0.3f, _cy - _r * 0.35f) };
                g.FillPath(pg, p);
            }
            var st = g.Save();
            g.TranslateTransform(_cx, _cy); g.RotateTransform(_ang);
            using (var kp = new Pen(Color.FromArgb(150, 0, 0, 0), Math.Max(0.35f, _r * 0.05f)))
            using (var kl = new Pen(Color.FromArgb(70, 160, 165, 170), Math.Max(0.3f, _r * 0.03f)))
                for (int i = 0; i < 36; i++)
                {
                    g.RotateTransform(10);
                    g.DrawLine(kp, 0, -_r * 0.98f, 0, -_r * 0.82f);
                    g.DrawLine(kl, _r * 0.04f, -_r * 0.98f, _r * 0.04f, -_r * 0.84f);
                }
            // índex blanc a l'anell
            using (var ib = new SolidBrush(Color.FromArgb(240, 240, 236))) g.FillRectangle(ib, -_r * 0.05f, -_r * 0.99f, _r * 0.1f, _r * 0.26f);
            g.Restore(st);
            using (var ep = new Pen(Color.FromArgb(5, 5, 6), 0.6f)) g.DrawEllipse(ep, o);
            // pom central "PULL STD": estirat (+1) s'apropa (més gran, més ombra); endinsat (-1) s'allunya (més petit, sense ombra, més fosc)
            float ir = _r * ((_alt ? 0.70f : 0.62f) + 0.045f * _push);
            var i0 = new RectangleF(_cx - ir, _cy - ir, ir * 2, ir * 2);
            float sh = Math.Max(0f, 1f + _push);   // 0 endinsat, 1 repòs, 2 estirat
            if (sh > 0.02f) Oh.SoftShadowEllipse(g, i0, 0.8f * sh, 1.2f * sh, 1.4f + 0.6f * Math.Max(0, _push), (int)(140 * Math.Min(1f, sh)));
            if (_push < 0)
            {
                // forat fosc al voltant quan s'endinsa
                var hole = RectangleF.Inflate(i0, ir * 0.06f * -_push, ir * 0.06f * -_push);
                using var hb = new SolidBrush(Color.FromArgb((int)(200 * -_push), 4, 4, 5));
                g.FillEllipse(hb, hole);
            }
            if (_alt) { DrawLobed(g, ir); return; }
            using (var p = new GraphicsPath())
            {
                p.AddEllipse(i0);
                // cara del pom una mica més fosca perquè les lletres clares es llegeixin bé
                float dk = 1f - 0.18f * Math.Max(0, -_push);
                Color C(int r, int gg, int b) => Color.FromArgb((int)(r * dk), (int)(gg * dk), (int)(b * dk));
                using var pg = new PathGradientBrush(p) { CenterColor = C(182, 187, 192), SurroundColors = new[] { C(104, 110, 116) }, CenterPoint = new PointF(_cx - ir * 0.35f, _cy - ir * 0.4f) };
                g.FillPath(pg, p);
            }
            using (var ip = new Pen(Color.FromArgb(110, 116, 122), 0.5f)) g.DrawEllipse(ip, i0);
            var tc = Color.FromArgb(244, 246, 248);
            // el text gira amb el pom (la llum i l'ombra es queden fixes)
            var ts = g.Save();
            g.TranslateTransform(_cx, _cy); g.RotateTransform(_knob); g.TranslateTransform(-_cx, -_cy);
            Oh.Text(g, "PULL", _cx, _cy - ir * 0.28f, ir * 0.5f, tc);
            Oh.Text(g, "STD", _cx, _cy + ir * 0.3f, ir * 0.5f, tc);
            g.Restore(ts);
        }
    }

    /// <summary>Indicador de tren (LDG GEAR): finestra negra amb "UNLK" vermell a dalt i triangle verd a baix.</summary>
    internal sealed class OhGearInd : OhElement
    {
        public float Unlk, Down;
        public OhGearInd(string id, RectangleF r) { Id = id; Bounds = r; }
        public override void Draw(Graphics g)
        {
            var f = Oh.MipFrame(g, Bounds);
            float half = f.Height / 2f;
            var up = new RectangleF(f.X, f.Y, f.Width, half);
            Oh.Legend(g, up, "UNLK", Oh.Red, Owner.Level(Unlk, false), false);
            float lv = Owner.Level(Down, false);
            var c = Oh.Lerp(Color.FromArgb(40, 70, 46), Oh.Green, lv);
            float tw = f.Width * 0.5f, th = half * 0.62f, cx = f.X + f.Width / 2, ty = f.Y + half + (half - th) / 2;
            using var tp = new Pen(c, 1.3f) { LineJoin = LineJoin.Round };
            g.DrawPolygon(tp, new[] { new PointF(cx - tw / 2, ty), new PointF(cx + tw / 2, ty), new PointF(cx, ty + th) });
        }
    }

    /// <summary>Palanca del tren d'aterratge: ranura vertical i pom en forma de roda; puja i baixa animada.</summary>
    internal sealed class OhGearLever : OhElement
    {
        readonly float _cx, _top, _bottom;
        public bool Down = true;
        float _pos = 1f;   // 0 = amunt, 1 = avall
        public OhGearLever(string id, float cx, float top, float bottom)
        {
            Id = id; _cx = cx; _top = top; _bottom = bottom;
            Bounds = new RectangleF(cx - 14, top - 6, 28, bottom - top + 12);
        }
        public override int State => Down ? 1 : 0;
        public override bool Animating => Math.Abs(_pos - (Down ? 1 : 0)) > 0.001f;
        public override void Tick(float dt) => _pos = Approach(_pos, Down ? 1 : 0, dt * 2.5f);
        public override void Tap(PointF p) { Down = !Down; Changed(); }
        public override void Draw(Graphics g)
        {
            // ranura
            var slot = new RectangleF(_cx - 3.5f, _top, 7f, _bottom - _top);
            using (var sp = Oh.RoundRect(slot, 3.5f))
            using (var sb = new LinearGradientBrush(slot, Color.FromArgb(8, 8, 10), Color.FromArgb(40, 44, 48), 0f)) g.FillPath(sb, sp);
            // braç i pom (roda blanca vista de cantell)
            float hy = _top + 8 + (_bottom - _top - 16) * _pos;
            var arm = new RectangleF(_cx - 2.2f, Math.Min(hy, _top + (_bottom - _top) / 2), 4.4f, Math.Abs(hy - (_top + (_bottom - _top) / 2)));
            using (var ab = new LinearGradientBrush(new RectangleF(_cx - 2.5f, _top, 5f, _bottom - _top), Color.FromArgb(60, 62, 66), Color.FromArgb(150, 154, 160), 0f)) g.FillRectangle(ab, arm);
            var wheel = new RectangleF(_cx - 9f, hy - 6.5f, 18f, 13f);
            using (var wp = Oh.RoundRect(wheel, 6f))
            {
                Oh.SoftShadow(g, wp, 1.6f, 2.4f, 2.6f, 140);
                using (var wb = new LinearGradientBrush(wheel, Color.FromArgb(246, 246, 242), Color.FromArgb(150, 150, 146), 0f)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { Color.FromArgb(150, 150, 146), Color.FromArgb(250, 250, 246), Color.FromArgb(214, 214, 208), Color.FromArgb(120, 120, 116) },
                        Positions = new[] { 0f, 0.35f, 0.65f, 1f }
                    }
                }) g.FillPath(wb, wp);
                using var pe = new Pen(Color.FromArgb(70, 70, 68), 0.6f); g.DrawPath(pe, wp);
            }
            using (var gp = new Pen(Color.FromArgb(110, 90, 90, 88), 0.5f))
                for (int i = -2; i <= 2; i++) g.DrawLine(gp, _cx + i * 3f, wheel.Y + 1.5f, _cx + i * 3f, wheel.Bottom - 1.5f);
        }
    }

    /// <summary>Indicador triple de pressió de frens (ACCU / BRAKES L-R) amb agulles mòbils animades.</summary>
    internal sealed class OhBrakeGauge : OhElement
    {
        readonly float _cx, _cy, _r;
        public float Accu = 3f, Left, Right;           // valors objectiu (×1000 PSI, 0..4)
        float _a = 3f, _l, _rr;                          // valors mostrats (animació)
        public OhBrakeGauge(string id, float cx, float cy, float r)
        {
            Id = id; _cx = cx; _cy = cy; _r = r; Bounds = new RectangleF(cx - r, cy - r, r * 2, r * 2);
        }
        public override int State => (int)Math.Round(Left * 1000);
        public override bool Animating => Math.Abs(_a - Accu) > 0.005f || Math.Abs(_l - Left) > 0.005f || Math.Abs(_rr - Right) > 0.005f;
        public override void Tick(float dt)
        {
            _a = Approach(_a, Accu, dt * 3f); _l = Approach(_l, Left, dt * 3f); _rr = Approach(_rr, Right, dt * 3f);
        }
        // tocar-lo simula el fre d'estacionament (0 ↔ 2000 PSI); amb SimConnect s'hi enviaran els valors reals
        public override void Tap(PointF p) { float v = Left > 1 ? 0f : 2f; Left = v; Right = v; Changed(); }

        // angles des de dalt, en sentit horari: ACCU a dalt (-60..60), L a baix-esquerra, R a baix-dreta
        static float AccuAng(float v) => -60f + v / 4f * 120f;
        static float LAng(float v) => 250f - v / 4f * 60f;
        static float RAng(float v) => 110f + v / 4f * 60f;

        public override void Draw(Graphics g)
        {
            var o = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            Oh.SoftShadowEllipse(g, o, 1.6f, 2.4f, 3f, 140);
            using (var bb = new LinearGradientBrush(o, Color.FromArgb(196, 204, 212), Color.FromArgb(70, 78, 86), 50f)) g.FillEllipse(bb, o);
            var face = RectangleF.Inflate(o, -_r * 0.12f, -_r * 0.12f);
            using (var fb = new SolidBrush(Color.FromArgb(10, 11, 12))) g.FillEllipse(fb, face);
            float fr = face.Width / 2f;
            void Arc(float a0, float a1, Color c, float rad, float w)
            {
                using var p = new Pen(c, w);
                g.DrawArc(p, _cx - rad, _cy - rad, rad * 2, rad * 2, Math.Min(a0, a1) - 90, Math.Abs(a1 - a0));
            }
            void Ticks(Func<float, float> ang, string[] labels)
            {
                using var tp = new Pen(Color.FromArgb(235, 235, 230), 0.8f);
                for (int i = 0; i <= 4; i++)
                {
                    double a = (ang(i) - 90) * Math.PI / 180.0;
                    float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
                    g.DrawLine(tp, _cx + c * fr * 0.80f, _cy + s * fr * 0.80f, _cx + c * fr * 0.93f, _cy + s * fr * 0.93f);
                    if (labels[i].Length > 0) Oh.Text(g, labels[i], _cx + c * fr * 0.66f, _cy + s * fr * 0.66f, fr * 0.16f, Color.FromArgb(235, 235, 230));
                }
            }
            // zones de color (verd = normal, groc = baixa pressió)
            Arc(AccuAng(0), AccuAng(1), Color.FromArgb(230, 190, 40), fr * 0.86f, 1.6f);
            Arc(AccuAng(2.5f), AccuAng(3.5f), Color.FromArgb(70, 200, 90), fr * 0.86f, 1.6f);
            Arc(LAng(0), LAng(1), Color.FromArgb(230, 190, 40), fr * 0.86f, 1.6f);
            Arc(RAng(0), RAng(1), Color.FromArgb(230, 190, 40), fr * 0.86f, 1.6f);
            Ticks(AccuAng, new[] { "0", "1", "2", "3", "4" });
            Ticks(LAng, new[] { "0", "", "", "", "3" });
            Ticks(RAng, new[] { "0", "", "", "", "3" });
            var tc = Color.FromArgb(235, 235, 230);
            Oh.Text(g, "ACCU", _cx - fr * 0.30f, _cy - fr * 0.30f, fr * 0.15f, tc);
            Oh.Text(g, "PRESS", _cx + fr * 0.34f, _cy - fr * 0.18f, fr * 0.13f, tc);
            Oh.Text(g, "BRAKES", _cx, _cy + fr * 0.70f, fr * 0.14f, tc);
            // agulles
            void Needle(float angDeg, float len)
            {
                var st = g.Save();
                g.TranslateTransform(_cx, _cy); g.RotateTransform(angDeg);
                var pts = new[] { new PointF(-fr * 0.035f, 0), new PointF(fr * 0.035f, 0), new PointF(fr * 0.012f, -len), new PointF(-fr * 0.012f, -len) };
                using (var sb = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) { var stt = g.Save(); g.TranslateTransform(0.6f, 0.9f); g.FillPolygon(sb, pts); g.Restore(stt); }
                using (var nb = new SolidBrush(Color.FromArgb(250, 250, 246))) g.FillPolygon(nb, pts);
                g.Restore(st);
            }
            Needle(AccuAng(_a), fr * 0.82f);
            Needle(LAng(_l), fr * 0.82f);
            Needle(RAng(_rr), fr * 0.82f);
            using (var hb = new SolidBrush(Color.FromArgb(60, 62, 66))) g.FillEllipse(hb, _cx - fr * 0.1f, _cy - fr * 0.1f, fr * 0.2f, fr * 0.2f);
            using (var vg = new LinearGradientBrush(face, Color.FromArgb(26, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 60f))
                g.FillPie(vg, face.X, face.Y, face.Width, face.Height, 180, 120);
        }
    }

    // ═════════════════════════════════════ EL CONTROL ════════════════════════════════════════

    public sealed class A320OverheadControl : Control
    {
        public const float LW = 1250f, LH = 1275f;
        /// <summary>Alçada lògica del panell principal (proporció de la imatge de referència 2286×1362).</summary>
        // Panell principal: pantalla fixa 16:9 (3840×2160 a 4K); el dibuix es retalla per sota
        public const float MainLH = LW * 9f / 16f;
        /// <summary>Espai buit de dalt del dibuix del panell principal que es retalla (la visera toca la vora de dalt).</summary>
        public const float MainTopCut = 14.5f;

        // ── Pàgines: 0 = overhead, 1 = panell principal (glareshield + instruments) ──
        sealed class PageData
        {
            public readonly List<OhSilk> Silk = new();
            public List<OhElement> Els = new();
            public float LH;
            public float Shift;   // unitats lògiques retallades per dalt (el dibuix es desplaça amunt)
            public PointF[] Frame = Array.Empty<PointF>();
            public int ScrollY;
            public bool Visited;
            // superfície guardada mentre la pàgina no és la visible (canvi de pàgina instantani)
            public Bitmap? Bmp; public IntPtr HDib, MemDC, OldObj; public int ContentH; public float S; public int Zoom;
        }
        readonly PageData[] _pages = { new(), new() };
        int _page;
        List<OhSilk> _silk => _pages[_page].Silk;
        List<OhElement> _els { get => _pages[_page].Els; set => _pages[_page].Els = value; }
        float CurLH => _pages[_page].LH;
        float CurShift => _pages[_page].Shift;
        PointF[] CurFrame => _pages[_page].Frame;
        readonly Dictionary<string, OhElement> _byId = new();
        readonly HashSet<OhElement> _animating = new();
        readonly List<(IOhTestable el, OhElement owner, DateTime until)> _tests = new();

        // El bitmap és una DIB de Windows: GDI+ hi dibuixa (via _bmp, que comparteix la mateixa memòria)
        // i a la pantalla es copia amb BitBlt de GDI, molt més ràpid que Graphics.DrawImage a 4K.
        Bitmap? _bmp;
        IntPtr _hDib, _memDC, _oldObj;
        float _s = 1f;
        int _contentH;
        int _scrollY;
        int _annMode = 1; // 0 TEST, 1 BRT, 2 DIM

        readonly System.Windows.Forms.Timer _anim = new() { Interval = 16 };
        readonly Stopwatch _clock = Stopwatch.StartNew();
        double _lastTick;

        bool _down, _dragging;
        Point _downPt;
        int _downScroll;
        double _lastMoveT;
        int _lastMoveY;
        float _velY, _flingV;
        DateTime _barHideAt = DateTime.MinValue;
        bool _hasPendingScroll;
        int _pendingScroll;
        int _zoom = 100;            // % de l'amplada original (sempre sobre l'original)
        int _offX, _offY;           // origen del panell dins el control (centrat en X, enganxat a baix si hi cap)
        bool _openAtBottom;
        int _btnDrag, _btnPressed;  // 1 = +, 2 = −
        Point _btnDragOff;

        // ─── Win32 ───
        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER
        {
            public uint biSize; public int biWidth; public int biHeight; public ushort biPlanes; public ushort biBitCount;
            public uint biCompression; public uint biSizeImage; public int biXPelsPerMeter; public int biYPelsPerMeter;
            public uint biClrUsed; public uint biClrImportant;
        }
        [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFOHEADER bmi, uint usage, out IntPtr bits, IntPtr hSection, uint offset);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hdc, int x, int y, int w, int h, IntPtr src, int sx, int sy, uint rop);
        [DllImport("user32.dll")] static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, IntPtr scroll, IntPtr clip, IntPtr hrgnUpdate, IntPtr update, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool SetProp(IntPtr hWnd, string name, IntPtr data);
        [DllImport("user32.dll")] static extern bool SetWindowFeedbackSetting(IntPtr hwnd, int feedback, uint flags, uint size, ref int config);
        [DllImport("gdi32.dll")] static extern bool StretchBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, uint rop);
        [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr hdc, int mode);
        const int COLORONCOLOR = 3;
        const uint SRCCOPY = 0x00CC0020;
        const uint SW_INVALIDATE = 0x0002;

        /// <summary>Es dispara quan l'usuari canvia un element: (id, estat). Punt d'enganx per a SimConnect.</summary>
        public event Action<string, int>? ElementChanged;
        /// <summary>Ordre d'un element cap a l'avió (id, ordre), p.ex. ("MIP_BARO", "INC").</summary>
        public event Action<string, string>? Command;
        internal void RaiseCommand(string id, string cmd) => Command?.Invoke(id, cmd);

        /// <summary>
        /// QNH del capità llegit del simulador (bidireccional): finestra de l'EFIS i anell in Hg / hPa.
        /// mode: 0 = STD, 1 = QNH, 2 = QFE. Sense corrent la finestra queda apagada.
        /// </summary>
        /// <summary>Posició de l'anell negre d'un rotatiu doble llegida del simulador (QNH: true = hPa; ALT: true = 1000).</summary>
        public void SetKnobRing(string id, bool right)
        {
            if (_byId.TryGetValue(id, out var k) && k is OhBaroKnob bk && bk.Hpa != right)
            {
                bk.Hpa = right;
                Redraw(TopDirty(bk));
                if (bk.Animating) { _animating.Add(bk); StartAnim(); }
            }
            _ov?.SetKnobRing(id, right);
        }

        bool _simDriven;
        /// <summary>
        /// true = hi ha un simulador connectat que governa els llums: les tecles i polsadors no canvien d'estat
        /// en tocar-los (només envien l'ordre i fan l'efecte de prémer); el llum arriba amb SetLight.
        /// </summary>
        public bool SimDriven { get => _simDriven; set { _simDriven = value; if (_ov != null) _ov.SimDriven = value; } }

        // vidres de les pantalles del panell principal (coordenades del panell): forats de la finestra
        readonly List<RectangleF> _screenGlass = new();
        /// <summary>Els forats de les pantalles han canviat (l'overhead s'ha mogut, obert o tancat, o la mida ha canviat).</summary>
        public event Action? HolesChanged;

        /// <summary>
        /// Forats de les pantalles PFD, ND i ECAM (coordenades de client d'aquest control): el vidre de cada pantalla,
        /// menys la part que tapa l'overhead quan és obert. null si no n'hi ha (overhead a pantalla completa, etc.).
        /// </summary>
        public Region? ScreenHoles()
        {
            if (_isOverlay || _page != 1 || _screenGlass.Count == 0 || Width <= 0) return null;
            float s = _s > 0 ? _s : Width / LW;
            float sh = CurShift;
            var rg = new Region(Rectangle.Empty);
            foreach (var gl in _screenGlass)
                rg.Union(Rectangle.FromLTRB(
                    (int)Math.Round(_offX + gl.Left * s), (int)Math.Round(_offY - _scrollY + (gl.Top - sh) * s),
                    (int)Math.Round(_offX + gl.Right * s), (int)Math.Round(_offY - _scrollY + (gl.Bottom - sh) * s)));
            if (_ov != null && _ov.Visible && _ov.Region != null) rg.Exclude(_ov.Region);
            return rg;
        }

        /// <summary>Vidre d'una pantalla (0 PFD, 1 ND, 2 ECAM superior, 3 ECAM inferior) en coordenades de pantalla de Windows.</summary>
        public Rectangle? GlassScreenRect(int index)
        {
            if (_isOverlay || index < 0 || index >= _screenGlass.Count || Width <= 0 || !IsHandleCreated) return null;
            float s = _s > 0 ? _s : Width / LW;
            var gl = _screenGlass[index];
            var rc = Rectangle.FromLTRB(
                (int)Math.Round(_offX + gl.Left * s), (int)Math.Round(_offY - _scrollY + (gl.Top - CurShift) * s),
                (int)Math.Round(_offX + gl.Right * s), (int)Math.Round(_offY - _scrollY + (gl.Bottom - CurShift) * s));
            return RectangleToScreen(rc);
        }

        /// <summary>Posició d'un selector (rotatiu o palanca) llegida del simulador.</summary>
        public void SetPosition(string id, int index)
        {
            if (_byId.TryGetValue(id, out var e))
            {
                bool ch = e is OhKnob kn ? kn.SetIndex(index) : e is OhToggle tg && tg.SetIndex(index);
                if (ch)
                {
                    Redraw(TopDirty(e));
                    if (e.Animating) { _animating.Add(e); StartAnim(); }
                }
            }
            _ov?.SetPosition(id, index);
        }

        /// <summary>Llum d'una tecla o polsador llegit del simulador.</summary>
        public void SetLight(string id, bool on)
        {
            if (_byId.TryGetValue(id, out var e))
            {
                if (e is OhKey key) { if (key.On != on) { key.On = on; Redraw(key.DirtyBounds); } }
                else if (e is OhPushButton pb)
                {
                    float lv = on ? 1f : 0f;
                    if (pb.Momentary) { if (pb.UpperLevel != lv || pb.LowerLevel != lv) { pb.UpperLevel = lv; pb.LowerLevel = lv; Redraw(TopDirty(pb)); } }
                    else if (pb.LowerLevel != lv || pb.In != on)
                    {
                        pb.LowerLevel = lv; pb.In = on;
                        Redraw(TopDirty(pb));
                        if (pb.Animating) { _animating.Add(pb); StartAnim(); }
                    }
                }
            }
            _ov?.SetLight(id, on);
        }

        /// <summary>Pantalla del FCU llegida del simulador.</summary>
        public void SetFcu(FcuState s)
        {
            if (_byId.TryGetValue("MIP_FCU_DISP", out var e) && e is OhFcuDisplay d && d.State0 != s) { d.State0 = s; Redraw(d.DirtyBounds); }
            _ov?.SetFcu(s);
        }

        public void SetBaro(bool powered, int mode, double value, bool inHg)
        {
            if (_byId.TryGetValue("MIP_BARO_WIN", out var w) && w is OhBaroWindow bw && bw.Set(powered, mode, value, inHg)) Redraw(bw.DirtyBounds);
            if (_byId.TryGetValue("MIP_BARO", out var k) && k is OhBaroKnob bk && bk.Hpa == inHg)
            {
                bk.Hpa = !inHg;
                Redraw(TopDirty(bk));
                if (bk.Animating) { _animating.Add(bk); StartAnim(); }
            }
            _ov?.SetBaro(powered, mode, value, inHg);
        }

        /// <summary>
        /// Control del mode A320: mostra el panell principal (fix, 4K) i, per sobre, l'overhead com una capa que
        /// penja de dalt de la pantalla (botó OVHD). L'overhead és un control fill retallat amb la seva forma:
        /// on no hi és es veu (i funciona) el panell principal.
        /// </summary>
        public A320OverheadControl() : this(false) { }

        A320OverheadControl(bool overlay)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false); // ja tenim el nostre propi Bitmap
            BackColor = Color.Black;
            _isOverlay = overlay;
            _anim.Tick += (s, e) => AnimTick();
            Build();
            if (!overlay) { _page = 1; _pages[1].Visited = true; }
        }

        // ─────────────── Capa overhead per sobre del panell principal ───────────────
        readonly bool _isOverlay;           // true: aquesta instància és la capa overhead
        A320OverheadControl? _ov;           // (panell principal) la capa overhead
        bool _closing;                      // (capa) animació de tancament en curs
        bool _startAtCut;                   // (capa) el gest ha començat amb l'overhead al topall
        /// <summary>Línia de tall de l'overhead (entre ANTI ICE / CABIN PRESS i EXT LT / APU / INT LT): topall superior.</summary>
        const float OverheadCutY = 1079f;

        event Action? OverlayMoved;
        event Action? OverlayClosed;

        bool OverlayOpen => _ov != null && _ov.Visible && !_ov._closing;

        void CreateOverlay()
        {
            if (_ov != null || _isOverlay || IsDisposed) return;
            _ov = new A320OverheadControl(true) { Visible = false, Bounds = ClientRectangle };
            _ov.ElementChanged += (id, st) => ElementChanged?.Invoke(id, st);
            _ov.Command += (id, c) => Command?.Invoke(id, c);
            _ov.OverlayMoved += OnOverlayMoved;
            _ov.OverlayClosed += () => { InvalidateButtons(); OnOverlayMoved(); };
            Controls.Add(_ov);
            // es prepara el dibuix de l'overhead en segon pla, perquè obrir-lo sigui immediat
            BeginInvoke(new Action(() => { if (_ov != null && !_ov.IsDisposed) _ov.EnsureBitmap(); }));
        }

        void ToggleOverlay()
        {
            CreateOverlay();
            if (_ov == null) return;
            if (OverlayOpen) _ov.BeginClose();
            else _ov.OpenAtCut();
            InvalidateButtons();
        }

        void InvalidateButtons() { Invalidate(Rectangle.Inflate(AllBtnsRect, 16, 16)); Invalidate(Rectangle.Inflate(AreasRect, 6, 6)); }

        /// <summary>(capa) S'obre amb la línia de tall a la vora de dalt de la pantalla.</summary>
        void OpenAtCut()
        {
            _closing = false;
            if (!EnsureBitmap()) return;
            _scrollY = MaxScroll;
            UpdateOverlayRegion();
            Visible = true;
            BringToFront();
            Invalidate();
            OverlayMoved?.Invoke();
        }

        bool _opening;                      // (capa) animació d'obertura en curs (baixa des de dalt)
        bool _edgeStart;                    // (panell principal) el gest ha començat a la vora de dalt
        int EdgePx => Math.Max(6, Height / 60);   // franja sensible a dalt de la pantalla (36 px a 4K)

        /// <summary>(capa) Baixa des de dalt de la pantalla, animada, fins a la línia de tall.</summary>
        void OpenAnimated()
        {
            if (!EnsureBitmap()) return;
            _closing = false;
            _opening = true;
            _flingV = 0; _hasPendingScroll = false; _down = false; _dragging = false;
            _scrollY = (int)Math.Ceiling(LH * _s);    // tot l'overhead per sobre de la pantalla
            UpdateOverlayRegion();
            Visible = true;
            BringToFront();
            Invalidate();
            StartAnim();
        }

        /// <summary>(panell principal) Obre l'overhead amb l'animació de baixada (lliscament des de dalt).</summary>
        void OpenOverlayAnimated()
        {
            CreateOverlay();
            if (_ov == null || OverlayOpen) return;
            _ov.OpenAnimated();
            InvalidateButtons();
        }

        /// <summary>(capa) Marxa cap amunt animada i s'amaga.</summary>
        void BeginClose()
        {
            if (_closing) return;
            _closing = true;
            _flingV = 0; _hasPendingScroll = false; _down = false; _dragging = false;
            StartAnim();
        }

        void FinishClose()
        {
            _closing = false;
            Visible = false;
            OverlayClosed?.Invoke();
        }

        /// <summary>(capa) Contorn de l'overhead en coordenades de pantalla.</summary>
        PointF[]? ScreenFrame()
        {
            if (_bmp == null) return null;
            return FramePoly.Select(p => new PointF(_offX + p.X * _s, -_scrollY + p.Y * _s)).ToArray();
        }

        int OverlayBottom => (int)Math.Ceiling(-_scrollY + LH * _s);

        /// <summary>(capa) Retalla la finestra a la forma de l'overhead (fora d'ella es veu el panell principal).</summary>
        void UpdateOverlayRegion()
        {
            var frame = ScreenFrame();
            if (frame == null) return;
            using var path = new GraphicsPath();
            path.AddPolygon(frame);
            var rg = new Region(path);
            rg.Intersect(ClientRectangle);
            if (Parent is A320OverheadControl main)
            {
                rg.Exclude(Rectangle.Inflate(main.AllBtnsRect, 12, 12));
                rg.Exclude(Rectangle.Inflate(main.AreasRect, 4, 4));   // el botó taronja sempre visible
            }
            var old = Region;
            Region = rg;
            old?.Dispose();
            OverlayMoved?.Invoke();
        }

        // Ombra de l'overhead: franges estretes sota les 3 vores de baix (diagonal dreta, base, diagonal esquerra).
        // Només es dibuixen i es repinten aquestes franges (abans es repintava tota l'amplada i era molt lent).
        static readonly byte[] ShadowAlpha = { 70, 46, 28, 14 };
        const int ShadowBand = 7;
        int ShadowDepth => ShadowAlpha.Length * ShadowBand;
        PointF[][]? _shadowEdges;   // (panell principal) vores de l'última ombra dibuixada

        /// <summary>(capa) Les 3 vores de baix de l'overhead en coordenades de pantalla.</summary>
        PointF[][]? BottomEdges()
        {
            var f = ScreenFrame();
            if (f == null) return null;
            // FramePoly: 0..1 dalt, 2 dreta-diagonal, 3-4 base, 5 esquerra-diagonal
            return new[] { new[] { f[2], f[3] }, new[] { f[3], f[4] }, new[] { f[4], f[5] } };
        }

        static Region ShadowRegion(PointF[][] edges, int depth)
        {
            using var path = new GraphicsPath();
            foreach (var e in edges)
                path.AddPolygon(new[] { new PointF(e[0].X, e[0].Y - 2), new PointF(e[1].X, e[1].Y - 2), new PointF(e[1].X, e[1].Y + depth + 2), new PointF(e[0].X, e[0].Y + depth + 2) });
            return new Region(path);
        }

        /// <summary>(panell principal) La capa s'ha mogut: es repinten només les franges de l'ombra (vella i nova).</summary>
        void OnOverlayMoved()
        {
            if (_ov == null) return;
            HolesChanged?.Invoke();
            var nw = _ov.Visible ? _ov.BottomEdges() : null;
            if (_shadowEdges != null) using (var r = ShadowRegion(_shadowEdges, ShadowDepth)) Invalidate(r);
            if (nw != null) using (var r = ShadowRegion(nw, ShadowDepth)) Invalidate(r);
            _shadowEdges = nw;
        }

        /// <summary>(panell principal) Ombra de l'overhead sobre el panell, sota les seves vores (sense suavitzat: ràpid).</summary>
        void DrawOverlayShadow(Graphics g, Rectangle r, Point origin)
        {
            if (_ov == null || !_ov.Visible) return;
            var edges = _ov.BottomEdges();
            if (edges == null) return;
            var st = g.Save();
            g.TranslateTransform(-origin.X, -origin.Y);
            g.SetClip(r);
            g.SmoothingMode = SmoothingMode.None;
            for (int i = 0; i < ShadowAlpha.Length; i++)
            {
                using var b = new SolidBrush(Color.FromArgb(ShadowAlpha[i], 0, 0, 0));
                float y0 = i * ShadowBand, y1 = (i + 1) * ShadowBand;
                foreach (var e in edges)
                {
                    var q = new[] { new PointF(e[0].X, e[0].Y + y0), new PointF(e[1].X, e[1].Y + y0), new PointF(e[1].X, e[1].Y + y1), new PointF(e[0].X, e[0].Y + y1) };
                    float minY = Math.Min(q[0].Y, q[1].Y), maxY = Math.Max(q[2].Y, q[3].Y);
                    if (maxY < r.Top || minY > r.Bottom) continue;
                    g.FillPolygon(b, q);
                }
            }
            g.Restore(st);
        }

        // ───────────────────────────────── API pública ─────────────────────────────────

        /// <summary>Intensitat (0..1) de les llegendes d'un polsador. null = no tocar.</summary>
        public void SetLegend(string id, float? upper, float? lower)
        {
            if (!_byId.TryGetValue(id, out var el)) return;
            if (el is OhPushButton pb)
            {
                if (upper.HasValue) pb.UpperLevel = Math.Clamp(upper.Value, 0f, 1f);
                if (lower.HasValue) { pb.LowerLevel = Math.Clamp(lower.Value, 0f, 1f); }
            }
            else if (el is OhFireButton fb && upper.HasValue) fb.Lit = Math.Clamp(upper.Value, 0f, 1f);
            else if (el is OhGearInd gi)
            {
                if (upper.HasValue) gi.Unlk = Math.Clamp(upper.Value, 0f, 1f);
                if (lower.HasValue) gi.Down = Math.Clamp(lower.Value, 0f, 1f);
            }
            Redraw(TopDirty(el));
        }

        public void SetDisplay(string id, string text)
        {
            if (_byId.TryGetValue(id, out var el) && el is OhSeg7 s && s.Text != text) { s.Text = text; Redraw(el.DirtyBounds); }
        }

        public int GetState(string id) => _byId.TryGetValue(id, out var el) ? el.State : -1;

        // ─────────────────────────── usat pels elements ───────────────────────────

        internal float Level(float level, bool test)
        {
            if (_annMode == 0 || test) return 1f;
            return _annMode == 2 ? level * 0.55f : level;
        }

        OhElement? _pressEl;   // element amb el dit a sobre (per a accions mantingudes)

        void EndPress()
        {
            var el = _pressEl;
            _pressEl = null;
            el?.PressEnd();
        }

        internal void NotifyChanged(OhElement el)
        {
            Redraw(TopDirty(el));
            if (el.Animating) { _animating.Add(el); StartAnim(); }
            ElementChanged?.Invoke(el.Id, el.State);
        }

        internal void ClearGroup(string group, OhElement keep)
        {
            foreach (var k in _byId.Values.OfType<OhKey>())
                if (k != keep && k.Group == group && k.On) { k.On = false; Redraw(k.DirtyBounds); }
        }

        void FlashTest(float seconds, params string[] ids)
        {
            var until = DateTime.UtcNow.AddSeconds(seconds);
            foreach (var id in ids)
                if (_byId.TryGetValue(id, out var el) && el is IOhTestable t)
                {
                    t.Test = true;
                    _tests.Add((t, el, until));
                    Redraw(TopDirty(el));
                }
            StartAnim();
        }

        /// <summary>Si l'element viu dins una guarda, la zona a repintar és la de la guarda.</summary>
        RectangleF TopDirty(OhElement el)
        {
            foreach (var e in _els)
                if (e is OhGuard g && g.Inner == el) return g.DirtyBounds;
            return el.DirtyBounds;
        }

        // ───────────────────────────────── Dibuix ─────────────────────────────────

        bool EnsureBitmap()
        {
            if (_bmp != null) return true;
            if (Width <= 0 || Height <= 0) return false;
            // L'amplada sempre es calcula sobre l'original (amplada de pantalla) i el panell es torna a
            // dibuixar en vectorial a aquesta mida exacta: mai s'escala la imatge anterior.
            int cw = ContentWidth;
            _s = cw / LW;
            _contentH = (int)Math.Ceiling(CurLH * _s);
            _offX = (Width - cw) / 2;
            _offY = _isOverlay ? 0 : Math.Max(0, Height - _contentH); // si hi cap sencer, queda enganxat a baix
            if (!CreateSurface(cw, _contentH, out _hDib, out _bmp, out _memDC, out _oldObj)) return false;
            ClampScroll();
            if (_openAtBottom) { _scrollY = MaxScroll; _openAtBottom = false; }
            DrawContent(_bmp!, _s, new Rectangle(0, 0, cw, _contentH), CancellationToken.None);
            return true;
        }

        // el panell principal sempre al 100% (sense zoom); l'overhead segons _zoom
        int EffZoom => _page == 1 ? 100 : _zoom;
        int ContentWidth => Math.Max(1, (int)Math.Round(Width * EffZoom / 100.0));

        /// <summary>Crea una DIB de Windows (memòria compartida entre GDI+ per dibuixar i GDI per copiar a pantalla).</summary>
        static bool CreateSurface(int w, int h, out IntPtr hDib, out Bitmap? bmp, out IntPtr memDC, out IntPtr oldObj)
        {
            bmp = null; memDC = IntPtr.Zero; oldObj = IntPtr.Zero;
            var bmi = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = w, biHeight = -h, // negatiu = files de dalt a baix
                biPlanes = 1, biBitCount = 32
            };
            hDib = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out IntPtr bits, IntPtr.Zero, 0);
            if (hDib == IntPtr.Zero || bits == IntPtr.Zero) { hDib = IntPtr.Zero; return false; }
            bmp = new Bitmap(w, h, w * 4, PixelFormat.Format32bppPArgb, bits);
            memDC = CreateCompatibleDC(IntPtr.Zero);
            oldObj = SelectObject(memDC, hDib);
            return true;
        }

        static void FreeSurface(Bitmap? bmp, IntPtr memDC, IntPtr oldObj, IntPtr hDib)
        {
            bmp?.Dispose();
            if (memDC != IntPtr.Zero) { SelectObject(memDC, oldObj); DeleteDC(memDC); }
            if (hDib != IntPtr.Zero) DeleteObject(hDib);
        }

        // ── Render en segon pla del zoom ──
        Task<bool>? _bgTask;
        CancellationTokenSource? _bgCts;
        int _bgGen;
        bool _bgRendering;
        readonly List<RectangleF> _bgDirty = new();

        /// <summary>Atura (i espera) un render de zoom en curs; el seu resultat es descarta.</summary>
        void CancelBackgroundRender()
        {
            _bgGen++;
            if (_bgTask != null)
            {
                _bgCts?.Cancel();
                try { _bgTask.Wait(); } catch { }
            }
            _bgTask = null; _bgCts = null;
            _bgRendering = false;
            _bgDirty.Clear();
        }

        void ReleaseCache()
        {
            CancelBackgroundRender();
            // Porta les animacions pendents al seu estat final (sense dibuixar: el bitmap s'allibera)
            var pending = _animating.ToArray();
            FreeSurface(_bmp, _memDC, _oldObj, _hDib);
            _bmp = null; _memDC = IntPtr.Zero; _oldObj = IntPtr.Zero; _hDib = IntPtr.Zero;
            // també les superfícies guardades de l'altra pàgina
            foreach (var p in _pages)
            {
                FreeSurface(p.Bmp, p.MemDC, p.OldObj, p.HDib);
                p.Bmp = null; p.HDib = p.MemDC = p.OldObj = IntPtr.Zero;
            }
            _hasPendingScroll = false;
            foreach (var el in pending) { for (int i = 0; i < 200 && el.Animating; i++) el.Tick(0.05f); }
            foreach (var t in _tests) t.el.Test = false;
            _tests.Clear();
            _animating.Clear();
            _anim.Stop();
            _flingV = 0;
        }

        void Redraw(RectangleF logical, bool invalidate = true)
        {
            if (_bmp == null) return;
            // mentre es calcula un zoom en segon pla, no es dibuixa res (s'apunta i es repinta en acabar)
            if (_bgRendering) { _bgDirty.Add(logical); return; }
            var px = Rectangle.FromLTRB(
                (int)Math.Floor(logical.Left * _s) - 1, (int)Math.Floor((logical.Top - CurShift) * _s) - 1,
                (int)Math.Ceiling(logical.Right * _s) + 1, (int)Math.Ceiling((logical.Bottom - CurShift) * _s) + 1);
            px.Intersect(new Rectangle(0, 0, _bmp.Width, _bmp.Height));
            if (px.Width <= 0 || px.Height <= 0) return;
            DrawContent(_bmp, _s, px, CancellationToken.None);
            if (invalidate) Invalidate(new Rectangle(px.X + _offX, px.Y + _offY - _scrollY, px.Width, px.Height));
        }

        /// <summary>Dibuixa la zona px (píxels del bitmap) del panell a l'escala s. Pot córrer en segon pla.</summary>
        void DrawContent(Bitmap target, float s, Rectangle px, CancellationToken ct)
        {
            float sh = CurShift;
            var lr = RectangleF.FromLTRB(px.Left / s, px.Top / s + sh, px.Right / s, px.Bottom / s + sh);
            using var g = Graphics.FromImage(target);
            g.SetClip(px);
            g.FillRectangle(Brushes.Black, px);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.ScaleTransform(s, s);
            g.TranslateTransform(0, -sh);
            Oh.PixelScale = s;
            DrawFrame(g);
            foreach (var si in _silk)
            {
                if (ct.IsCancellationRequested) return;
                if (si.Bounds.IntersectsWith(lr)) si.Draw(g);
            }
            foreach (var e in _els)
            {
                if (ct.IsCancellationRequested) return;
                if (e.DirtyBounds.IntersectsWith(lr)) e.Draw(g);
            }
            DrawLighting(g);
        }

        static readonly PointF[] FramePoly =
        {
            new(12, 5), new(1238, 5), new(1238, 1104), new(950, 1266), new(300, 1266), new(12, 1104)
        };

        /// <summary>Contorn del panell principal: glareshield amb la vora de dalt inclinada a l'esquerra, pedestal
        /// central fins a baix i la zona dels genolls a l'esquerra (coordenades lògiques).</summary>
        static readonly PointF[] MainFramePoly =
        {
            new(7, 124.5f), new(681, 14.5f), new(1241, 14.5f), new(1241, 740), new(519, 740), new(519, 656), new(7, 656)
        };

        /// <summary>Llum global del cockpit: una mica més clar a dalt, més fosc a baix i a les vores laterals.</summary>
        void DrawLighting(Graphics g)
        {
            var full = new RectangleF(0, CurShift, LW, CurLH);
            var FramePoly = CurFrame;
            using (var v = new LinearGradientBrush(full, Color.Black, Color.Black, 90f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(26, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(46, 0, 0, 0) },
                    Positions = new[] { 0f, 0.4f, 0.6f, 1f }
                }
            }) g.FillPolygon(v, FramePoly);
            using (var h = new LinearGradientBrush(full, Color.Black, Color.Black, 0f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(40, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(40, 0, 0, 0) },
                    Positions = new[] { 0f, 0.14f, 0.86f, 1f }
                }
            }) g.FillPolygon(h, FramePoly);
        }

        void DrawFrame(Graphics g)
        {
            using var b = new LinearGradientBrush(new RectangleF(0, CurShift, LW, CurLH), Oh.T(88, 108, 126, 82, 110, 134), Oh.Frame, 90f);
            g.FillPolygon(b, CurFrame);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (!EnsureBitmap()) { g.Clear(Color.Black); return; }
            if (!_isOverlay && _ov == null) BeginInvoke(new Action(CreateOverlay));
            var clip = e.ClipRectangle;

            // La zona dels botons es compon fora de pantalla (panell + botons) i es copia d'un sol cop:
            // així no es veu mai el panell sense els botons a sobre (sense pampallugues). La capa overhead
            // no en dibuixa (els botons són del panell principal i la capa hi deixa un forat).
            var btnArea = _isOverlay ? Rectangle.Empty : Rectangle.Inflate(AllBtnsRect, 14, 14);
            var ov = Rectangle.Intersect(clip, btnArea);

            // 1) tot el que no és la zona dels botons, directament a pantalla
            foreach (var piece in Subtract(clip, ov))
                PaintBase(g, piece, Point.Empty);

            // 2) zona dels botons: composició en un bitmap petit i una sola còpia
            if (ov.Width > 0 && ov.Height > 0)
            {
                using var buf = new Bitmap(ov.Width, ov.Height, PixelFormat.Format32bppPArgb);
                using (var gb = Graphics.FromImage(buf))
                {
                    PaintBase(gb, ov, ov.Location);
                    gb.TranslateTransform(-ov.X, -ov.Y);
                    DrawZoomButtons(gb, ov);
                }
                var oldMode = g.CompositingMode;
                g.CompositingMode = CompositingMode.SourceCopy;
                g.DrawImageUnscaled(buf, ov.X, ov.Y);
                g.CompositingMode = oldMode;
            }

            // 3) zones clicables ressaltades i botó taronja per mostrar-les
            DrawHitAreas(g, clip);
            if (!_isOverlay && AreasRect.IntersectsWith(clip)) DrawAreasButton(g);

            if (DateTime.UtcNow < _barHideAt && _contentH > Height)
            {
                var bar = ScrollBarRect();
                if (bar.IntersectsWith(clip))
                {
                    using var bb = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
                    g.FillRectangle(bb, bar);
                }
            }
        }

        /// <summary>Panell (o negre fora del panell) a la zona r de la pantalla; origin = on comença r dins de g.</summary>
        void PaintBase(Graphics g, Rectangle r, Point origin)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            var content = new Rectangle(_offX, _offY - _scrollY, _bmp!.Width, _contentH);
            var vis = Rectangle.Intersect(r, content);
            if (vis.Width > 0 && vis.Height > 0)
            {
                IntPtr hdc = g.GetHdc();
                try { BitBlt(hdc, vis.X - origin.X, vis.Y - origin.Y, vis.Width, vis.Height, _memDC, vis.X - _offX, vis.Y - _offY + _scrollY, SRCCOPY); }
                finally { g.ReleaseHdc(hdc); }
            }
            // franges negres fora del panell (laterals en reduir el zoom, i a dalt si hi cap sencer)
            using (var outside = new Region(r))
            {
                outside.Exclude(content);
                outside.Translate(-origin.X, -origin.Y);
                g.FillRegion(Brushes.Black, outside);
            }
            if (!_isOverlay) DrawOverlayShadow(g, r, origin);
        }

        /// <summary>El rectangle a menys el rectangle b, en fins a 4 rectangles.</summary>
        static IEnumerable<Rectangle> Subtract(Rectangle a, Rectangle b)
        {
            if (b.Width <= 0 || b.Height <= 0 || !a.IntersectsWith(b)) { yield return a; yield break; }
            if (b.Top > a.Top) yield return Rectangle.FromLTRB(a.Left, a.Top, a.Right, b.Top);
            if (b.Bottom < a.Bottom) yield return Rectangle.FromLTRB(a.Left, b.Bottom, a.Right, a.Bottom);
            if (b.Left > a.Left) yield return Rectangle.FromLTRB(a.Left, b.Top, b.Left, b.Bottom);
            if (b.Right < a.Right) yield return Rectangle.FromLTRB(b.Right, b.Top, a.Right, b.Bottom);
        }

        // ─────────────────────────── Zoom i botons + / − ───────────────────────────

        // panell principal: no es desplaça; capa overhead: de "tot desplegat" (0) fins al topall de la línia de tall
        int MaxScroll => _page == 1 ? 0 : _isOverlay ? (int)Math.Round(OverheadCutY * _s) : Math.Max(0, _contentH - Height);
        int ZoomBtnSize => Math.Max(40, (int)(80 * Width / 3840f));
        // columna de botons sota l'indicador de pressió de frens (panell principal 4K)
        Point BtnPos(float x, float y) => new((int)(x * Width / 3840f), (int)(y * Width / 3840f));
        Point DefaultMinusPos => BtnPos(3646, 2070);
        Point DefaultPlusPos => BtnPos(3646, 1980);
        // botó TH (quadrant de gas), just a sota d'OVHD
        Point DefaultThPos => BtnPos(3646, 1890);
        Rectangle ThRect => new(ThLocation ?? DefaultThPos, new Size(ZoomBtnSize, ZoomBtnSize));
        /// <summary>Posició del botó TH (null = sota d'OVHD).</summary>
        public Point? ThLocation { get; set; }
        OhThrottle? _throttle;
        bool _throttleShown;
        /// <summary>Si es mostra el quadrant de gas al panell principal (opció de l'usuari, per defecte no).</summary>
        public bool ThrottleVisible
        {
            get => _throttleShown;
            set
            {
                if (_throttleShown == value) return;
                _throttleShown = value;
                if (_throttle != null) { _throttle.Shown = value; Redraw(_throttle.DirtyBounds); }
                InvalidateButtons();
            }
        }
        /// <summary>Posició de les palanques de gas llegida del simulador (-1 REV FULL … 0 ralentí … 1 TO GA).</summary>
        public void SetThrottle(float left, float right)
        {
            if (_throttle == null) return;
            // el simulador pot enviar posicions desenes de cops per segon: es guarda només l'última i es dibuixa
            // com a molt cada 30 ms, i només la franja de cada palanca que es mou (no tot el quadrant)
            _thrPending = (left, right);
            if (_thrTimer == null)
            {
                _thrTimer = new System.Windows.Forms.Timer { Interval = 30 };
                _thrTimer.Tick += (s, e) => ApplyThrottle();
            }
            if (!_thrTimer.Enabled) { ApplyThrottle(); _thrTimer.Start(); }
        }
        (float l, float r)? _thrPending;
        System.Windows.Forms.Timer? _thrTimer;

        void ApplyThrottle()
        {
            if (_throttle == null || _thrPending is not var (left, right)) { _thrTimer?.Stop(); return; }
            _thrPending = null;
            var t = _throttle;
            bool chL = Math.Abs(t.PosL - left) >= 0.002f, chR = Math.Abs(t.PosR - right) >= 0.002f;
            if (!chL && !chR) return;
            var dirty = new List<RectangleF>();
            if (chL) { dirty.Add(t.LeverRect(t.PosL, -1)); t.PosL = left; dirty.Add(t.LeverRect(left, -1)); }
            if (chR) { dirty.Add(t.LeverRect(t.PosR, 1)); t.PosR = right; dirty.Add(t.LeverRect(right, 1)); }
            if (!t.Shown) return;
            // la posició vella i la nova sovint s'encavalquen: es redibuixa la unió de cada palanca d'un sol cop
            for (int i = 0; i + 1 < dirty.Count; i += 2) Redraw(RectangleF.Union(dirty[i], dirty[i + 1]));
        }

        /// <summary>L'usuari ha premut TH (per desar l'opció).</summary>
        public event Action<bool>? ThrottleVisibleChanged;
        Rectangle PlusRect => new(PlusLocation ?? DefaultPlusPos, new Size(ZoomBtnSize, ZoomBtnSize));
        Rectangle MinusRect => new(MinusLocation ?? DefaultMinusPos, new Size(ZoomBtnSize, ZoomBtnSize));
        Point DefaultPagePos => BtnPos(3646, 1800);
        Rectangle PageRect => new(PageLocation ?? DefaultPagePos, new Size(ZoomBtnSize, ZoomBtnSize));
        // botó taronja "mostrar àrees clicables" (com el de la banda G1000): cantonada de dalt a l'esquerra
        Point DefaultAreasPos => BtnPos(24, 48);
        Rectangle AreasRect => new(AreasLocation ?? DefaultAreasPos, new Size(ZoomBtnSize, ZoomBtnSize));
        /// <summary>Posició del botó "mostrar àrees clicables" (null = cantonada de dalt a l'esquerra).</summary>
        public Point? AreasLocation { get; set; }
        /// <summary>Si es ressalten les zones clicables (compartit pel panell principal i l'overhead).</summary>
        static bool s_showAreas;
        /// <summary>Color del botó "mostrar àrees": taronja, dibuixat molt subtil (poca opacitat) (a joc amb el panell A320).</summary>
        static readonly Color AreasColor = Color.Orange;
        Rectangle BtnRect(int b) => b == 1 ? PlusRect : b == 2 ? MinusRect : b == 4 ? AreasRect : b == 5 ? ThRect : PageRect;
        Rectangle AllBtnsRect => Rectangle.Union(Rectangle.Union(Rectangle.Union(PlusRect, MinusRect), PageRect), ThRect);

        /// <summary>Posició dels botons de zoom (null = posició per defecte a baix a la dreta).</summary>
        public Point? PlusLocation { get; set; }
        public Point? MinusLocation { get; set; }
        /// <summary>Posició del botó de canvi de pàgina overhead / panell principal (a sobre del +).</summary>
        public Point? PageLocation { get; set; }
        /// <summary>true si es mostra el panell principal; false si es mostra l'overhead.</summary>
        public bool ShowingMain => _page == 1;

        /// <summary>
        /// Canvia entre l'overhead i el panell principal. La pàgina que es deixa conserva el seu bitmap i la
        /// seva posició de desplaçament, de manera que tornar-hi és instantani (si el zoom no ha canviat).
        /// </summary>
        public void TogglePage()
        {
            CancelBackgroundRender();
            foreach (var el in _animating.ToArray()) { for (int i = 0; i < 200 && el.Animating; i++) el.Tick(0.05f); }
            _animating.Clear();
            _flingV = 0; _hasPendingScroll = false; _down = false; _dragging = false;

            var cur = _pages[_page];
            cur.ScrollY = _scrollY;
            cur.Bmp = _bmp; cur.HDib = _hDib; cur.MemDC = _memDC; cur.OldObj = _oldObj;
            cur.ContentH = _contentH; cur.S = _s; cur.Zoom = EffZoom;
            _bmp = null; _hDib = _memDC = _oldObj = IntPtr.Zero;

            _page = 1 - _page;
            var nxt = _pages[_page];
            if (nxt.Bmp != null && nxt.Zoom == EffZoom && nxt.Bmp.Width == ContentWidth)
            {
                _bmp = nxt.Bmp; _hDib = nxt.HDib; _memDC = nxt.MemDC; _oldObj = nxt.OldObj;
                _contentH = nxt.ContentH; _s = nxt.S;
                _offX = (Width - _bmp.Width) / 2;
                _offY = _isOverlay ? 0 : Math.Max(0, Height - _contentH);
            }
            else FreeSurface(nxt.Bmp, nxt.MemDC, nxt.OldObj, nxt.HDib);
            nxt.Bmp = null; nxt.HDib = nxt.MemDC = nxt.OldObj = IntPtr.Zero;

            // primera visita: overhead per baix, panell principal per dalt (es veu el glareshield sencer)
            _scrollY = nxt.Visited ? nxt.ScrollY : (_page == 0 ? int.MaxValue : 0);
            nxt.Visited = true;
            if (_bmp != null) ClampScroll(); else _openAtBottom = _scrollY == int.MaxValue;
            Invalidate();
        }
        /// <summary>Mode calibració (F12): els botons + / − es poden arrossegar.</summary>
        public bool CalibrationMode { get; set; }
        /// <summary>Es dispara en deixar anar un botó de zoom arrossegat en calibració (per desar-ne la posició).</summary>
        public event Action? ZoomButtonsMoved;

        public int ZoomPercent => _zoom;

        public const int MinZoom = 50, MaxZoom = 100;

        /// <summary>
        /// Mida del panell en % de l'original (50..100, passos de 10). Resposta immediata: es mostra al moment
        /// la imatge actual escalada a la nova mida, i en segon pla es redibuixa des de zero en vectorial a la
        /// mida exacta (sempre sobre l'original); en acabar substitueix la vista prèvia.
        /// </summary>
        public void SetZoom(int percent)
        {
            percent = Math.Clamp(percent, MinZoom, MaxZoom);
            if (percent == _zoom) return;
            if (_page == 1) return;   // el panell principal no té zoom
            float frac = MaxScroll > 0 ? (float)_scrollY / MaxScroll : 1f; // es manté el punt relatiu
            _zoom = percent;
            if (_bmp == null) { Invalidate(); return; }   // no visible: es construirà en mostrar-se
            CancelBackgroundRender();

            int cw = ContentWidth, ch = (int)Math.Ceiling(CurLH * (cw / LW));
            // 1) vista prèvia instantània: la imatge actual escalada (GDI, uns pocs mil·lisegons)
            if (!CreateSurface(cw, ch, out var pH, out var pB, out var pDC, out var pOld))
            {
                ReleaseCache();
                if (EnsureBitmap()) _scrollY = (int)Math.Round(frac * MaxScroll);
                Invalidate();
                return;
            }
            // escalat ràpid (només es veu un instant, fins que arriba la versió nítida)
            SetStretchBltMode(pDC, COLORONCOLOR);
            StretchBlt(pDC, 0, 0, cw, ch, _memDC, 0, 0, _bmp.Width, _contentH, SRCCOPY);
            FreeSurface(_bmp, _memDC, _oldObj, _hDib);
            _bmp = pB; _hDib = pH; _memDC = pDC; _oldObj = pOld;
            _s = cw / LW;
            _contentH = ch;
            _offX = (Width - cw) / 2;
            _offY = _isOverlay ? 0 : Math.Max(0, Height - _contentH);
            _scrollY = (int)Math.Round(frac * MaxScroll);
            ClampScroll();
            Invalidate();
            if (_isOverlay) UpdateOverlayRegion();

            // 2) render exacte en segon pla, a una superfície a part
            if (!CreateSurface(cw, ch, out var rH, out var rB, out var rDC, out var rOld)) return;
            _bgRendering = true;
            _bgDirty.Clear();
            var cts = new CancellationTokenSource();
            _bgCts = cts;
            int gen = ++_bgGen;
            float s = _s;
            var target = rB!;
            _bgTask = Task.Run(() =>
            {
                try { DrawContent(target, s, new Rectangle(0, 0, cw, ch), cts.Token); return !cts.IsCancellationRequested; }
                catch { return false; }
            });
            _bgTask.ContinueWith(t =>
            {
                bool ok = t.Status == TaskStatus.RanToCompletion && t.Result;
                if (IsDisposed || gen != _bgGen || !ok || _bmp == null || _bmp.Width != cw || _contentH != ch)
                {
                    FreeSurface(rB, rDC, rOld, rH);
                    return;
                }
                FreeSurface(_bmp, _memDC, _oldObj, _hDib);
                _bmp = rB; _hDib = rH; _memDC = rDC; _oldObj = rOld;
                _bgRendering = false;
                _bgTask = null; _bgCts = null;
                // el que hagi canviat mentre es calculava (tocs, animacions) es repinta ara
                var dirty = _bgDirty.ToArray();
                _bgDirty.Clear();
                foreach (var d in dirty) Redraw(d, invalidate: false);
                Invalidate();
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>Homogeneïtat de la textura (0 = granelluda, 1 = molt fina). Redibuixa els dos panells (provisional).</summary>
        public void SetTexture(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            if (Math.Abs(Oh.Texture - t) < 0.001f) return;
            int keepScroll = _scrollY;
            ReleaseCache();              // primer s'atura qualsevol render en segon pla que faci servir el gra
            Oh.Texture = t;
            Oh.RebuildGrain();
            if (EnsureBitmap()) { _scrollY = keepScroll; ClampScroll(); }
            Invalidate();
        }

        /// <summary>La propera vegada que es mostri, el panell apareix amb la vora de baix a baix de la pantalla.</summary>
        public void OpenAtBottom()
        {
            // panell principal: en entrar al mode A320 es comença només amb el main (overhead tancat)
            if (!_isOverlay && _page == 1)
            {
                if (_ov != null && _ov.Visible) { _ov._closing = false; _ov.Visible = false; }
                InvalidateButtons();
                return;
            }
            _openAtBottom = true;
            if (_bmp != null) { _scrollY = MaxScroll; _openAtBottom = false; Invalidate(); }
        }

        void DrawZoomButtons(Graphics g, Rectangle clip)
        {
            if (_isOverlay) return;
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool open = OverlayOpen;
            int z = _ov?._zoom ?? 100;
            // amb l'overhead obert, OVHD, + i − van dins un mateix marc: el zoom és només de l'overhead
            if (open)
            {
                var grp = Rectangle.Inflate(AllBtnsRect, 9, 9);
                using var gp = Oh.RoundRect(grp, ZoomBtnSize * 0.24f);
                using (var gb = new SolidBrush(Color.FromArgb(70, 0, 0, 0))) g.FillPath(gb, gp);
                using var pe = new Pen(Color.FromArgb(120, 255, 255, 255), Math.Max(1.5f, ZoomBtnSize / 40f)) { DashPattern = new[] { 3f, 2f } };
                g.DrawPath(pe, gp);
            }
            foreach (var (r, plus) in new[] { (PlusRect, true), (MinusRect, false) })
            {
                if (!open) continue;
                if (!r.IntersectsWith(clip)) continue;
                bool pressed = _btnPressed == (plus ? 1 : 2);
                bool enabled = plus ? z < MaxZoom : z > MinZoom;
                using (var path = Oh.RoundRect(r, r.Width * 0.18f))
                {
                    using (var b = new SolidBrush(Color.FromArgb(pressed ? 170 : 110, 0, 0, 0))) g.FillPath(b, path);
                    using var p = new Pen(CalibrationMode ? Color.FromArgb(220, 255, 140, 0) : Color.FromArgb(150, 255, 255, 255), Math.Max(2f, r.Width / 30f));
                    g.DrawPath(p, path);
                }
                float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, arm = r.Width * 0.26f;
                using var sp = new Pen(Color.FromArgb(enabled ? 230 : 80, 255, 255, 255), Math.Max(3f, r.Width / 11f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(sp, cx - arm, cy, cx + arm, cy);
                if (plus) g.DrawLine(sp, cx, cy - arm, cx, cy + arm);
            }
            // botó de canvi de pàgina: indica on porta (MAIN = panell principal, OVHD = overhead)
            var pr = PageRect;
            if (pr.IntersectsWith(clip))
            {
                bool pressed = _btnPressed == 3;
                using (var path = Oh.RoundRect(pr, pr.Width * 0.18f))
                {
                    using (var b = new SolidBrush(Color.FromArgb(pressed ? 170 : 110, 0, 0, 0))) g.FillPath(b, path);
                    using var p = new Pen(CalibrationMode ? Color.FromArgb(220, 255, 140, 0) : Color.FromArgb(150, 255, 255, 255), Math.Max(2f, pr.Width / 30f));
                    g.DrawPath(p, path);
                }
                using var f = new Font("Segoe UI", Math.Max(9f, pr.Width * 0.24f), FontStyle.Bold, GraphicsUnit.Pixel);
                using var tb = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
                var oldHint = g.TextRenderingHint;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.DrawString("OVHD", f, tb, pr.X + pr.Width / 2f, pr.Y + pr.Height / 2f, Oh.Center);
                g.TextRenderingHint = oldHint;
            }
            // botó TH: igual que OVHD; quan el quadrant es mostra, el fons és més clar
            var th = ThRect;
            if (th.IntersectsWith(clip))
            {
                bool pressed = _btnPressed == 5;
                using (var path = Oh.RoundRect(th, th.Width * 0.18f))
                {
                    var fill = _throttleShown ? Color.FromArgb(pressed ? 200 : 160, 95, 108, 122) : Color.FromArgb(pressed ? 170 : 110, 0, 0, 0);
                    using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                    using var p = new Pen(CalibrationMode ? Color.FromArgb(220, 255, 140, 0) : Color.FromArgb(150, 255, 255, 255), Math.Max(2f, th.Width / 30f));
                    g.DrawPath(p, path);
                }
                using var f2 = new Font("Segoe UI", Math.Max(9f, th.Width * 0.3f), FontStyle.Bold, GraphicsUnit.Pixel);
                using var tb2 = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
                var oldHint2 = g.TextRenderingHint;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.DrawString("TH", f2, tb2, th.X + th.Width / 2f, th.Y + th.Height / 2f, Oh.Center);
                g.TextRenderingHint = oldHint2;
            }
            g.SmoothingMode = oldMode;
        }

        int ZoomButtonAt(Point p) => _isOverlay ? 0 : Rectangle.Inflate(AreasRect, 8, 8).Contains(p) ? 4 : PageRect.Contains(p) ? 3 : ThRect.Contains(p) ? 5 : !OverlayOpen ? 0 : PlusRect.Contains(p) ? 1 : MinusRect.Contains(p) ? 2 : 0;

        void ToggleShowAreas()
        {
            s_showAreas = !s_showAreas;
            Invalidate();
            _ov?.Invalidate();
        }

        /// <summary>Ressalta en taronja les zones clicables dels elements (si el botó "mostrar àrees" està actiu).</summary>
        void DrawHitAreas(Graphics g, Rectangle clip)
        {
            if (!s_showAreas) return;
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            using var fill = new SolidBrush(Color.FromArgb(60, Color.Orange));
            using var pen = new Pen(Color.Orange, 2);
            float sh = CurShift;
            foreach (var el in _els)
                foreach (var r in el.HitAreas)
                {
                    // només el que respon de veritat a un toc (fora ombres, indicadors, etc.)
                    if (!IsTappable(el) || !el.HitTest(new PointF(r.X + r.Width / 2, r.Y + r.Height / 2))) continue;
                    var sr = Rectangle.Round(new RectangleF(_offX + r.X * _s, _offY - _scrollY + (r.Y - sh) * _s, r.Width * _s, r.Height * _s));
                    if (!sr.IntersectsWith(clip)) continue;
                    g.FillRectangle(fill, sr);
                    g.DrawRectangle(pen, sr);
                }
            g.SmoothingMode = oldMode;
        }

        static readonly Dictionary<Type, bool> s_tappable = new();
        /// <summary>true si el tipus d'element redefineix Tap (és a dir, fa alguna cosa en tocar-lo).</summary>
        static bool IsTappable(OhElement el)
        {
            var t = el.GetType();
            if (!s_tappable.TryGetValue(t, out bool ok))
                s_tappable[t] = ok = t.GetMethod(nameof(OhElement.Tap))!.DeclaringType != typeof(OhElement);
            return ok;
        }

        void DrawAreasButton(Graphics g)
        {
            var r = AreasRect;
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool on = s_showAreas, pressed = _btnPressed == 4;
            using (var path = Oh.RoundRect(r, r.Width * 0.18f))
            {
                using (var b = new SolidBrush(on ? Color.FromArgb(pressed ? 110 : 70, AreasColor) : Color.FromArgb(pressed ? 120 : 50, 0, 0, 0))) g.FillPath(b, path);
                using var p = new Pen(CalibrationMode ? Color.FromArgb(220, 255, 140, 0) : Color.FromArgb(on ? 150 : 80, AreasColor), Math.Max(1.5f, r.Width / 40f));
                g.DrawPath(p, path);
            }
            using var f = new Font("Segoe UI Emoji", Math.Max(12f, r.Width * 0.5f), FontStyle.Bold, GraphicsUnit.Pixel);
            using var tb = new SolidBrush(on ? Color.FromArgb(200, 255, 235, 200) : Color.FromArgb(95, AreasColor));
            var oldHint = g.TextRenderingHint;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.DrawString("👆", f, tb, r.X + r.Width / 2f, r.Y + r.Height / 2f, Oh.Center);
            g.TextRenderingHint = oldHint;
            g.SmoothingMode = oldMode;
        }

        Rectangle ScrollBarRect()
        {
            int bw = Math.Max(6, Width / 320);
            int h = Math.Max(bw * 4, (int)((long)Height * Height / Math.Max(1, _contentH)));
            int maxScroll = Math.Max(1, _contentH - Height);
            int y = (int)((long)_scrollY * (Height - h) / maxScroll);
            return new Rectangle(Width - bw - 3, y, bw, h);
        }

        // ──────────────────────────── Animació / scroll ────────────────────────────

        void StartAnim()
        {
            if (_anim.Enabled) return;
            _lastTick = _clock.Elapsed.TotalSeconds;
            _anim.Start();
        }

        void AnimTick()
        {
            double now = _clock.Elapsed.TotalSeconds;
            float dt = (float)Math.Min(0.05, now - _lastTick);
            _lastTick = now;

            // Desplaçament amb el dit: com a màxim un cop per fotograma, amb l'última posició rebuda
            if (_hasPendingScroll) { _hasPendingScroll = false; ScrollTo(_pendingScroll); }

            // (capa overhead) obertura: baixa des de dalt fins que la línia de tall arriba a la vora superior
            if (_opening)
            {
                int target = MaxScroll;
                int step = Math.Max(4, (int)(dt * Math.Max(Height * 1.0f, (_scrollY - target) / 0.30f)));
                int ny = Math.Max(target, _scrollY - step);
                ScrollTo(ny);
                if (ny <= target) { _opening = false; ClampScroll(); }
            }

            // (capa overhead) tancament: puja accelerant fins que surt per dalt i s'amaga
            if (_closing)
            {
                // velocitat: des del topall surt en ~0,3 s; si estava desplegat, una mica més
                int step = Math.Max(4, (int)(dt * Math.Max(Height * 1.0f, OverlayBottom / 0.35f)));
                ScrollTo(_scrollY + step);
                if (OverlayBottom <= 0) FinishClose();
            }

            foreach (var el in _animating.ToArray())
            {
                el.Tick(dt);
                Redraw(TopDirty(el));
                if (!el.Animating) _animating.Remove(el);
            }

            if (_tests.Count > 0)
            {
                var t = DateTime.UtcNow;
                for (int i = _tests.Count - 1; i >= 0; i--)
                    if (t >= _tests[i].until)
                    {
                        _tests[i].el.Test = false;
                        Redraw(TopDirty(_tests[i].owner));
                        _tests.RemoveAt(i);
                    }
            }

            if (_flingV != 0)
            {
                int before = _scrollY;
                ScrollTo(_scrollY - (int)Math.Round(_flingV * dt));
                _flingV *= (float)Math.Exp(-dt * 4.0);
                if (Math.Abs(_flingV) < 40 || before == _scrollY) _flingV = 0;
            }

            bool barPending = _barHideAt != DateTime.MinValue;
            if (barPending && DateTime.UtcNow >= _barHideAt)
            {
                _barHideAt = DateTime.MinValue;
                barPending = false;
                Invalidate(new Rectangle(Width - Math.Max(6, Width / 320) - 6, 0, Math.Max(6, Width / 320) + 6, Height));
            }

            if (_animating.Count == 0 && _tests.Count == 0 && _flingV == 0 && !barPending && !_hasPendingScroll && !_down && !_closing && !_opening)
                _anim.Stop();
        }

        void ClampScroll() { if (!_closing && !_opening) _scrollY = Math.Clamp(_scrollY, 0, MaxScroll); }

        void ScrollTo(int y)
        {
            int old = _scrollY;
            _scrollY = y;
            ClampScroll();
            int d = old - _scrollY;
            if (d == 0) return;
            _barHideAt = DateTime.UtcNow.AddSeconds(1.0);
            StartAnim();
            if (IsHandleCreated && Math.Abs(d) < Height)
            {
                // Primer es buida qualsevol repintat pendent (perquè no quedi desplaçat a mitges), després
                // Windows mou els píxels que ja són a pantalla i només cal pintar la franja nova.
                Update();
                ScrollWindowEx(Handle, 0, d, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, SW_INVALIDATE);
                int bw = Math.Max(6, Width / 320) + 6;
                Invalidate(new Rectangle(Width - bw, 0, bw, Height)); // la barreta de posició es redibuixa sencera
                // Els botons no es desplacen amb el panell: es repinten al seu lloc i també on Windows n'ha
                // deixat la còpia desplaçada (si no, queden rastres repetits en arrossegar).
                // Es repinta tota la franja vertical de cada botó (de dalt a baix): qualsevol còpia
                // desplaçada, d'aquest pas o d'un d'anterior, queda esborrada.
                if (!_isOverlay)
                    foreach (var zr in new[] { PlusRect, MinusRect, PageRect })
                        Invalidate(new Rectangle(zr.X - 3, 0, zr.Width + 6, Height));
            }
            else Invalidate();
            if (_isOverlay) UpdateOverlayRegion();
            Update();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Desactiva els gestos/efectes tàctils propis de Windows (mantenir premut, flicks, cercles de
            // feedback...) sobre aquest control: el desplaçament amb el dit el gestionem nosaltres.
            try
            {
                const int flags = 0x00000001 | 0x00000008 | 0x00000010 | 0x00010000 | 0x00080000 | 0x00100000;
                SetProp(Handle, "MicrosoftTabletPenServiceProperty", (IntPtr)flags);
                int off = 0;
                foreach (int fb in new[] { 1, 7, 8, 9, 10, 11 })
                    SetWindowFeedbackSetting(Handle, fb, 0, sizeof(int), ref off);
            }
            catch { }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int zb = ZoomButtonAt(e.Location);
            if (zb != 0)
            {
                if (CalibrationMode)
                {
                    _btnDrag = zb;
                    var r = BtnRect(zb);
                    _btnDragOff = new Point(e.X - r.X, e.Y - r.Y);
                }
                else { _btnPressed = zb; Invalidate(Rectangle.Inflate(BtnRect(zb), 6, 6)); }
                return;
            }
            if (_closing || _opening) return;
            // (panell principal) gest des de la vora de dalt amb l'overhead tancat: obrirà l'overhead
            _edgeStart = !_isOverlay && !OverlayOpen && e.Y <= EdgePx;
            _down = true; _dragging = false; _flingV = 0; _velY = 0;
            _downPt = e.Location; _downScroll = _scrollY;
            // (capa) si el gest comença amb l'overhead al topall, arrossegar amunt el tanca
            _startAtCut = _isOverlay && _scrollY >= MaxScroll - 1;
            _lastMoveY = e.Y; _lastMoveT = _clock.Elapsed.TotalSeconds;
            // acció mantinguda (p.ex. gir continu del rotatiu PULL STD); amb el dit ja s'ha començat a WM_POINTERDOWN
            if (!_touchPress && !_edgeStart) BeginPress(e.Location);
        }

        void BeginPress(Point pt)
        {
            if (_bmp == null || ElementAt(pt, out var pp) is not OhElement pe) return;
            _pressEl = pe;
            pe.PressStart(pp);
            if (pe.Animating) { _animating.Add(pe); StartAnim(); }
        }

        // Amb el dit, Windows no envia el WM_LBUTTONDOWN fins que el dit s'aixeca o es mou: per a les accions
        // mantingudes es fa servir directament el WM_POINTERDOWN / WM_POINTERUP tàctil (la resta, igual que abans).
        const int WM_POINTERDOWN = 0x0246, WM_POINTERUP = 0x0247;
        [DllImport("user32.dll")] static extern bool GetPointerType(uint pointerId, out int pointerType);
        bool _touchPress;   // el toc actual l'ha començat un WM_POINTERDOWN tàctil (fins al OnMouseUp següent)

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_POINTERDOWN || m.Msg == WM_POINTERUP)
            {
                uint id = (uint)((long)m.WParam & 0xFFFF);
                if (GetPointerType(id, out int type) && type == 2 /* PT_TOUCH */)
                {
                    long lp = (long)m.LParam;
                    var pt = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                    if (m.Msg == WM_POINTERDOWN)
                    {
                        EndPress();
                        _touchPress = true;
                        if (ZoomButtonAt(pt) == 0 && !_closing && !_opening && !(!_isOverlay && !OverlayOpen && pt.Y <= EdgePx))
                            BeginPress(pt);
                    }
                    else EndPress();
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_btnDrag != 0)
            {
                var old = BtnRect(_btnDrag);
                var np = new Point(Math.Clamp(e.X - _btnDragOff.X, 0, Width - old.Width), Math.Clamp(e.Y - _btnDragOff.Y, 0, Height - old.Height));
                if (_btnDrag == 1) PlusLocation = np; else if (_btnDrag == 2) MinusLocation = np; else if (_btnDrag == 4) AreasLocation = np; else if (_btnDrag == 5) ThLocation = np; else PageLocation = np;
                Invalidate(Rectangle.Inflate(old, 6, 6)); Invalidate(Rectangle.Inflate(BtnRect(_btnDrag), 6, 6));
                if (_ov != null && _ov.Visible) _ov.UpdateOverlayRegion();
                return;
            }
            if (!_down) return;
            int dy = e.Y - _downPt.Y;
            if (!_dragging && Math.Abs(dy) > Math.Max(10, Width / 200)) { _dragging = true; EndPress(); }
            if (!_dragging) return;
            if (_isOverlay && _startAtCut && dy < 0) { BeginClose(); return; }
            if (_edgeStart && dy > 0) { _edgeStart = false; _down = false; _dragging = false; OpenOverlayAnimated(); return; }
            double t = _clock.Elapsed.TotalSeconds;
            double ddt = t - _lastMoveT;
            if (ddt > 0.001)
            {
                float v = (float)((e.Y - _lastMoveY) / ddt);
                _velY = _velY * 0.6f + v * 0.4f;
                _lastMoveT = t; _lastMoveY = e.Y;
            }
            _pendingScroll = _downScroll - dy;
            _hasPendingScroll = true;
            StartAnim();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_btnDrag != 0) { _btnDrag = 0; ZoomButtonsMoved?.Invoke(); return; }
            if (_btnPressed != 0)
            {
                int b = _btnPressed;
                _btnPressed = 0;
                Invalidate(PlusRect); Invalidate(MinusRect); Invalidate(PageRect); Invalidate(Rectangle.Inflate(AreasRect, 6, 6));
                if (ZoomButtonAt(e.Location) == b)
                {
                    if (b == 4) ToggleShowAreas();
                    else if (b == 5) { ThrottleVisible = !ThrottleVisible; ThrottleVisibleChanged?.Invoke(ThrottleVisible); }
                    else if (b == 3) ToggleOverlay();
                    else if (_ov != null) { _ov.SetZoom(_ov._zoom + (b == 1 ? 10 : -10)); InvalidateButtons(); }
                }
                return;
            }
            if (!_down) return;
            _down = false;
            EndPress();
            _touchPress = false;
            if (_dragging)
            {
                bool recent = _clock.Elapsed.TotalSeconds - _lastMoveT < 0.08;
                if (recent && Math.Abs(_velY) > 300) { _flingV = _velY; StartAnim(); }
                return;
            }
            TapAt(e.Location);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollTo(_scrollY - e.Delta * Math.Max(1, Height / 12) / 120);
        }

        OhElement? ElementAt(Point pt, out PointF p)
        {
            p = new PointF((pt.X - _offX) / _s, (pt.Y - _offY + _scrollY) / _s + CurShift);
            for (int i = _els.Count - 1; i >= 0; i--)
                if (_els[i].HitTest(p)) return _els[i];
            return null;
        }

        void TapAt(Point pt)
        {
            if (!EnsureBitmap()) return;
            ElementAt(pt, out var p)?.Tap(p);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_ov != null) _ov.Bounds = ClientRectangle;
            if (_bmp != null && _bmp.Width != ContentWidth) ReleaseCache();
            _offY = _isOverlay ? 0 : Math.Max(0, Height - _contentH);
            ClampScroll();
            Invalidate();
            if (!_isOverlay) HolesChanged?.Invoke();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            // la capa overhead conserva el dibuix en amagar-se (tornar-la a obrir és immediat); el panell principal,
            // en sortir del mode A320, allibera el seu i el de la capa
            if (!Visible && !_isOverlay) { ReleaseCache(); _ov?.ReleaseCache(); }
            if (_isOverlay && Visible) UpdateOverlayRegion();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { ReleaseCache(); _anim.Dispose(); }
            base.Dispose(disposing);
        }

        // ═════════════════════════════════════ MAQUETACIÓ ═════════════════════════════════════

        static readonly Color G = Oh.SilkGreen;
        static readonly float[] TempAngles = { -120, -90, -60, -30, 0, 30, 60, 90, 120 };

        T Add<T>(T el) where T : OhElement
        {
            Register(el);
            _els.Add(el);
            return el;
        }

        void Register(OhElement el)
        {
            el.Owner = this;
            _byId[el.Id] = el;
            if (el is OhGuard g) Register(g.Inner);
        }

        void Panel(float x, float y, float w, float h) => _silk.Add(new OhPanel(new RectangleF(x, y, w, h)));
        // Escala de serigrafia i interruptors. EXT LT i AIR COND ja tenen les mides finals (escala 1);
        // la resta de l'overhead s'iguala proporcionalment amb TextScale.
        const float TextScale = 1.22f;
        // Mida patró dels interruptors (la d'EXT LT): la resta de l'overhead la segueix
        const float ExtToggleSize = 14.5f;
        float _ls = TextScale;
        void L(string t, float x, float y, float size = 6.5f, bool bold = true) => _silk.Add(new OhLabel(t, x, y, size * _ls, Oh.Silk, bold));
        void VT(string t, float x, float y, float size = 10f) => _silk.Add(new OhVText(t, x, y, size * _ls));
        void Box(string t, float x, float y, float size = 6.5f) => _silk.Add(new OhBoxLabel(t, x, y, size * _ls, Oh.Silk));
        void Title(string t, float cx, float y, float x0, float x1, float size = 9.5f) => _silk.Add(new OhTitle(t, cx, y, x0, x1, size * _ls));
        void Arrow(float x, float y, float dir) => _silk.Add(new OhArrow(x, y, dir, 4.2f, G));
        void Groove(float x0, float y0, float x1, float y1) => _silk.Add(new OhGroove(x0, y0, x1, y1));
        /// <summary>Etiqueta blanca dins un "⊓" verd (línia a dalt i laterals avall), com GND HP / APU BLEED.</summary>
        void GreenBracket(string t, float cx, float top, float halfW)
        {
            Ln(cx - halfW, top + 11, cx - halfW, top, cx + halfW, top, cx + halfW, top + 11);
            L(t, cx, top + 6.5f, 6.5f);
        }

        void Ln(params float[] xy)
        {
            var pts = new PointF[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
            _silk.Add(new OhLine(G, 2.6f, pts));
        }
        void WLn(params float[] xy)
        {
            var pts = new PointF[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
            _silk.Add(new OhLine(Oh.Silk, 1.1f, pts));
        }

        OhPushButton MakePB(string id, float cx, float top, string upper, string lower, float w = 44, float h = 40, bool momentary = false) =>
            new(id, new RectangleF(cx - w / 2, top, w, h), upper, lower, momentary);

        OhPushButton PB(string id, float cx, float top, string upper, string lower, float w = 44, float h = 40, bool momentary = false) =>
            Add(MakePB(id, cx, top, upper, lower, w, h, momentary));

        OhGuard Guarded(OhElement inner, Color col, int centerAlpha) => Add(new OhGuard(inner, col, centerAlpha));
        OhGuard RedGuard(string id, float cx, float top, string up, string lo, float w = 44, float h = 44, bool mom = false) =>
            Guarded(MakePB(id, cx, top, up, lo, w, h, mom), Oh.GuardRed, 235);
        OhGuard GrayGuard(string id, float cx, float top, string up, string lo, float w = 44, float h = 44, bool mom = false) =>
            Guarded(MakePB(id, cx, top, up, lo, w, h, mom), Oh.GuardGray, 225);
        OhGuard FrameGuard(string id, float cx, float top, string up, string lo, float w = 40, float h = 44) =>
            Guarded(MakePB(id, cx, top, up, lo, w, h), Oh.GuardRed, 40);

        OhKnob Knob(string id, float cx, float cy, float r, int index, float[] angles, KnobStyle style = KnobStyle.Pointer) =>
            Add(new OhKnob(id, cx, cy, r, index, angles, style));
        OhKnob TempKnob(string id, float cx, float cy, float r = 22)
        {
            _silk.Add(new OhDial(cx, cy, r + 2.5f, TempAngles, null, 5f));
            return Knob(id, cx, cy, r, 4, TempAngles);
        }
        OhToggle Tog(string id, float cx, float cy, int n, int index, bool paddle = false, float size = 10f, bool spring = false) =>
            // Tots els interruptors de palanca/pala de l'overhead tenen la mida d'EXT LT (patró)
            Add(new OhToggle(id, cx, cy, n, index, paddle ? ToggleStyle.Paddle : ToggleStyle.Bat, ExtToggleSize) { SpringToCenter = spring });
        OhToggle TogS(string id, float cx, float cy, int n, int index, ToggleStyle style, float size) =>
            Add(new OhToggle(id, cx, cy, n, index, style, size));
        OhRoundButton Round(string id, float cx, float cy, float r) => Add(new OhRoundButton(id, cx, cy, r));
        OhSeg7 Seg(string id, float x, float y, float w, float h, string text, int cells, string suffix = "") =>
            Add(new OhSeg7(id, new RectangleF(x, y, w, h), text, cells, suffix));
        OhKey Key(string id, float x, float y, float w, float h, string label) => Add(new OhKey(id, new RectangleF(x, y, w, h), label));
        OhVolKnob Vol(string id, float cx, float cy, float r) => Add(new OhVolKnob(id, cx, cy, r));

        void Build()
        {
            // pàgina 0: overhead
            _page = 0;
            _pages[0].LH = LH;
            _pages[0].Frame = FramePoly;
            _ls = TextScale;
            BuildLeft();
            BuildCenter();
            BuildRight();
            _els = _els.OrderBy(e => e.Z).ToList();
            // pàgina 1: panell principal
            _page = 1;
            _pages[1].LH = MainLH;
            _pages[1].Shift = MainTopCut;
            _pages[1].Frame = MainFramePoly;
            _ls = 1f;
            BuildMain();
            _els = _els.OrderBy(e => e.Z).ToList();
            _page = 0;
            _pages[0].Visited = true;
        }

        // ───────────────────────────── Columna esquerra ─────────────────────────────
        void BuildLeft()
        {
            Panel(26, 14, 282, 112);
            L("PA", 167, 32, 11f);
            PB("PA_SYS", 167, 50, "SYS ON", "••").UpperColor = Oh.Green;

            Panel(26, 132, 282, 62);

            // ADIRS
            Panel(26, 200, 282, 262);
            L("ADIRS", 167, 214, 11f);
            _silk.Add(new OhDarkWindow(new RectangleF(144, 224, 46, 15)));
            float[] irx = { 82, 166, 250 };
            string[] ir = { "IR1", "IR3", "IR2" }, adr = { "ADR1", "ADR3", "ADR2" };
            float[] modeAng = { -60, 0, 60 };
            for (int i = 0; i < 3; i++)
            {
                float x = irx[i];
                L(ir[i], x, 255, 7f);
                PB("ADIRS_" + ir[i], x, 263, "FAULT", "ALIGN");
                L("OFF", x - 30, 326, 6f); L("NAV", x, 318, 6f); L("ATT", x + 30, 326, 6f);
                _silk.Add(new OhDial(x, 352, 25, modeAng, null, 5f));
                Knob("ADIRS_" + ir[i] + "_MODE", x, 352, 22, 0, modeAng);
                L(adr[i], x, 386, 6.5f);
                PB("ADIRS_" + adr[i], x, 395, "FAULT", "OFF");
            }

            // FLT CTL 1
            Panel(26, 468, 282, 88);
            L("FLT CTL", 196, 479, 10f);
            L("ELAC 1", 140, 491); L("SEC 1", 196, 491); L("FAC 1", 252, 491);
            PB("FLTCTL_ELAC1", 140, 500, "FAULT", "OFF");
            PB("FLTCTL_SEC1", 196, 500, "FAULT", "OFF");
            PB("FLTCTL_FAC1", 252, 500, "FAULT", "OFF");

            // Panell inferior esquerre (EVAC, EMER ELEC, GPWS, RCDR, OXYGEN, CALLS, WIPER)
            _silk.Add(new OhPanel(new PointF[] { new(26, 562), new(308, 562), new(308, 1248), new(26, 1098) },
                                  new PointF[] { new(35, 571), new(299, 571), new(35, 1088), new(298, 1232) }));
            L("COMMAND", 131, 573, 6f); L("EVAC", 195, 573, 9f); L("HORN SHUT OFF", 195, 586, 5.5f); L("CAPT & PURS", 262, 581, 6f);
            GrayGuard("EVAC_COMMAND", 131, 586, "EVAC", "ON", 44, 42);
            Round("EVAC_HORN_SHUTOFF", 195, 612, 13);
            Tog("EVAC_CAPT_PURS", 262, 609, 2, 0, false, 10);
            L("CAPT", 262, 638, 6f);

            Title("EMER ELEC PWR", 167, 650, 44, 298);
            L("EMER GEN TEST", 76, 664, 5.5f);
            GrayGuard("EMER_GEN_TEST", 76, 674, "••", "••", 42, 44, true);
            L("GEN 1 LINE", 135, 677, 5.5f);
            PB("EMER_GEN1_LINE", 135, 686, "SMOKE", "OFF", 42);
            L("RAT &", 192, 664, 5.5f); L("EMER GEN", 192, 673, 5.5f);
            PB("EMER_RAT_EMER_GEN", 192, 686, "FAULT", "••", 42);
            L("MAN ON", 248, 665, 6f);
            RedGuard("EMER_MAN_ON", 248, 676, "••", "••", 42, 46, true);
            VT("AUTO", 285, 702, 5.5f);

            Title("GPWS", 178, 748, 44, 298);
            L("TERR", 60, 768); L("SYS", 120, 768);
            L("G/S", 172, 762); L("MODE", 172, 771); L("FLAP", 222, 762); L("MODE", 222, 771);
            L("LDG", 274, 762); L("FLAP 3", 274, 771);
            PB("GPWS_TERR", 60, 780, "FAULT", "OFF", 42);
            PB("GPWS_SYS", 120, 780, "FAULT", "OFF", 42);
            PB("GPWS_GS_MODE", 172, 780, "••", "OFF", 42);
            PB("GPWS_FLAP_MODE", 222, 780, "••", "OFF", 42);
            PB("GPWS_LDG_FLAP3", 274, 780, "••", "ON", 42);

            Title("RCDR", 196, 836, 96, 298);
            L("GND CTL", 133, 848); L("CVR ERASE", 196, 848); L("CVR TEST", 260, 848);
            PB("RCDR_GND_CTL", 133, 857, "••", "ON");
            VT("AUTO", 166, 877, 5.5f);
            Round("RCDR_CVR_ERASE", 196, 878, 13);
            Round("RCDR_CVR_TEST", 260, 878, 13);

            Title("OXYGEN", 196, 910, 96, 298);
            L("MASK MAN ON", 133, 922, 5.5f);
            RedGuard("OXY_MASK_MAN_ON", 133, 931, "••", "••", 42, 46, true);
            VT("AUTO", 167, 956, 5.5f);
            L("PASSENGER", 199, 926, 6f);
            PB("OXY_PASSENGER", 199, 938, "SYS ON", "••", 42);
            L("CREW", 262, 926, 6f); L("SUPPLY", 262, 935, 6f);
            PB("OXY_CREW_SUPPLY", 262, 946, "••", "OFF", 42);

            Title("CALLS", 167, 1004, 44, 298);
            L("MECH", 70, 1020); L("FWD", 165, 1020); L("AFT", 214, 1020); L("EMER", 266, 1011);
            Round("CALLS_MECH", 70, 1047, 15);
            Round("CALLS_FWD", 165, 1047, 15);
            Round("CALLS_AFT", 214, 1047, 15);
            GrayGuard("CALLS_EMER", 266, 1022, "CALL", "ON", 42, 42);

            L("RAIN RPLNT", 130, 1084);
            Round("RAIN_RPLNT_1", 130, 1111, 13);
            L("WIPER", 232, 1082, 7f); L("OFF", 232, 1093, 6f);
            float[] wip = { -15, 45, 105 };
            _silk.Add(new OhDial(232, 1134, 24.5f, wip, null, 5f));
            Knob("WIPER_1", 232, 1134, 22, 0, wip);
            L("SLOW", 274, 1113, 6f); L("FAST", 272, 1163, 6f);
        }

        // ───────────────────────────── Columna central ─────────────────────────────
        void BuildCenter()
        {
            Panel(314, 14, 626, 130);

            // ENG / APU FIRE
            Panel(314, 150, 626, 134);
            VT("FIRE", 328, 217); VT("FIRE", 926, 217);
            FirePanel("ENG1", 447, "ENG 1", 360, 532, 404, 362);
            FirePanel("ENG2", 811, "ENG 2", 720, 893, 764, 897);
            L("APU", 630, 159, 8f);
            var apuFire = new OhFireButton("APU_FIRE", new RectangleF(585, 168, 90, 54));
            Guarded(apuFire, Oh.GuardRed, 60);
            var apuAg = PB("APU_AGENT", 594, 238, "SQUIB", "DISCH", 42, 36, true);
            L("AGENT", 641, 278, 6f);
            L("TEST", 676, 238, 6f);
            Round("APU_FIRE_TEST", 676, 258, 10).OnChange = _ => FlashTest(2.5f, "APU_FIRE", "APU_AGENT");
            apuFire.OnChange = _ => { SetLegend("APU_AGENT", apuFire.In ? 1f : 0f, null); };
            apuAg.OnChange = _ => { if (apuAg.UpperLevel > 0.5f) SetLegend("APU_AGENT", null, 1f); };

            // HYD / FUEL
            Panel(314, 290, 626, 266);
            VT("HYD", 328, 380); VT("HYD", 926, 380);
            VT("FUEL", 328, 485); VT("FUEL", 926, 485);
            L("40 VU", 916, 300, 5.5f, false);
            Box("GREEN", 440, 304); Box("BLUE", 627, 304); Box("YELLOW", 810, 304);
            Ln(440, 311, 440, 354);
            Ln(440, 326, 698, 326);
            Ln(739, 326, 810, 326);
            Ln(627, 311, 627, 345);
            Ln(810, 311, 810, 354);
            Ln(810, 336, 877, 336, 877, 344);
            Arrow(680, 326, 0); Arrow(758, 326, 180);
            Box("PTU", 718, 326, 6f);
            L("ENG 1 PUMP", 440, 362);
            PB("HYD_ENG1_PUMP", 440, 370, "FAULT", "OFF");
            L("RAT MAN ON", 535, 341);
            RedGuard("HYD_RAT_MAN_ON", 535, 352, "••", "••", 38, 44, true);
            L("ELEC PUMP", 627, 352);
            PB("HYD_BLUE_ELEC_PUMP", 627, 360, "FAULT", "ON");
            VT("AUTO", 659, 380, 5.5f);
            PB("HYD_PTU", 718, 340, "FAULT", "OFF");
            VT("AUTO", 749, 360, 5.5f);
            L("ENG 2 PUMP", 810, 362);
            PB("HYD_ENG2_PUMP", 810, 370, "FAULT", "OFF");
            L("ELEC PUMP", 877, 352);
            PB("HYD_YELLOW_ELEC_PUMP", 877, 360, "FAULT", "ON");

            Box("ENG 1", 440, 437); Box("APU", 564, 437); Box("ENG 2", 810, 437);
            L("X FEED", 627, 424);
            Ln(455, 437, 551, 437); Ln(577, 437, 604, 437); Ln(650, 437, 795, 437);
            Ln(440, 444, 440, 484); Ln(810, 444, 810, 484);
            Ln(388, 484, 866, 484);
            Arrow(440, 458, 270); Arrow(810, 458, 270);
            Arrow(520, 484, 180); Arrow(735, 484, 0);
            PB("FUEL_XFEED", 627, 432, "OPEN", "ON");
            L("1", 395, 494); L("L TK PUMPS", 440, 494); L("2", 486, 494);
            L("CTR TK", 570, 476); L("L XFR", 570, 494);
            L("MODE SEL", 626, 494);
            L("CTR TK", 686, 476); L("R XFR", 686, 494);
            L("1", 765, 494); L("R TK PUMPS", 812, 494); L("2", 858, 494);
            PB("FUEL_L_TK_PUMP1", 414, 502, "FAULT", "OFF");
            PB("FUEL_L_TK_PUMP2", 466, 502, "FAULT", "OFF");
            PB("FUEL_CTR_TK_L_XFR", 570, 502, "FAULT", "OFF");
            PB("FUEL_MODE_SEL", 626, 502, "FAULT", "MAN");
            VT("AUTO", 657, 522, 5.5f);
            PB("FUEL_CTR_TK_R_XFR", 686, 502, "FAULT", "OFF");
            PB("FUEL_R_TK_PUMP1", 786, 502, "FAULT", "OFF");
            PB("FUEL_R_TK_PUMP2", 838, 502, "FAULT", "OFF");

            // ELEC
            Panel(314, 562, 626, 196);
            VT("ELEC", 328, 662); VT("ELEC", 926, 662);
            L("BAT 1", 518, 573);
            Seg("ELEC_BAT1_V", 486, 581, 66, 34, "28.8", 3, "V");
            L("1", 571, 573); L("BAT", 606, 573); L("2", 641, 573);
            PB("ELEC_BAT1", 583, 583, "FAULT", "OFF", 40);
            PB("ELEC_BAT2", 629, 583, "FAULT", "OFF", 40);
            L("BAT 2", 690, 573);
            Seg("ELEC_BAT2_V", 657, 581, 66, 34, "28.9", 3, "V");
            Box("DC BUS 1", 441, 578); Box("DC BUS 2", 810, 578);
            Ln(441, 585, 441, 630); Ln(810, 585, 810, 630);
            Arrow(441, 600, 270); Arrow(810, 600, 270);
            L("AC ESS FEED", 756, 598, 5.5f);
            PB("ELEC_AC_ESS_FEED", 756, 606, "FAULT", "ALTN");
            Box("AC BUS 1", 441, 637); Box("AC ESS BUS", 627, 637); Box("AC BUS 2", 810, 637);
            Ln(467, 637, 597, 637); Ln(657, 637, 785, 637);
            L("GALLEY", 378, 612);
            PB("ELEC_GALLEY", 378, 620, "FAULT", "OFF");
            VT("AUTO", 409, 640, 5.5f);
            L("BUS TIE", 627, 656);
            PB("ELEC_BUS_TIE", 627, 664, "••", "OFF");
            VT("AUTO", 658, 684, 5.5f);
            Ln(441, 644, 441, 678, 605, 678);
            Ln(649, 678, 810, 678, 810, 644);
            L("IDG 1", 378, 686);
            FrameGuard("ELEC_IDG1", 378, 695, "FAULT", "••");
            L("GEN 1", 437, 690);
            PB("ELEC_GEN1", 437, 698, "FAULT", "OFF");
            L("APU GEN", 550, 690);
            PB("ELEC_APU_GEN", 550, 698, "FAULT", "OFF");
            L("EXT PWR", 705, 690);
            PB("ELEC_EXT_PWR", 705, 698, "AVAIL", "ON");
            L("GEN 2", 810, 690);
            PB("ELEC_GEN2", 810, 698, "FAULT", "OFF");
            L("IDG 2", 876, 686);
            FrameGuard("ELEC_IDG2", 876, 695, "FAULT", "••");

            // AIR COND — segons la fotografia de referència (mides ja definitives: escala 1)
            _ls = 1f;
            Panel(314, 764, 626, 228);
            WLn(328, 800, 328, 956); VT("AIR", 340, 834, 10.5f); VT("COND", 340, 916, 10.5f);
            WLn(926, 800, 926, 956); VT("AIR", 914, 834, 10.5f); VT("COND", 914, 916, 10.5f);
            WLn(352, 982, 902, 982);

            L("PACK FLOW", 388, 771, 7.5f); L("NORM", 388, 780, 6.5f); L("LO", 359, 801, 6.5f); L("HI", 418, 801, 6.5f);
            float[] pf = { -55, 0, 55 };
            _silk.Add(new OhArcDial(388, 822, 25, -55, 55, pf, false));
            Knob("AIRCOND_PACK_FLOW", 388, 822, 19, 1, pf);

            float[] tk = { 534, 628, 722 };
            string[] tl = { "COCKPIT", "FWD CABIN", "AFT CABIN" };
            string[] tid = { "AIRCOND_CKPT_TEMP", "AIRCOND_FWD_CABIN_TEMP", "AIRCOND_AFT_CABIN_TEMP" };
            for (int i = 0; i < 3; i++)
            {
                Box(tl[i], tk[i], 775, 7f);
                _silk.Add(new OhArcDial(tk[i], 822, 26, -125, 125, new float[] { -125, -75, -25, 25, 75, 125 }, true));
                Knob(tid[i], tk[i], 822, 20, 4, TempAngles);
                L("COLD", tk[i] - 27, 851, 6.5f); L("HOT", tk[i] + 25, 851, 6.5f);
                Ln(tk[i], 849, tk[i], 860);
            }

            L("HOT AIR", 859, 790, 7f);
            PB("AIRCOND_HOT_AIR", 859, 798, "FAULT", "OFF");
            L("PACK 1", 374, 860, 7f);
            PB("AIRCOND_PACK1", 374, 868, "FAULT", "OFF");
            L("PACK 2", 880, 860, 7f);
            PB("AIRCOND_PACK2", 880, 868, "FAULT", "OFF");

            // sinòptic: línia superior (alimenta les 3 zones), PACK 1/2, línia inferior de purga
            Ln(450, 868, 450, 860, 809, 860, 809, 868);
            Box("PACK 1", 450, 874, 7f); Box("PACK 2", 809, 874, 7f);
            Ln(450, 881, 450, 888, 712, 888);
            Ln(728, 888, 809, 888, 809, 881);
            _silk.Add(new OhValve(720, 888, 7.5f));
            L("X BLEED", 720, 875, 7f);
            // RAM AIR: vàlvula antiretorn (semicercle) i fletxa cap a la línia superior
            var arc = new List<float>();
            for (int k = 0; k <= 8; k++) { double a = Math.PI + k * Math.PI / 8; arc.Add(545 + 6.5f * (float)Math.Cos(a)); arc.Add(888 + 6.5f * (float)Math.Sin(a)); }
            Ln(arc.ToArray());
            Ln(545, 881, 545, 863); Arrow(545, 867, 270);
            Arrow(497, 894, 90); L("START", 497, 903, 6.5f);
            Arrow(768, 894, 90); L("START", 768, 903, 6.5f);
            Arrow(589, 893, 270); GreenBracket("GND HP", 589, 898, 18);
            Arrow(633, 893, 270); GreenBracket("APU BLEED", 633, 898, 22);
            Ln(450, 888, 450, 905); GreenBracket("ENG 1 BLEED", 450, 905, 26);
            Ln(809, 888, 809, 905); GreenBracket("ENG 2 BLEED", 809, 905, 26);

            PB("AIRCOND_ENG1_BLEED", 450, 922, "FAULT", "OFF");
            Box("RAM AIR", 545, 903, 7f);
            Guarded(MakePB("AIRCOND_RAM_AIR", 545, 914, "••", "ON", 44, 44), Color.FromArgb(30, 32, 36), 175);
            PB("AIRCOND_APU_BLEED", 633, 922, "FAULT", "ON");
            L("SHUT", 690, 932, 6.5f); L("AUTO", 722, 921, 6.5f); L("OPEN", 755, 932, 6.5f);
            float[] xb = { -60, 0, 60 };
            _silk.Add(new OhArcDial(722, 950, 24, -60, 60, xb, false));
            Knob("AIRCOND_X_BLEED", 722, 950, 19, 1, xb);
            PB("AIRCOND_ENG2_BLEED", 809, 922, "FAULT", "OFF");

            // Panell inferior central
            _ls = TextScale;
            Panel(314, 998, 626, 256);
            Groove(584, 1004, 584, 1248); Groove(670, 1004, 670, 1248);
            Groove(320, 1079, 934, 1079);
            Groove(674, 1156, 934, 1156);

            Title("ANTI ICE", 455, 1008, 322, 578);
            L("WING", 360, 1021); L("ENG 1", 487, 1021); L("ENG 2", 547, 1021);
            PB("ANTI_ICE_WING", 360, 1030, "FAULT", "ON");
            PB("ANTI_ICE_ENG1", 487, 1030, "FAULT", "ON");
            PB("ANTI_ICE_ENG2", 547, 1030, "FAULT", "ON");
            Groove(404, 1016, 404, 1074);

            L("PROBE/WINDOW", 627, 1008, 7f); L("HEAT", 627, 1018, 7f);
            PB("PROBE_WINDOW_HEAT", 627, 1030, "••", "ON", 42);
            VT("AUTO", 658, 1050, 5.5f);

            L("CABIN PRESS", 770, 1006, 8.5f);
            L("MAN V/S CTL", 700, 1021, 5.5f); L("UP", 725, 1031, 5.5f); L("DN", 725, 1069, 5.5f);
            Tog("PRESS_MAN_VS_CTL", 700, 1050, 3, 1, false, 10, spring: true);
            L("MODE SEL", 760, 1021, 5.5f);
            PB("PRESS_MODE_SEL", 760, 1030, "FAULT", "MAN", 40, 38);
            VT("AUTO", 788, 1049, 5f);
            L("LDG ELEV", 838, 1013, 5.5f);
            float[] le = { -150, -110, -80, -50, -20, 10, 40, 70, 100, 130 };
            // amb lletra mínima de 25 px no hi caben els 10 números: marques a totes les posicions, xifres alternes
            _silk.Add(new OhDial(838, 1050, 16f, le, new[] { "AUTO", "", "0", "", "4", "", "8", "", "12", "" }, 4f));
            Knob("PRESS_LDG_ELEV", 838, 1050, 14, 0, le);
            L("DITCHING", 900, 1021, 5.5f);
            GrayGuard("PRESS_DITCHING", 900, 1030, "••", "ON", 38, 40);

            Title("EXT LT", 455, 1086, 322, 578);
            // EXT LT — estil fidel a la referència: sòcols foscos, pales/palanques/càpsules LAND (escala 1)
            _ls = 1f;
            // EXT LT — proporcions de la referència: sòcols grans i etiquetes enganxades a l'interruptor
            const float es = ExtToggleSize;   // mida dels interruptors (patró de tot l'overhead)
            const float fn = 8f;      // nom de l'interruptor
            const float fp = 7.5f;    // posicions (ON/OFF...)
            L("STROBE", 352, 1095, fn); L("ON", 352, 1104, fp); VT("AUTO", 378, 1128, 6f); L("OFF", 352, 1153, fp);
            TogS("EXT_LT_STROBE", 352, 1128, 3, 2, ToggleStyle.Paddle, es);
            L("BEACON", 415, 1095, fn); L("ON", 415, 1104, fp); L("OFF", 415, 1153, fp);
            TogS("EXT_LT_BEACON", 415, 1128, 2, 1, ToggleStyle.Paddle, es);
            L("WING", 484, 1095, fn); L("ON", 484, 1104, fp); L("OFF", 484, 1153, fp);
            TogS("EXT_LT_WING", 484, 1128, 2, 1, ToggleStyle.Bat, es);
            L("NAV & LOGO", 541, 1095, fn); L("2", 541, 1104, fp); L("1", 565, 1128, fp); L("OFF", 541, 1153, fp);
            TogS("EXT_LT_NAV_LOGO", 541, 1128, 3, 2, ToggleStyle.Paddle, es);
            L("RWY TURN OFF", 357, 1164, fn); L("ON", 357, 1173, fp); L("OFF", 357, 1223, fp);
            TogS("EXT_LT_RWY_TURN_OFF", 357, 1197, 2, 1, ToggleStyle.Bat, es);
            // claudàtor de LAND que agrupa L i R
            WLn(398, 1172, 398, 1164, 434, 1164); WLn(470, 1164, 506, 1164, 506, 1172);
            L("LAND", 452, 1164, fn); L("L", 420, 1173, fn); L("R", 484, 1173, fn);
            L("ON", 452, 1185, fp); L("OFF", 452, 1197, fp); L("RETRACT", 452, 1222, 7f);
            TogS("EXT_LT_LAND_L", 420, 1197, 3, 2, ToggleStyle.Land, es);
            TogS("EXT_LT_LAND_R", 484, 1197, 3, 2, ToggleStyle.Land, es);
            L("NOSE", 541, 1164, fn); L("T.O", 568, 1185, fp); L("TAXI", 570, 1197, fp); L("OFF", 568, 1209, fp);
            TogS("EXT_LT_NOSE", 541, 1197, 3, 2, ToggleStyle.Bat, es);

            _ls = TextScale;
            Title("APU", 627, 1090, 590, 664, 8.5f);
            L("MASTER SW", 627, 1104, 6f);
            PB("APU_MASTER_SW", 627, 1112, "FAULT", "ON");
            L("START", 627, 1170, 6f);
            PB("APU_START", 627, 1178, "AVAIL", "ON");

            Title("INT LT", 845, 1090, 796, 930, 8.5f);
            L("OVHD INTEG LT", 704, 1097, 5f);
            float[] integ = { -140, -100, -60, -20, 20, 60, 100, 140 };
            _silk.Add(new OhArrowArc(704, 1128, 16 * 1.32f, -140, 140));
            Knob("INT_LT_OVHD_INTEG", 704, 1128, 16, 4, integ, KnobStyle.Fluted);
            L("OFF", 683, 1150, 5f); L("BRT", 725, 1150, 5f);
            L("ICE IND &", 763, 1095, 5f); L("STBY COMPASS", 763, 1103, 5f);
            Tog("INT_LT_ICE_IND_STBY_COMPASS", 763, 1130, 2, 1, false, 9);
            L("DOME", 815, 1100, 5.5f); L("BRT", 840, 1116, 5f); L("DIM", 840, 1130, 5f); L("OFF", 840, 1144, 5f);
            Tog("INT_LT_DOME", 815, 1130, 3, 2, false, 9);
            L("ANN LT", 877, 1100, 5.5f); L("TEST", 905, 1116, 5f); L("BRT", 905, 1130, 5f); L("DIM", 905, 1144, 5f);
            var ann = Tog("INT_LT_ANN_LT", 877, 1130, 3, 1, false, 9);
            ann.OnChange = t => { _annMode = ((OhToggle)t).Index; Redraw(new RectangleF(0, 0, LW, CurLH)); };

            Title("SIGNS", 840, 1165, 795, 930, 8.5f);
            L("SEAT BELTS", 695, 1176, 5.5f); L("ON", 695, 1186, 5f); L("OFF", 695, 1240, 5f);
            Tog("SIGNS_SEAT_BELTS", 695, 1212, 2, 1, false, 10);
            L("NO PORTABLE", 757, 1168, 5f); L("ELEC DEVICE", 757, 1177, 5f);
            L("ON", 757, 1186, 4.8f); L("AUTO", 784, 1212, 4.8f); L("OFF", 757, 1240, 5f);
            Tog("SIGNS_NO_PED", 757, 1212, 3, 1, false, 10);
            L("EMER EXIT LT", 852, 1176, 5.5f);
            PB("SIGNS_EMER_EXIT_LT_PB", 812, 1190, "••", "OFF", 38, 36);
            L("ON", 893, 1196, 5f); L("ARM", 895, 1212, 5f); L("OFF", 893, 1228, 5f);
            Tog("SIGNS_EMER_EXIT_LT", 868, 1212, 3, 1, false, 10);
        }

        // ═══════════════════════════════ PANELL PRINCIPAL (pàgina 1) ═══════════════════════════════
        // Coordenades preses de la imatge de referència (2000 px d'ample) i convertides a unitats del panell
        // (×0.625). Totes les pantalles i finestres de dígits queden en negre.
        void BuildMain()
        {
            const float k = 0.625f;
            RectangleF R(float x0, float y0, float x1, float y1) => RectangleF.FromLTRB(x0 * k, y0 * k, x1 * k, y1 * k);
            PointF Pt(float x, float y) => new(x * k, y * k);
            void Lb(string t, float x, float y, float size = 8.15f) => L(t, x * k, y * k, size);
            OhPushButton Pb(string id, float cx, float cy, float w, float h, string up, string lo, bool mom = false) =>
                PB(id, cx * k, cy * k - h * k / 2, up, lo, w * k, h * k, mom);
            OhKey KeyL(string id, float cx, float cy, float w, float h, string label, bool led = true, bool wide = true)
            {
                var kk = Key(id, (cx - w / 2) * k, (cy - h / 2) * k, w * k, h * k, label);
                kk.Led = led; kk.LedWide = wide;
                return kk;
            }

            // ── Glareshield ──
            // vora superior fosca inclinada
            // visera de pell negra: 70% del gruix d'abans (28 → 19,6), mantenint la vora de baix al mateix lloc
            // colze arrodonit (radi 500 de la maqueta)
            _silk.Add(new OhVisor(Oh.RoundAt(new[] { Pt(12, 190 + 8.4f), Pt(1090, 14 + 8.4f), Pt(1985, 14 + 8.4f) }, 1, 500 * k), 19.6f * k));
            // planxa esquerra (inclinada a dalt)
            // vora de dalt: la mateixa línia que l'EFIS (vora de baix de la visera + 10)
            {
                const float slope = 176f / 1078f;
                float yA = 218f - (15f - 12f) * slope + 10f, yB = 218f - (882f - 12f) * slope + 10f;
                _silk.Add(new OhPanel(new[] { Pt(15, yA), Pt(882, yB), Pt(882, 288), Pt(15, 288) },
                                      new[] { Pt(29, 228f - 17f * slope + 12), Pt(868, 228f - 856f * slope + 12), Pt(30, 276), Pt(868, 276) }));
            }
            Pb("MIP_AUTO_LAND", 175, 237, 44, 44, "AUTO", "LAND", true).UpperColor = Oh.White;
            ((OhPushButton)_byId["MIP_AUTO_LAND"]).LowerColor = Oh.White;
            var mw = Pb("MIP_MASTER_WARN", 580, 160, 44, 44, "MASTER", "WARN", true); mw.UpperColor = Oh.Red; mw.LowerColor = Oh.Red;
            var mc = Pb("MIP_MASTER_CAUT", 580, 240, 44, 44, "MASTER", "CAUT", true); mc.UpperColor = Oh.Amber; mc.LowerColor = Oh.Amber;
            Lb("CHRONO", 812, 122);
            Round("MIP_CHRONO", 815 * k, 160 * k, 12f);
            Lb("SIDE STICK PRIORITY", 812, 200);
            var ssp = Pb("MIP_SIDESTICK_PRIORITY", 813, 238, 36, 36, "→", "CAPT"); ssp.LowerColor = Oh.Green; ssp.UpperColor = Oh.Red;

            // panell EFIS: la vora de dalt segueix la rampa de la visera fins al seu punt d'inflexió (x=1090) i
            // després és horitzontal (mateixa separació de 10 que al tram pla)
            {
                const float slope = 176f / 1078f;                    // pendent de la vora de baix de la visera
                float yL = 218f - (890f - 12f) * slope + 10f;          // alçada de la vora a x = 890
                _silk.Add(new OhPanel(Oh.RoundAt(new[] { Pt(890, yL), Pt(1090, 52), Pt(1296, 52), Pt(1296, 288), Pt(890, 288) }, 1, 500 * k),
                                      new[] { Pt(904, 228f - 892f * slope + 12), Pt(1282, 64), Pt(904, 276), Pt(1282, 276) }));
            }
            // finestra del QNH (el rètol QNH/QFE va a dins, en taronja, i el valor el dona el simulador)
            Add(new OhBaroWindow("MIP_BARO_WIN", R(921, 104, 994, 150)));
            // QNH: rotatiu doble (base negra in Hg / hPa + pom blanc PULL STD) amb arc indicador a sobre
            Lb("in Hg", 910, 166, 8.15f); Lb("hPa", 1006, 166, 8.15f);
            _silk.Add(new OhArcDial(958 * k, 202 * k, 24.5f, -38, 38, new float[] { -38, 38 }, false));
            Add(new OhBaroKnob("MIP_BARO", 958 * k, 202 * k, 20.8f));   // 30% més gran
            // ratlla vertical que separa la zona QNH de la zona EFIS (ND)
            // (comença a l'alçada del centre del rotatiu de mode, com a la referència)
            _silk.Add(new OhLine(Oh.Silk, 1.1f, new[] { Pt(1019, 174), Pt(1019, 284) }));
            var fd = KeyL("MIP_EFIS_FD", 937, 262, 42, 28, "FD"); fd.On = true;
            KeyL("MIP_EFIS_LS", 987, 262, 42, 28, "LS");
            string[] ndKeys = { "CSTR", "WPT", "VOR.D", "NDB", "ARPT" };
            // 5 tecles repartides com si n'hi hagués 6 (ARPT al seu lloc, CSTR més a l'esquerra)
            float[] ndX = { 1031, 1087.25f, 1143.5f, 1199.75f, 1256 };
            for (int i = 0; i < 5; i++) KeyL("MIP_EFIS_" + ndKeys[i].Replace(".", ""), ndX[i], 95, 42, 30, ndKeys[i]);
            // selector de mode del ND
            // LS a 270º, NAV a 360º (dalt), PLAN a 90º; posicions repartides cada 45º
            float[] modeAng = { -90, -45, 0, 45, 90 };
            _silk.Add(new OhArcDial(1095 * k, 178 * k, 21f, -90, 90, modeAng, false));   // arc ajustat al rotatiu
            // quart de cercle ROSE: de LS a NAV per fora de l'escala, interromput per la paraula ROSE
            _silk.Add(new OhArcDial(1095 * k, 178 * k, 64f * k, -86, -74, Array.Empty<float>(), false));   // tram LS → ROSE
            _silk.Add(new OhArcDial(1095 * k, 178 * k, 64f * k, -56, -16, Array.Empty<float>(), false));   // tram ROSE → NAV
            var ndMode = Knob("MIP_ND_MODE", 1095 * k, 178 * k, 17.1f, 2, modeAng);   // 10% més petit
            ndMode.ZoneHalfWidth = 2f; ndMode.ZoneUp = 2f; ndMode.ZoneDown = 1.5f;     // zones grans: esquerra −, dreta +
            // etiquetes acostades a l'arc (menys del que s'ha reduït l'arc, perquè quedin més espaiades)
            Lb("ROSE", 1037, 151); Lb("LS", 1045, 178); Lb("VOR", 1062.9f, 139.7f); Lb("NAV", 1095, 129); Lb("ARC", 1127.1f, 139.7f); Lb("PLAN", 1155, 178);
            // selector d'abast del ND
            // 10 a 270º, 40 a 360º, 160 a 90º; posicions repartides cada 45º
            float[] rngAng = { -90, -45, 0, 45, 90, 135 };
            _silk.Add(new OhArcDial(1236 * k, 178 * k, 21f, -90, 135, rngAng, false));   // arc ajustat al rotatiu
            var ndRange = Knob("MIP_ND_RANGE", 1236 * k, 178 * k, 17.1f, 2, rngAng);   // 10% més petit
            ndRange.ZoneHalfWidth = 2f; ndRange.ZoneUp = 2f; ndRange.ZoneDown = 1.5f;
            Lb("10", 1184, 178); Lb("20", 1202.8f, 144.8f); Lb("40", 1236, 129); Lb("80", 1269.2f, 144.8f); Lb("160", 1286, 178); Lb("320", 1270.6f, 212.6f);
            // ADF / OFF / VOR: palanques metàl·liques horitzontals (OFF al centre)
            // zones de toc: mateixa amplada que les del QNH (4 x 20,8) i un 25% més altes que la de la palanca
            var adf1 = TogS("MIP_EFIS_ADFVOR1", 1096 * k, 250 * k, 3, 1, ToggleStyle.Metal, 9.5f).MakeHorizontal();
            adf1.ZoneWidth = 4f * 20.8f; adf1.ZoneHeight = (adf1.Bounds.Height + 6f) * 1.25f;
            Lb("1", 1097, 222); Lb("ADF", 1055, 252); Lb("VOR", 1137, 252); Lb("OFF", 1097, 280);
            var adf2 = TogS("MIP_EFIS_ADFVOR2", 1236 * k, 250 * k, 3, 1, ToggleStyle.Metal, 9.5f).MakeHorizontal();
            adf2.ZoneWidth = 4f * 20.8f; adf2.ZoneHeight = (adf2.Bounds.Height + 6f) * 1.25f;
            Lb("2", 1237, 222); Lb("ADF", 1195, 252); Lb("VOR", 1277, 252); Lb("OFF", 1237, 280);

            // FCU
            // cargols a les cantonades, mateix patró que el de baix a la dreta del mòdul de WARN/CAUT (14 del costat, 12 de dalt/baix)
            _silk.Add(new OhPanel(new[] { Pt(1302, 50), Pt(1980, 50), Pt(1980, 288), Pt(1302, 288) },
                                  new[] { Pt(1316, 62), Pt(1966, 62), Pt(1316, 276), Pt(1966, 276) }));
            Add(new OhFcuDisplay("MIP_FCU_DISP", R(1345, 60, 1950, 115)));   // pantalla del FCU (dades del simulador)
            // SPD / HDG: rotatius simples (sense anell de selecció), 4 zones de toc: PUSH, PULL, gir esquerra / dreta.
            // Les zones dels dos rotatius gairebé es toquen (mig ample 1,8r); SPD/MACH queda just a l'esquerra de les del SPD.
            const float fcuZw = 1.8f;
            const float spdMachX = (1388f * 0.625f - fcuZw * 20f - 8f) / 0.625f;
            Lb("SPD", spdMachX, 125); Lb("MACH", spdMachX, 170);
            Round("MIP_FCU_SPD_MACH", spdMachX * k, 146 * k, 8f).HitPad = 0f;
            Add(new OhBaroKnob("MIP_FCU_SPD", 1388 * k, 176 * k, 20f, zoneHalfWidth: fcuZw, face: KnobFace.Spd));
            Add(new OhBaroKnob("MIP_FCU_HDG", 1505 * k, 176 * k, 20f, zoneHalfWidth: fcuZw, face: KnobFace.Hdg) { SingleZone = true });
            KeyL("MIP_FCU_LOC", 1505, 258, 46, 30, "LOC");
            WLn(1558 * k, 128 * k, 1558 * k, 272 * k);
            Lb("HDG", 1586, 133); Lb("TRK", 1586, 147); Lb("V/S", 1668, 133); Lb("FPA", 1668, 147);
            Round("MIP_FCU_HDGTRK_VSFPA", 1628 * k, 146 * k, 8f);
            KeyL("MIP_FCU_AP1", 1597, 192, 44, 44, "AP1");
            KeyL("MIP_FCU_AP2", 1657, 192, 44, 44, "AP2");
            KeyL("MIP_FCU_ATHR", 1628, 250, 44, 44, "A/THR");
            WLn(1700 * k, 128 * k, 1700 * k, 272 * k);
            // ALT: rotatiu doble com el del QNH (anell negre 100 / 1000 + pom estriat de 8 ones, PUSH / PULL),
            // amb un anell circular al voltant i les marques de 100 i 1000
            _silk.Add(new OhArcDial(1750 * k, 176 * k, 24.5f, -180, 180, new float[] { -38, 38 }, false));
            Lb("100", 1716, 131); Lb("1000", 1785, 131);
            Add(new OhBaroKnob("MIP_FCU_ALT", 1750 * k, 176 * k, 20.8f, alt: true));   // mateixes zones de toc que el QNH
            // METRIC ALT entre l'ALT i el V/S (fora de les zones de toc de l'ALT)
            // zona de toc just el voltant negre, tocant la zona de la dreta de l'ALT (cx + 2r)
            const float metricX = (1750f * 0.625f + 2f * 20.8f + 8f) / 0.625f;
            Lb("METRIC", metricX, 168); Lb("ALT", metricX, 182);
            Round("MIP_FCU_METRIC_ALT", metricX * k, 145 * k, 8f).HitPad = 0f;
            KeyL("MIP_FCU_EXPED", 1755, 255, 48, 30, "EXPED");
            // V/S: idèntic al SPD, amb dues fletxes en semicercle a l'esquerra (UP amunt, DN avall)
            _silk.Add(new OhArrowArc(1900 * k, 172 * k, 19f * 1.4f, -95, -30));
            _silk.Add(new OhArrowArc(1900 * k, 172 * k, 19f * 1.4f, -85, -150));
            Lb("UP", 1893, 128); Lb("DN", 1893, 216);   // just a continuació de la punta de cada fletxa
            Add(new OhBaroKnob("MIP_FCU_VS", 1900 * k, 172 * k, 19f, zoneHalfWidth: fcuZw, face: KnobFace.Spd));
            Lb("PUSH", 1958, 150); Lb("TO", 1958, 164); Lb("LEVEL", 1958, 178); Lb("OFF", 1958, 192);
            KeyL("MIP_FCU_APPR", 1908, 255, 48, 30, "APPR");

            // ombra de la visera sobre el mòdul esquerre, l'EFIS i el FCU (per sobre dels botons)
            Add(new OhVisorShadow(Oh.RoundAt(new[] { Pt(7, 218.8f), Pt(12, 218), Pt(1090, 42), Pt(1990, 42) }, 2, 500 * k), k, "MIP_VISOR_SHADOW", 0.75f));

            // llavi inferior del glareshield amb les sortides d'aire
            _silk.Add(new OhPoly(new[] { Pt(12, 294), Pt(1985, 294), Pt(1985, 332), Pt(12, 332) },
                                 Color.FromArgb(38, 44, 51), Color.FromArgb(12, 14, 17), true));   // franja fosca entre glareshield i panell
            // ombra de la franja sobre el PFD, ND, ECAM i LDG GEAR (mateixos paràmetres que la visera)
            Add(new OhVisorShadow(new[] { Pt(7, 332), Pt(1990, 332) }, k, "MIP_LIP_SHADOW"));
            // cantell de la carcassa dels mòduls: menja la part de dalt de la franja fosca (1/4 de la seva alçada)
            _silk.Add(new OhCasingEdge(R(12, 288, 1985, 294 + (332 - 294) * 0.25f)));
            foreach (float vx in new float[] { 842, 1408, 1876 }) _silk.Add(new OhVent(vx * k, 314 * k, 28, 10));

            // ── Panell principal ──
            // planxa de l'esquerra (PFD/ND i zona dels genolls)
            _silk.Add(new OhPanel(new[] { Pt(15, 338), Pt(872, 338), Pt(872, 1046), Pt(15, 1046) },
                                  new[] { Pt(28, 352), Pt(858, 352), Pt(28, 1032), Pt(858, 1032) }));
            // planxa central i dreta
            _silk.Add(new OhPanel(new[] { Pt(880, 338), Pt(1982, 338), Pt(1982, 1178), Pt(880, 1178) },
                                  new[] { Pt(894, 352), Pt(1968, 352), Pt(894, 1164), Pt(1968, 1164) }));

            // pantalles (en negre)
            // pantalles: el vidre de cadascuna és un forat a la finestra de l'app (s'hi veuen les pantalles d'X-Plane de sota)
            // vidres (forats) de 725 x 705 px a 4K (1 unitat de referència = 1,92 px), centrats on eren
            RectangleF Glass(float cx, float cy) => R(cx - 725f / 1.92f / 2f, cy - 705f / 1.92f / 2f, cx + 725f / 1.92f / 2f, cy + 705f / 1.92f / 2f);
            var gPfd = Glass(226, 572); var gNd = Glass(661, 572); var gEwd = Glass(1470, 545); var gSd = Glass(1471, 943);
            _silk.Add(new OhBezel(R(24, 372, 428, 772), gPfd));      // PFD
            _silk.Add(new OhBezel(R(454, 372, 868, 772), gNd));      // ND
            _screenGlass.AddRange(new[] { gPfd, gNd, gEwd, gSd });
            _silk.Add(new OhBlock(R(520, 776, 812, 796), Oh.T(120, 132, 144, 116, 142, 164), Oh.T(70, 80, 90, 66, 88, 106), 3f));
            _silk.Add(new OhBezel(R(1254, 344, 1686, 736), gEwd));  // ECAM superior
            _silk.Add(new OhBezel(R(1254, 744, 1686, 1136), gSd));   // ECAM inferior

            // zona dels genolls (fosca)
            _silk.Add(new OhBlock(R(15, 846, 660, 914), Color.FromArgb(70, 72, 76), Color.FromArgb(14, 14, 16), 12f));
            _silk.Add(new OhBlock(R(22, 926, 482, 1044), Color.FromArgb(58, 60, 64), Color.FromArgb(26, 26, 28), 2f));
            _silk.Add(new OhBlock(R(550, 926, 788, 1036), Color.FromArgb(70, 72, 76), Color.FromArgb(30, 30, 32), 2f, 7));
            Oh_BigScrews(new[] { Pt(676, 818), Pt(860, 948) });
            // quadrant de gas (mòdul opcional, botó TH): a sota del rectangle que hi ha sota l'ND, centrat amb ell
            // (més gran: la carcassa comença just sota la llengüeta i tapa el cargol; l'espai de dalt de l'element
            //  només el fa servir el pom quan la palanca és a TO GA)
            _throttle = Add(new OhThrottle("MIP_THROTTLE", R(556, 742, 776, 1146)) { Shown = _throttleShown });

            // TERR ON ND (capità)
            // DCDU, tapa cega, TERR ON ND i ISIS pujats en bloc: la vora de baix del DCDU alineada amb la de l'ECAM inferior
            const float Up = 40f;
            Panel(890 * k, (655 - Up) * k, (1016 - 890) * k, (792 - 655) * k);
            Lb("TERR ON ND", 945, 667 - Up);
            Pb("MIP_TERR_ON_ND_1", 942, 698 - Up, 40, 40, "••", "ON");

            // ISIS (pantalla negra amb botons)
            _silk.Add(new OhScreen(R(1030, 520 - Up, 1222, 714 - Up), R(1058, 548 - Up, 1196, 688 - Up), 12f, false));
            Key("MIP_ISIS_BUGS", 1078 * k, (526 - Up) * k, 34 * k, 16 * k, "BUGS");
            Key("MIP_ISIS_LS", 1144 * k, (526 - Up) * k, 30 * k, 16 * k, "LS");
            Key("MIP_ISIS_PLUS", 1037 * k, (578 - Up) * k, 12 * k, 24 * k, "+");
            Key("MIP_ISIS_MINUS", 1037 * k, (640 - Up) * k, 12 * k, 24 * k, "-");
            Key("MIP_ISIS_ATT", 1052 * k, (694 - Up) * k, 30 * k, 15 * k, "ATT");
            Key("MIP_ISIS_RST", 1088 * k, (694 - Up) * k, 30 * k, 15 * k, "RST");
            Knob("MIP_ISIS_BARO", 1193 * k, (686 - Up) * k, 10f, 0, Enumerable.Range(0, 12).Select(i => i * 30f).ToArray(), KnobStyle.Round).Wrap = true;
            // panell buit sota l'ISIS
            _silk.Add(new OhPanel(new[] { Pt(1036, 730 - Up), Pt(1228, 730 - Up), Pt(1228, 970 - Up), Pt(1036, 970 - Up) },
                                  new[] { Pt(1048, 744 - Up), Pt(1216, 744 - Up), Pt(1048, 956 - Up), Pt(1216, 956 - Up) }));

            // DCDU (pantalla negra amb tecles laterals)
            _silk.Add(new OhScreen(R(880, 996 - Up, 1206, 1176 - Up), R(930, 1016 - Up, 1156, 1152 - Up), 6f, true));
            string[] dl = { "BRT", "DIM", "MSG-", "MSG+", "", "" };
            string[] dr = { "PRINT", "PGE-", "PGE+", "", "" };
            for (int i = 0; i < dl.Length; i++) Key("MIP_DCDU_L" + i, 890 * k, (1016 - Up + i * 22) * k, 28 * k, 15 * k, dl[i]).Momentary = true;
            for (int i = 0; i < dr.Length; i++) Key("MIP_DCDU_R" + i, 1166 * k, (1032 - Up + i * 22) * k, 30 * k, 15 * k, dr[i]).Momentary = true;

            // LDG GEAR / BRK FAN / AUTO BRK / A-SKID
            Panel(1720 * k, 340 * k, (1976 - 1720) * k, (522 - 340) * k);
            Lb("LDG GEAR", 1806, 352);
            for (int i = 0; i < 3; i++)
            {
                var gi = Add(new OhGearInd("MIP_GEAR_IND_" + i, R(1740 + i * 45, 362, 1780 + i * 45, 402)));
                gi.Down = 1f;   // tren baixat i blocat: triangle verd encès
            }
            WLn(1896 * k, 346 * k, 1896 * k, 414 * k);
            Lb("BRK FAN", 1930, 352);
            var bf = Pb("MIP_BRK_FAN", 1930, 383, 40, 40, "HOT", "ON"); bf.UpperColor = Oh.Amber;
            WLn(1730 * k, 416 * k, 1966 * k, 416 * k);
            Lb("AUTO/BRK", 1806, 428);
            Lb("LO", 1756, 443); Lb("MED", 1801, 443); Lb("MAX", 1858, 443);
            foreach (var (id, x) in new[] { ("LO", 1756f), ("MED", 1801f), ("MAX", 1858f) })
            {
                var ab = Pb("MIP_AUTOBRK_" + id, x, 474, 40, 40, "DECEL", "ON"); ab.UpperColor = Oh.Green;
            }
            Lb("A/SKID &", 1932, 426); Lb("N/W STRG", 1932, 440);
            TogS("MIP_ASKID_NWSTRG", 1922 * k, 476 * k, 2, 0, ToggleStyle.Bat, 10f);
            Lb("ON", 1958, 458); Lb("OFF", 1958, 494);

            // TERR ON ND (primer oficial)
            Panel(1885 * k, 526 * k, (1976 - 1885) * k, (614 - 526) * k);
            Lb("TERR ON ND", 1930, 536);
            Pb("MIP_TERR_ON_ND_2", 1930, 572, 40, 40, "••", "ON");

            // rellotge (pantalles en negre)
            Panel(1686 * k, 530 * k, (1876 - 1686) * k, (716 - 530) * k);
            _silk.Add(new OhDarkWindow(R(1732, 552, 1830, 694)));
            Lb("RST", 1714, 546); Lb("CHR", 1846, 546);
            Round("MIP_CLK_RST", 1714 * k, 580 * k, 6f);
            Round("MIP_CLK_CHR", 1846 * k, 580 * k, 6f);
            Lb("DATE", 1712, 636);
            Knob("MIP_CLK_DATE", 1712 * k, 614 * k, 6f, 0, new float[] { -40, 0, 40 }, KnobStyle.Round);
            Lb("GPS", 1846, 600);
            TogS("MIP_CLK_GPS", 1846 * k, 624 * k, 2, 0, ToggleStyle.Bat, 5.5f);
            Lb("RUN", 1846, 652);
            TogS("MIP_CLK_RUN", 1846 * k, 678 * k, 3, 0, ToggleStyle.Bat, 5.5f);

            // palanca del tren
            Panel(1720 * k, 740 * k, (1836 - 1720) * k, (958 - 740) * k);
            Lb("UP", 1803, 766); Lb("DOWN", 1803, 946);
            _silk.Add(new OhArrow(1803 * k, 790 * k, 270, 5f, Oh.Silk));
            WLn(1803 * k, 794 * k, 1803 * k, 830 * k);
            WLn(1803 * k, 848 * k, 1803 * k, 926 * k);
            _silk.Add(new OhArrow(1803 * k, 930 * k, 90, 5f, Oh.Silk));
            Add(new OhGearLever("MIP_GEAR_LEVER", 1762 * k, 788 * k, 932 * k));

            // indicador de pressió de frens (agulles mòbils)
            Panel(1856 * k, 812 * k, (1984 - 1856) * k, (944 - 812) * k);
            Add(new OhBrakeGauge("MIP_BRAKE_GAUGE", 1920 * k, 878 * k, 36f));

            // tots els botons i tecles del panell principal amb l'estil validat (marc fosc, cara enfonsada)
            foreach (var el in _els)
            {
                if (el is OhPushButton pbm) pbm.Mip = true;
                else if (el is OhKey km) km.Mip = true;
            }
        }

        /// <summary>Cargols grans de la planxa principal (cap de tornavís, com els Dzus però més grossos).</summary>
        void Oh_BigScrews(PointF[] pts)
        {
            foreach (var p in pts) _silk.Add(new OhScrewSilk(p.X, p.Y, 7.5f));
        }

        void FirePanel(string p, float titleX, string title, float ag1X, float ag2X, float fireX, float testX)
        {
            L(title, titleX, 176, 20f, false);
            L("AGENT 1", ag1X, 175, 6f); L("AGENT 2", ag2X, 175, 6f);
            var ag1 = PB(p + "_AGENT1", ag1X, 185, "SQUIB", "DISCH", 42, 36, true);
            var ag2 = PB(p + "_AGENT2", ag2X, 185, "SQUIB", "DISCH", 42, 36, true);
            var fire = new OhFireButton(p + "_FIRE", new RectangleF(fireX, 203, 86, 54));
            Guarded(fire, Oh.GuardRed, 60);
            L("TEST", testX, 238, 6f);
            Round(p + "_FIRE_TEST", testX, 258, 10).OnChange = _ => FlashTest(2.5f, p + "_FIRE", p + "_AGENT1", p + "_AGENT2");
            // FIRE premut → s'encenen els SQUIB; disparar un agent amb SQUIB encès → DISCH
            fire.OnChange = _ =>
            {
                SetLegend(p + "_AGENT1", fire.In ? 1f : 0f, null);
                SetLegend(p + "_AGENT2", fire.In ? 1f : 0f, null);
            };
            ag1.OnChange = _ => { if (ag1.UpperLevel > 0.5f) SetLegend(ag1.Id, null, 1f); };
            ag2.OnChange = _ => { if (ag2.UpperLevel > 0.5f) SetLegend(ag2.Id, null, 1f); };
        }

        // ───────────────────────────── Columna dreta ─────────────────────────────
        void BuildRight()
        {
            // ACP
            Panel(946, 14, 280, 160);
            float[] xs = { 980, 1014, 1048, 1083, 1118, 1153, 1188 };
            string[] tx = { "VHF1", "VHF2", "VHF3", "HF1", "HF2", "INT", "CAB" };
            for (int i = 0; i < 7; i++)
            {
                if (i < 5) { var k = Key("ACP_CALL_" + tx[i], xs[i] - 14, 22, 28, 13, "CALL"); k.LitText = true; }
                else if (i == 5) { var k = Key("ACP_CALL_MECH", xs[i] - 14, 22, 28, 13, "MECH"); k.LitText = true; }
                else L("ATT", xs[i], 28, 6f);
                L(tx[i], xs[i], 44, 5.5f);
                Vol("ACP_VOL_" + tx[i], xs[i], 66, 12);
            }
            VT("VOICE", 957, 104, 5f);
            L("INT", 980, 86, 5f); L("RAD", 980, 121, 5f);
            // l'ACP no té espai per a la mida d'EXT LT: és l'únic interruptor més petit
            TogS("ACP_INT_RAD", 980, 104, 3, 1, ToggleStyle.Bat, 11f);
            VT("LS", 1012, 104, 5f);
            Vol("ACP_VOL_LS", 1033, 104, 12);
            VT("MKR", 1088, 104, 5f);
            Vol("ACP_VOL_MKR", 1068, 104, 12);
            Key("ACP_CALL_X1", 1102, 97, 28, 13, "CALL").LitText = true;
            Key("ACP_CALL_X2", 1136, 97, 28, 13, "CALL").LitText = true;
            Key("ACP_RESET", 1172, 97, 26, 13, "").Momentary = true;
            VT("RESET", 1214, 104, 5f);
            L("1 - VOR - 2", 997, 133, 5.5f); L("1 - ADF - 2", 1065, 133, 5.5f); L("1 - SAT - 2", 1135, 133, 5.5f); L("PA", 1188, 133, 5.5f);
            string[] nav = { "VOR1", "VOR2", "ADF1", "ADF2", "SAT1", "SAT2", "PA" };
            for (int i = 0; i < 7; i++) Vol("ACP_VOL_" + nav[i], xs[i], 154, 11);

            // RMP
            Panel(946, 180, 280, 170);
            L("ACTIVE", 1015, 192, 8f); L("STBY/CRS", 1152, 192, 8f);
            Seg("RMP_ACTIVE", 972, 201, 88, 30, "123.750", 6);
            var xfer = Key("RMP_XFER", 1066, 205, 38, 22, "");
            xfer.Arrows = true; xfer.Momentary = true;
            Seg("RMP_STBY", 1110, 201, 88, 30, "132.500", 6);
            xfer.OnChange = _ =>
            {
                var a = (OhSeg7)_byId["RMP_ACTIVE"]; var s = (OhSeg7)_byId["RMP_STBY"];
                string t = a.Text; SetDisplay("RMP_ACTIVE", s.Text); SetDisplay("RMP_STBY", t);
            };
            void RKey(string id, float x, float y, string label, string? group, bool on = false, float w = 30)
            {
                var k = Key(id, x, y, w, 17, label); k.Led = true; k.Group = group; k.On = on;
            }
            RKey("RMP_VHF1", 954, 240, "VHF1", "COM", true);
            RKey("RMP_VHF2", 990, 240, "VHF2", "COM");
            RKey("RMP_VHF3", 1026, 240, "VHF3", "COM");
            RKey("RMP_HF1", 966, 266, "HF1", "COM");
            Round("RMP_SEL", 1012, 278, 7);
            L("SEL", 1012, 265.5f, 5f);
            RKey("RMP_HF2", 1030, 266, "HF2", "COM");
            RKey("RMP_AM", 1066, 266, "AM", null);
            float[] dk = new float[12];
            for (int i = 0; i < 12; i++) dk[i] = i * 30f;
            var big = Knob("RMP_TUNING_KNOB", 1150, 264, 22, 0, dk, KnobStyle.Round);
            big.Wrap = true;
            Title("STBY NAV", 1048, 296, 952, 1150, 6.5f);
            RKey("RMP_NAV", 964, 308, "NAV", null);
            RKey("RMP_VOR", 999, 308, "VOR", "NAV");
            RKey("RMP_ILS", 1034, 308, "ILS", "NAV");
            RKey("RMP_MLS", 1069, 308, "MLS", "NAV");
            RKey("RMP_ADF", 1104, 308, "ADF", "NAV");
            RKey("RMP_BFO", 1139, 308, "BFO", null);
            L("ON", 1194, 292, 5.5f); L("OFF", 1194, 344, 5.5f);
            Tog("RMP_ON_OFF", 1194, 319, 2, 0, false, 8);

            // FLT CTL 2
            Panel(946, 357, 280, 86);
            L("FLT CTL", 1086, 367, 10f);
            L("ELAC 2", 1000, 380); L("SEC 2", 1057, 380); L("SEC 3", 1113, 380); L("FAC 2", 1169, 380);
            PB("FLTCTL_ELAC2", 1000, 389, "FAULT", "OFF");
            PB("FLTCTL_SEC2", 1057, 389, "FAULT", "OFF");
            PB("FLTCTL_SEC3", 1113, 389, "FAULT", "OFF");
            PB("FLTCTL_FAC2", 1169, 389, "FAULT", "OFF");

            Panel(946, 450, 280, 94);

            // Panell inferior dret
            _silk.Add(new OhPanel(new PointF[] { new(946, 550), new(1226, 550), new(1226, 1098), new(946, 1248) },
                                  new PointF[] { new(955, 559), new(1217, 559), new(1217, 1088), new(958, 1231) }));
            L("CARGO VENT", 1086, 566, 10f);
            WLn(1066, 578, 1066, 724);
            L("AFT ISOL VALVE", 1008, 581, 5f);
            PB("CARGO_AFT_ISOL_VALVE_1", 1008, 589, "FAULT", "OFF");
            Box("AFT", 1022, 650, 6f);
            TempKnob("CARGO_TEMP_1", 1022, 690);
            L("COLD", 994, 719, 5.5f); L("HOT", 1052, 719, 5.5f);
            L("HOT AIR", 1112, 581, 5.5f);
            PB("CARGO_HOT_AIR", 1112, 590, "FAULT", "OFF");
            L("AFT ISOL VALVE", 1180, 581, 5f);
            PB("CARGO_AFT_ISOL_VALVE_2", 1180, 590, "FAULT", "OFF");
            Box("AFT", 1148, 650, 6f);
            TempKnob("CARGO_TEMP_2", 1148, 690);
            L("COLD", 1120, 719, 5.5f); L("HOT", 1178, 719, 5.5f);

            Title("CARGO SMOKE", 1086, 738, 952, 1220);
            L("FWD", 990, 749); L("AFT", 1180, 749); L("TEST", 1086, 756, 5f);
            PB("CARGO_SMOKE_FWD", 990, 757, "SMOKE", "••");
            PB("CARGO_SMOKE_AFT", 1180, 757, "SMOKE", "••");
            Round("CARGO_SMOKE_TEST", 1086, 774, 11).OnChange = _ => FlashTest(2.5f, "CARGO_SMOKE_FWD", "CARGO_SMOKE_AFT", "CARGO_DISCH_1", "CARGO_DISCH_2");
            PB("CARGO_DISCH_1", 1086, 800, "DISCH", "AGENT2");
            PB("CARGO_DISCH_2", 1086, 852, "DISCH", "DISCH2");
            L("AGENT 1", 1040, 862, 5.5f); L("AGENT 2", 1040, 884, 5.5f);
            L("AGENT 1", 1132, 862, 5.5f); L("AGENT 2", 1132, 884, 5.5f);
            RedGuard("CARGO_AGENT_FWD", 992, 856, "••", "••", 36, 38, true);
            RedGuard("CARGO_AGENT_AFT", 1180, 856, "••", "••", 36, 38, true);

            Title("VENTILATION", 1088, 916, 952, 1220);
            L("BLOWER", 990, 928); L("EXTRACT", 1052, 928); L("CAB FANS", 1176, 928);
            PB("VENT_BLOWER", 990, 937, "FAULT", "OVRD");
            VT("AUTO", 1020, 957, 5f);
            PB("VENT_EXTRACT", 1052, 937, "FAULT", "OVRD");
            VT("AUTO", 1082, 957, 5f);
            PB("VENT_CAB_FANS", 1176, 937, "••", "OFF");

            Title("ENG", 1086, 992, 952, 1220);
            L("MAN START", 1019, 1004, 6f);
            WLn(972, 1004, 990, 1004); WLn(1048, 1004, 1066, 1004);
            L("1", 990, 1013, 6f); L("2", 1048, 1013, 6f);
            GrayGuard("ENG_MAN_START1", 990, 1021, "••", "ON", 42, 40);
            GrayGuard("ENG_MAN_START2", 1048, 1021, "••", "ON", 42, 40);

            L("WIPER", 1028, 1082, 7f); L("OFF", 1028, 1093, 6f);
            float[] wip = { -15, 45, 105 };
            _silk.Add(new OhDial(1028, 1134, 24.5f, wip, null, 5f));
            Knob("WIPER_2", 1028, 1134, 22, 0, wip);
            L("SLOW", 1070, 1113, 6f); L("FAST", 1068, 1163, 6f);
            L("RAIN RPLNT", 1124, 1084);
            Round("RAIN_RPLNT_2", 1124, 1111, 13);
        }
    }
}
