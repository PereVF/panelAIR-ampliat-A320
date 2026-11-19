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
        public static readonly Color Frame   = Color.FromArgb(42, 58, 74);
        public static readonly Color PlateA  = Color.FromArgb(104, 134, 158);
        public static readonly Color PlateB  = Color.FromArgb(86, 115, 139);
        public static readonly Color PlateHi = Color.FromArgb(150, 176, 196);
        public static readonly Color PlateLo = Color.FromArgb(32, 46, 60);

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
            _grainFine ??= new TextureBrush(MakeNoise(128, 7, 20), WrapMode.Tile);
            _grainCoarse ??= new TextureBrush(MakeNoise(64, 11, 16), WrapMode.Tile);
            _grainCoarse.ResetTransform(); _grainCoarse.ScaleTransform(7f / PixelScale, 7f / PixelScale);
            _grainFine.ResetTransform(); _grainFine.ScaleTransform(1f / PixelScale, 1f / PixelScale);
            g.FillPath(_grainCoarse, area);
            g.FillPath(_grainFine, area);
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
            float aStop = _a1 - head / _r * 180f / MathF.PI;      // l'arc acaba on comença la punta
            var rect = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            using (var p = new Pen(Oh.Silk, 2.2f)) g.DrawArc(p, rect, _a0 - 90, aStop - _a0);
            double ae = (_a1 - 90) * Math.PI / 180.0;
            float tx = -(float)Math.Sin(ae), ty = (float)Math.Cos(ae);   // tangent en sentit horari
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
            if (Momentary) { _hold = 0.16f; Changed(); return; }
            In = !In;
            if (LowerFollowsState) LowerLevel = (In == LowerLitWhenIn) ? 1f : 0f;
            Changed();
        }

        public override void Draw(Graphics g)
        {
            var r = Bounds;
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
            Index = ni; Changed();
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

    internal enum ToggleStyle { Bat, Paddle, Land }

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
            int ni = _n == 2 ? 1 - Index : Math.Clamp(Index + (p.Y < _cy ? -1 : 1), 0, _n - 1);
            if (ni == Index) return;
            Index = ni;
            if (SpringToCenter && _n == 3 && ni != 1) _spring = 0.45f;
            Changed();
        }

        public override void Draw(Graphics g)
        {
            float R = _sz * 1.02f;
            DrawSocket(g, R);
            float ext = 1f - Math.Abs(_pos);             // 0 = palanca estirada, 1 = apuntant a l'observador
            float dir = _pos >= 0 ? 1f : -1f;
            switch (Style)
            {
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
        public override void Tap(PointF p) { _hold = 0.18f; Changed(); }
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
            ['6'] = "afgedc", ['7'] = "abc", ['8'] = "abcdefg", ['9'] = "abcdfg", ['-'] = "g", [' '] = ""
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

        static void Digit(Graphics g, float x, float y, float w, float h, char c, bool dp)
        {
            float pad = w * 0.12f;
            float sx = x + pad, swd = w - pad * 2 - w * 0.1f;
            float t = Math.Max(0.9f, swd * 0.19f);
            float hh = h / 2f;
            Map.TryGetValue(c, out var segs);
            segs ??= "";
            var rects = new (char n, RectangleF r)[]
            {
                ('a', new RectangleF(sx + t * 0.6f, y, swd - t * 1.2f, t)),
                ('g', new RectangleF(sx + t * 0.6f, y + hh - t / 2, swd - t * 1.2f, t)),
                ('d', new RectangleF(sx + t * 0.6f, y + h - t, swd - t * 1.2f, t)),
                ('f', new RectangleF(sx, y + t * 0.6f, t, hh - t * 0.9f)),
                ('b', new RectangleF(sx + swd - t, y + t * 0.6f, t, hh - t * 0.9f)),
                ('e', new RectangleF(sx, y + hh + t * 0.3f, t, hh - t * 0.9f)),
                ('c', new RectangleF(sx + swd - t, y + hh + t * 0.3f, t, hh - t * 0.9f)),
            };
            using var on = new SolidBrush(Oh.SegAmber);
            using var off = new SolidBrush(Color.FromArgb(26, Oh.SegAmber));
            foreach (var (n, rr) in rects) g.FillRectangle(segs.IndexOf(n) >= 0 ? on : off, rr);
            g.FillRectangle(dp ? on : off, sx + swd + w * 0.03f, y + h - t, t, t);
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
            if (Momentary) { _hold = 0.16f; Changed(); return; }
            if (Group != null) { if (On) return; On = true; Owner.ClearGroup(Group, this); }
            else On = !On;
            Changed();
        }
        public override void Draw(Graphics g)
        {
            var r = Bounds;
            bool pressed = _hold > 0;
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
                using (var ob = new LinearGradientBrush(_outer, Color.FromArgb(128, 156, 178), Color.FromArgb(68, 92, 112), 60f)) g.FillPath(ob, op);
                using var pe = new Pen(Color.FromArgb(40, 48, 56), 0.8f); g.DrawPath(pe, op);
            }
            // llavi interior fosc al voltant del vidre
            var lip = RectangleF.Inflate(_glass, 2.2f, 2.2f);
            using (var lp = Oh.RoundRect(lip, 2.5f))
            using (var lb = new LinearGradientBrush(lip, Color.FromArgb(22, 30, 38), Color.FromArgb(90, 114, 134), 90f)) g.FillPath(lb, lp);
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

    /// <summary>Indicador de tren (LDG GEAR): finestra negra amb "UNLK" vermell a dalt i triangle verd a baix.</summary>
    internal sealed class OhGearInd : OhElement
    {
        public float Unlk, Down;
        public OhGearInd(string id, RectangleF r) { Id = id; Bounds = r; }
        public override void Draw(Graphics g)
        {
            var r = Bounds;
            using (var sp = Oh.RoundRect(r, 2.5f)) { Oh.SoftShadow(g, sp, 1f, 1.6f, 2f, 120); using var bb = new LinearGradientBrush(r, Color.FromArgb(96, 102, 108), Color.FromArgb(28, 31, 34), 90f); g.FillPath(bb, sp); }
            var f = RectangleF.Inflate(r, -3f, -3f);
            using (var fb = new SolidBrush(Color.FromArgb(14, 15, 16))) g.FillRectangle(fb, f);
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
        public const float MainLH = 745f;

        // ── Pàgines: 0 = overhead, 1 = panell principal (glareshield + instruments) ──
        sealed class PageData
        {
            public readonly List<OhSilk> Silk = new();
            public List<OhElement> Els = new();
            public float LH;
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

        public A320OverheadControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false); // ja tenim el nostre propi Bitmap
            BackColor = Color.Black;
            _anim.Tick += (s, e) => AnimTick();
            Build();
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
            _offY = Math.Max(0, Height - _contentH); // si hi cap sencer, queda enganxat a baix
            if (!CreateSurface(cw, _contentH, out _hDib, out _bmp, out _memDC, out _oldObj)) return false;
            ClampScroll();
            if (_openAtBottom) { _scrollY = MaxScroll; _openAtBottom = false; }
            DrawContent(_bmp!, _s, new Rectangle(0, 0, cw, _contentH), CancellationToken.None);
            return true;
        }

        int ContentWidth => Math.Max(1, (int)Math.Round(Width * _zoom / 100.0));

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
                (int)Math.Floor(logical.Left * _s) - 1, (int)Math.Floor(logical.Top * _s) - 1,
                (int)Math.Ceiling(logical.Right * _s) + 1, (int)Math.Ceiling(logical.Bottom * _s) + 1);
            px.Intersect(new Rectangle(0, 0, _bmp.Width, _bmp.Height));
            if (px.Width <= 0 || px.Height <= 0) return;
            DrawContent(_bmp, _s, px, CancellationToken.None);
            if (invalidate) Invalidate(new Rectangle(px.X + _offX, px.Y + _offY - _scrollY, px.Width, px.Height));
        }

        /// <summary>Dibuixa la zona px (píxels del bitmap) del panell a l'escala s. Pot córrer en segon pla.</summary>
        void DrawContent(Bitmap target, float s, Rectangle px, CancellationToken ct)
        {
            var lr = RectangleF.FromLTRB(px.Left / s, px.Top / s, px.Right / s, px.Bottom / s);
            using var g = Graphics.FromImage(target);
            g.SetClip(px);
            g.FillRectangle(Brushes.Black, px);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.ScaleTransform(s, s);
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
            new(7, 119), new(681, 9), new(1241, 9), new(1241, 740), new(519, 740), new(519, 656), new(7, 656)
        };

        /// <summary>Llum global del cockpit: una mica més clar a dalt, més fosc a baix i a les vores laterals.</summary>
        void DrawLighting(Graphics g)
        {
            var full = new RectangleF(0, 0, LW, CurLH);
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
            using var b = new LinearGradientBrush(new RectangleF(0, 0, LW, CurLH), Color.FromArgb(82, 110, 134), Oh.Frame, 90f);
            g.FillPolygon(b, CurFrame);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (!EnsureBitmap()) { g.Clear(Color.Black); return; }
            var clip = e.ClipRectangle;

            // La zona dels botons + / − es compon fora de pantalla (panell + botons) i es copia d'un sol cop:
            // així no es veu mai el panell sense els botons a sobre (sense pampallugues en desplaçar).
            var btnArea = Rectangle.Inflate(AllBtnsRect, 4, 4);
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
            using var outside = new Region(r);
            outside.Exclude(content);
            outside.Translate(-origin.X, -origin.Y);
            g.FillRegion(Brushes.Black, outside);
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

        int MaxScroll => Math.Max(0, _contentH - Height);
        int ZoomBtnSize => Math.Max(48, Width / 34);
        Point DefaultMinusPos => new(Width - ZoomBtnSize - 40, Height - ZoomBtnSize - 40);
        Point DefaultPlusPos => new(Width - ZoomBtnSize - 40, Height - ZoomBtnSize * 2 - 56);
        Rectangle PlusRect => new(PlusLocation ?? DefaultPlusPos, new Size(ZoomBtnSize, ZoomBtnSize));
        Rectangle MinusRect => new(MinusLocation ?? DefaultMinusPos, new Size(ZoomBtnSize, ZoomBtnSize));
        Point DefaultPagePos => new(Width - ZoomBtnSize - 40, Height - ZoomBtnSize * 3 - 72);
        Rectangle PageRect => new(PageLocation ?? DefaultPagePos, new Size(ZoomBtnSize, ZoomBtnSize));
        Rectangle BtnRect(int b) => b == 1 ? PlusRect : b == 2 ? MinusRect : PageRect;
        Rectangle AllBtnsRect => Rectangle.Union(Rectangle.Union(PlusRect, MinusRect), PageRect);

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
            cur.ContentH = _contentH; cur.S = _s; cur.Zoom = _zoom;
            _bmp = null; _hDib = _memDC = _oldObj = IntPtr.Zero;

            _page = 1 - _page;
            var nxt = _pages[_page];
            if (nxt.Bmp != null && nxt.Zoom == _zoom && nxt.Bmp.Width == ContentWidth)
            {
                _bmp = nxt.Bmp; _hDib = nxt.HDib; _memDC = nxt.MemDC; _oldObj = nxt.OldObj;
                _contentH = nxt.ContentH; _s = nxt.S;
                _offX = (Width - _bmp.Width) / 2;
                _offY = Math.Max(0, Height - _contentH);
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
            _offY = Math.Max(0, Height - _contentH);
            _scrollY = (int)Math.Round(frac * MaxScroll);
            ClampScroll();
            Invalidate();

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

        /// <summary>La propera vegada que es mostri, el panell apareix amb la vora de baix a baix de la pantalla.</summary>
        public void OpenAtBottom()
        {
            if (_page != 0) { _pages[0].ScrollY = int.MaxValue; _pages[0].Visited = true; return; }
            _openAtBottom = true;
            if (_bmp != null) { _scrollY = MaxScroll; _openAtBottom = false; Invalidate(); }
        }

        void DrawZoomButtons(Graphics g, Rectangle clip)
        {
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var (r, plus) in new[] { (PlusRect, true), (MinusRect, false) })
            {
                if (!r.IntersectsWith(clip)) continue;
                bool pressed = _btnPressed == (plus ? 1 : 2);
                bool enabled = plus ? _zoom < MaxZoom : _zoom > MinZoom;
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
                g.DrawString(_page == 0 ? "MAIN" : "OVHD", f, tb, pr.X + pr.Width / 2f, pr.Y + pr.Height / 2f, Oh.Center);
                g.TextRenderingHint = oldHint;
            }
            g.SmoothingMode = oldMode;
        }

        int ZoomButtonAt(Point p) => PlusRect.Contains(p) ? 1 : MinusRect.Contains(p) ? 2 : PageRect.Contains(p) ? 3 : 0;

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

            if (_animating.Count == 0 && _tests.Count == 0 && _flingV == 0 && !barPending && !_hasPendingScroll && !_down)
                _anim.Stop();
        }

        void ClampScroll() => _scrollY = Math.Clamp(_scrollY, 0, MaxScroll);

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
                foreach (var zr in new[] { PlusRect, MinusRect, PageRect })
                    Invalidate(new Rectangle(zr.X - 3, 0, zr.Width + 6, Height));
            }
            else Invalidate();
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
                else { _btnPressed = zb; Invalidate(BtnRect(zb)); }
                return;
            }
            _down = true; _dragging = false; _flingV = 0; _velY = 0;
            _downPt = e.Location; _downScroll = _scrollY;
            _lastMoveY = e.Y; _lastMoveT = _clock.Elapsed.TotalSeconds;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_btnDrag != 0)
            {
                var old = BtnRect(_btnDrag);
                var np = new Point(Math.Clamp(e.X - _btnDragOff.X, 0, Width - old.Width), Math.Clamp(e.Y - _btnDragOff.Y, 0, Height - old.Height));
                if (_btnDrag == 1) PlusLocation = np; else if (_btnDrag == 2) MinusLocation = np; else PageLocation = np;
                Invalidate(old); Invalidate(BtnRect(_btnDrag));
                return;
            }
            if (!_down) return;
            int dy = e.Y - _downPt.Y;
            if (!_dragging && Math.Abs(dy) > Math.Max(10, Width / 200)) _dragging = true;
            if (!_dragging) return;
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
                Invalidate(PlusRect); Invalidate(MinusRect); Invalidate(PageRect);
                if (ZoomButtonAt(e.Location) == b)
                {
                    if (b == 3) TogglePage();
                    else SetZoom(_zoom + (b == 1 ? 10 : -10));
                }
                return;
            }
            if (!_down) return;
            _down = false;
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

        void TapAt(Point pt)
        {
            if (!EnsureBitmap()) return;
            var p = new PointF((pt.X - _offX) / _s, (pt.Y - _offY + _scrollY) / _s);
            for (int i = _els.Count - 1; i >= 0; i--)
                if (_els[i].HitTest(p)) { _els[i].Tap(p); return; }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_bmp != null && _bmp.Width != Math.Max(1, (int)Math.Round(Width * _zoom / 100.0))) ReleaseCache();
            _offY = Math.Max(0, Height - _contentH);
            ClampScroll();
            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) ReleaseCache();
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
            _silk.Add(new OhPoly(new[] { Pt(12, 190), Pt(1090, 14), Pt(1985, 14), Pt(1985, 42), Pt(1092, 42), Pt(12, 218) },
                                 Color.FromArgb(70, 76, 84), Color.FromArgb(30, 34, 38), true));
            // planxa esquerra (inclinada a dalt)
            _silk.Add(new OhPanel(new[] { Pt(15, 222), Pt(882, 80), Pt(882, 288), Pt(15, 288) },
                                  new[] { Pt(30, 222), Pt(868, 92), Pt(30, 276), Pt(868, 276) }));
            Pb("MIP_AUTO_LAND", 175, 237, 50, 46, "AUTO", "LAND", true).UpperColor = Oh.Red;
            ((OhPushButton)_byId["MIP_AUTO_LAND"]).LowerColor = Oh.Red;
            var mw = Pb("MIP_MASTER_WARN", 580, 160, 50, 46, "MASTER", "WARN", true); mw.UpperColor = Oh.Red; mw.LowerColor = Oh.Red;
            var mc = Pb("MIP_MASTER_CAUT", 580, 240, 50, 46, "MASTER", "CAUT", true); mc.UpperColor = Oh.Amber; mc.LowerColor = Oh.Amber;
            Lb("CHRONO", 812, 122);
            Round("MIP_CHRONO", 815 * k, 160 * k, 12f);
            Lb("SIDE STICK PRIORITY", 812, 200);
            var ssp = Pb("MIP_SIDESTICK_PRIORITY", 813, 238, 46, 40, "→", "CAPT"); ssp.LowerColor = Oh.Green; ssp.UpperColor = Oh.White;

            // panell EFIS
            Panel(890 * k, 52 * k, (1296 - 890) * k, (288 - 52) * k);
            Lb("QNH", 958, 110);
            _silk.Add(new OhDarkWindow(R(921, 120, 994, 150)));
            Lb("in Hg", 928, 168, 8.15f); Lb("h Pa", 990, 168, 8.15f);
            Knob("MIP_BARO", 958 * k, 202 * k, 15f, 4, new float[] { -120, -90, -60, -30, 0, 30, 60, 90, 120 }, KnobStyle.Round);
            KeyL("MIP_EFIS_ILS", 937, 262, 42, 28, "ILS");
            var fd = KeyL("MIP_EFIS_FD", 987, 262, 42, 28, "FD"); fd.On = true;
            string[] ndKeys = { "CSTR", "WPT", "VOR.D", "NDB", "ARPT" };
            float[] ndX = { 1077, 1122, 1166, 1211, 1256 };
            for (int i = 0; i < 5; i++) KeyL("MIP_EFIS_" + ndKeys[i].Replace(".", ""), ndX[i], 95, 42, 30, ndKeys[i]);
            // selector de mode del ND
            float[] modeAng = { -105, -55, -10, 40, 90 };
            _silk.Add(new OhArcDial(1095 * k, 178 * k, 25, -105, 90, modeAng, false));
            Knob("MIP_ND_MODE", 1095 * k, 178 * k, 19f, 2, modeAng);
            Lb("ROSE", 1050, 126); Lb("LS", 1046, 178); Lb("VOR", 1060, 144); Lb("NAV", 1093, 129); Lb("ARC", 1128, 140); Lb("PLAN", 1153, 182);
            // selector d'abast del ND
            float[] rngAng = { -105, -55, 0, 55, 100, 140 };
            _silk.Add(new OhArcDial(1236 * k, 178 * k, 25, -105, 140, rngAng, false));
            Knob("MIP_ND_RANGE", 1236 * k, 178 * k, 19f, 2, rngAng);
            Lb("10", 1182, 178); Lb("20", 1196, 144); Lb("40", 1233, 126); Lb("80", 1272, 144); Lb("160", 1282, 186); Lb("320", 1272, 216);
            // ADF/VOR 1 i 2 (3 posicions; OFF a baix)
            float[] adfAng = { 290, 180, 70 };
            Knob("MIP_EFIS_ADFVOR1", 1096 * k, 250 * k, 9f, 1, adfAng);
            Lb("ADF1", 1055, 252); Lb("VOR1", 1137, 252); Lb("OFF", 1097, 280);
            Knob("MIP_EFIS_ADFVOR2", 1236 * k, 250 * k, 9f, 1, adfAng);
            Lb("ADF2", 1195, 252); Lb("VOR2", 1277, 252); Lb("OFF", 1237, 280);

            // FCU
            Panel(1302 * k, 50 * k, (1980 - 1302) * k, (288 - 50) * k);
            _silk.Add(new OhDarkWindow(R(1345, 60, 1950, 115)));
            Lb("SPD", 1330, 125); Lb("MACH", 1330, 170);
            Round("MIP_FCU_SPD_MACH", 1330 * k, 146 * k, 8f);
            Knob("MIP_FCU_SPD", 1388 * k, 176 * k, 20f, 0, Enumerable.Range(0, 12).Select(i => i * 30f).ToArray(), KnobStyle.Round).Wrap = true;
            Knob("MIP_FCU_HDG", 1505 * k, 176 * k, 20f, 0, Enumerable.Range(0, 12).Select(i => i * 30f).ToArray(), KnobStyle.Round).Wrap = true;
            KeyL("MIP_FCU_LOC", 1505, 258, 46, 30, "LOC");
            WLn(1558 * k, 128 * k, 1558 * k, 272 * k);
            Lb("HDG", 1586, 133); Lb("TRK", 1586, 147); Lb("V/S", 1668, 133); Lb("FPA", 1668, 147);
            Round("MIP_FCU_HDGTRK_VSFPA", 1628 * k, 146 * k, 8f);
            KeyL("MIP_FCU_AP1", 1597, 192, 48, 40, "AP 1");
            KeyL("MIP_FCU_AP2", 1657, 192, 48, 40, "AP 2");
            KeyL("MIP_FCU_ATHR", 1628, 250, 48, 38, "A/THR");
            WLn(1700 * k, 128 * k, 1700 * k, 272 * k);
            Lb("100", 1722, 128); Lb("1000", 1783, 128);
            Knob("MIP_FCU_ALT", 1750 * k, 176 * k, 20f, 0, Enumerable.Range(0, 12).Select(i => i * 30f).ToArray(), KnobStyle.Round).Wrap = true;
            Lb("METRIC", 1820, 168); Lb("ALT", 1820, 182);
            Round("MIP_FCU_METRIC_ALT", 1818 * k, 145 * k, 8f);
            KeyL("MIP_FCU_EXPED", 1755, 255, 48, 30, "EXPED");
            Lb("UP", 1906, 128); Lb("DN", 1906, 218);
            Knob("MIP_FCU_VS", 1900 * k, 172 * k, 19f, 0, Enumerable.Range(0, 12).Select(i => i * 30f).ToArray(), KnobStyle.Round).Wrap = true;
            Lb("PUSH", 1958, 150); Lb("TO", 1958, 164); Lb("LEVEL", 1958, 178); Lb("OFF", 1958, 192);
            KeyL("MIP_FCU_APPR", 1908, 255, 48, 30, "APPR");

            // llavi inferior del glareshield amb les sortides d'aire
            _silk.Add(new OhPoly(new[] { Pt(12, 294), Pt(1985, 294), Pt(1985, 332), Pt(12, 332) },
                                 Color.FromArgb(74, 94, 112), Color.FromArgb(30, 40, 50), true));
            foreach (float vx in new float[] { 842, 1408, 1876 }) _silk.Add(new OhVent(vx * k, 314 * k, 28, 10));

            // ── Panell principal ──
            // planxa de l'esquerra (PFD/ND i zona dels genolls)
            _silk.Add(new OhPanel(new[] { Pt(15, 338), Pt(872, 338), Pt(872, 1046), Pt(15, 1046) },
                                  new[] { Pt(28, 352), Pt(858, 352), Pt(28, 1032), Pt(858, 1032) }));
            // planxa central i dreta
            _silk.Add(new OhPanel(new[] { Pt(880, 338), Pt(1982, 338), Pt(1982, 1178), Pt(880, 1178) },
                                  new[] { Pt(894, 352), Pt(1968, 352), Pt(894, 1164), Pt(1968, 1164) }));

            // pantalles (en negre)
            _silk.Add(new OhScreen(R(24, 372, 428, 772), R(38, 388, 414, 756), 4f, true));      // PFD
            _silk.Add(new OhScreen(R(454, 372, 868, 772), R(470, 388, 852, 756), 4f, true));    // ND
            _silk.Add(new OhBlock(R(520, 776, 812, 796), Color.FromArgb(116, 142, 164), Color.FromArgb(66, 88, 106), 3f));
            _silk.Add(new OhScreen(R(1254, 344, 1686, 736), R(1280, 362, 1660, 728), 4f, true));  // ECAM superior
            _silk.Add(new OhScreen(R(1254, 744, 1686, 1136), R(1282, 758, 1660, 1128), 4f, true)); // ECAM inferior

            // zona dels genolls (fosca)
            _silk.Add(new OhBlock(R(15, 846, 660, 914), Color.FromArgb(70, 72, 76), Color.FromArgb(14, 14, 16), 12f));
            _silk.Add(new OhBlock(R(22, 926, 482, 1044), Color.FromArgb(58, 60, 64), Color.FromArgb(26, 26, 28), 2f));
            _silk.Add(new OhBlock(R(550, 926, 788, 1036), Color.FromArgb(70, 72, 76), Color.FromArgb(30, 30, 32), 2f, 7));
            Oh_BigScrews(new[] { Pt(676, 818), Pt(860, 948) });

            // TERR ON ND (capità)
            Panel(890 * k, 655 * k, (1016 - 890) * k, (792 - 655) * k);
            Lb("TERR ON ND", 945, 667);
            Pb("MIP_TERR_ON_ND_1", 942, 698, 44, 40, "••", "ON");

            // ISIS (pantalla negra amb botons)
            _silk.Add(new OhScreen(R(1030, 520, 1222, 714), R(1058, 548, 1196, 688), 12f, false));
            Key("MIP_ISIS_BUGS", 1078 * k, 526 * k, 34 * k, 16 * k, "BUGS");
            Key("MIP_ISIS_LS", 1144 * k, 526 * k, 30 * k, 16 * k, "LS");
            Key("MIP_ISIS_PLUS", 1034 * k, 578 * k, 18 * k, 24 * k, "+");
            Key("MIP_ISIS_MINUS", 1034 * k, 640 * k, 18 * k, 24 * k, "-");
            Key("MIP_ISIS_ATT", 1052 * k, 694 * k, 30 * k, 15 * k, "ATT");
            Key("MIP_ISIS_RST", 1088 * k, 694 * k, 30 * k, 15 * k, "RST");
            Knob("MIP_ISIS_BARO", 1193 * k, 686 * k, 10f, 0, Enumerable.Range(0, 12).Select(i => i * 30f).ToArray(), KnobStyle.Round).Wrap = true;
            // panell buit sota l'ISIS
            _silk.Add(new OhPanel(new[] { Pt(1036, 730), Pt(1228, 730), Pt(1228, 970), Pt(1036, 970) },
                                  new[] { Pt(1048, 744), Pt(1216, 744), Pt(1048, 956), Pt(1216, 956) }));

            // DCDU (pantalla negra amb tecles laterals)
            _silk.Add(new OhScreen(R(880, 996, 1206, 1176), R(930, 1016, 1156, 1152), 6f, true));
            string[] dl = { "BRT", "DIM", "MSG-", "MSG+", "", "" };
            string[] dr = { "PRINT", "PGE-", "PGE+", "", "" };
            for (int i = 0; i < dl.Length; i++) Key("MIP_DCDU_L" + i, 890 * k, (1016 + i * 22) * k, 28 * k, 15 * k, dl[i]).Momentary = true;
            for (int i = 0; i < dr.Length; i++) Key("MIP_DCDU_R" + i, 1166 * k, (1032 + i * 22) * k, 30 * k, 15 * k, dr[i]).Momentary = true;

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
            var bf = Pb("MIP_BRK_FAN", 1930, 383, 40, 38, "HOT", "ON"); bf.UpperColor = Oh.Amber;
            WLn(1730 * k, 416 * k, 1966 * k, 416 * k);
            Lb("AUTO/BRK", 1806, 428);
            Lb("LO", 1756, 443); Lb("MED", 1801, 443); Lb("MAX", 1858, 443);
            foreach (var (id, x) in new[] { ("LO", 1756f), ("MED", 1801f), ("MAX", 1858f) })
            {
                var ab = Pb("MIP_AUTOBRK_" + id, x, 474, 42, 40, "DECEL", "ON"); ab.UpperColor = Oh.Green;
            }
            Lb("A/SKID &", 1932, 426); Lb("N/W STRG", 1932, 440);
            TogS("MIP_ASKID_NWSTRG", 1922 * k, 476 * k, 2, 0, ToggleStyle.Bat, 10f);
            Lb("ON", 1958, 458); Lb("OFF", 1958, 494);

            // TERR ON ND (primer oficial)
            Panel(1885 * k, 526 * k, (1976 - 1885) * k, (614 - 526) * k);
            Lb("TERR ON ND", 1930, 536);
            Pb("MIP_TERR_ON_ND_2", 1930, 572, 40, 36, "••", "ON");

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
