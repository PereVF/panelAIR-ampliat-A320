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
        public static readonly Color Frame   = Color.FromArgb(46, 55, 64);
        public static readonly Color PlateA  = Color.FromArgb(102, 115, 129);
        public static readonly Color PlateB  = Color.FromArgb(86, 99, 113);
        public static readonly Color PlateHi = Color.FromArgb(144, 156, 168);
        public static readonly Color PlateLo = Color.FromArgb(36, 44, 52);

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
            _t = t; _x = x; _y = y; _size = size; _c = c; _bold = bold;
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
            _t = t; _x = x; _y = y; _size = size;
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
            _t = t; _x = x; _y = y; _size = size; _c = c;
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
            _t = t; _cx = cx; _y = y; _x0 = x0; _x1 = x1; _size = size;
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
            _cx = cx; _cy = cy; _r = r; _angles = angles; _labels = labels; _size = size;
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

    internal enum KnobStyle { Pointer, Round }

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
        public override RectangleF DirtyBounds => RectangleF.Inflate(Bounds, 5, 5);

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

        public override void Draw(Graphics g)
        {
            var rect = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            Oh.SoftShadowEllipse(g, rect, 1.8f, 2.8f, 3.2f, 130);
            if (Style == KnobStyle.Pointer)
            {
                // Rotatiu Airbus: faldilla rodona color crema + aleta alta que fa de punter
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    using var pg = new PathGradientBrush(path)
                    {
                        CenterColor = Color.FromArgb(248, 247, 242),
                        SurroundColors = new[] { Color.FromArgb(182, 180, 172) },
                        CenterPoint = new PointF(_cx - _r * 0.3f, _cy - _r * 0.35f)
                    };
                    g.FillPath(pg, path);
                }
                using (var p = new Pen(Color.FromArgb(84, 82, 76), 0.8f)) g.DrawEllipse(p, rect);
                var st = g.Save();
                g.TranslateTransform(_cx, _cy); g.RotateTransform(_ang);
                // l'aleta: més estreta a la punta, amb cares laterals ombrejades
                var fin = new[]
                {
                    new PointF(-_r * 0.34f, _r * 0.95f), new PointF(-_r * 0.24f, -_r * 1.02f),
                    new PointF(_r * 0.24f, -_r * 1.02f), new PointF(_r * 0.34f, _r * 0.95f)
                };
                using (var fp = new GraphicsPath())
                {
                    fp.AddPolygon(fin);
                    // l'ombra es projecta en direcció fixa (la llum no gira amb el rotatiu)
                    double a = -_ang * Math.PI / 180.0; // desplaçament (1.6, 2.4) del món, expressat en el marc girat
                    Oh.SoftShadow(g, fp, (float)(1.6 * Math.Cos(a) - 2.4 * Math.Sin(a)), (float)(1.6 * Math.Sin(a) + 2.4 * Math.Cos(a)), 2.2f, 110);
                    var fr = new RectangleF(-_r * 0.36f, -_r * 1.05f, _r * 0.72f, _r * 2.1f);
                    using var fb = new LinearGradientBrush(fr, Color.Black, Color.Black, 0f)
                    {
                        InterpolationColors = new ColorBlend
                        {
                            Colors = new[] { Color.FromArgb(150, 148, 140), Color.FromArgb(252, 251, 246), Color.FromArgb(228, 226, 218), Color.FromArgb(132, 130, 122) },
                            Positions = new[] { 0f, 0.38f, 0.62f, 1f }
                        }
                    };
                    g.FillPath(fb, fp);
                    using var pe = new Pen(Color.FromArgb(96, 94, 88), 0.6f); g.DrawPath(pe, fp);
                }
                using (var lp = new Pen(Color.FromArgb(46, 46, 46), Math.Max(0.9f, _r * 0.065f))) g.DrawLine(lp, 0, -_r * 0.15f, 0, -_r * 0.96f);
                g.Restore(st);
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
                    float bw = R * 0.2f, tw = R * 0.16f;
                    Shaft(g, bw, tw, tipY, Color.FromArgb(14, 14, 16), Color.FromArgb(118, 118, 122));
                    float br = R * (0.25f + 0.12f * ext);
                    var ball = new RectangleF(_cx - br, tipY - br * (0.85f + 0.15f * ext), br * 2, br * (1.7f + 0.3f * ext));
                    Oh.SoftShadowEllipse(g, ball, 1.4f, 2.0f, 2.2f, 120);
                    Oh.Specular(g, ball, Color.FromArgb(240, 236, 226), Color.FromArgb(78, 74, 68));
                    break;
                }
                case ToggleStyle.Paddle:
                {
                    float L = R * 0.95f;
                    float tipY = _cy + _pos * L;
                    float bw = R * 0.36f, tw = R * 0.44f;
                    Shaft(g, bw, tw, tipY, Color.FromArgb(10, 10, 12), Color.FromArgb(70, 70, 74));
                    // cara frontal plana de la pala (clara), més alta com més apunta a l'observador
                    float fh = R * (0.24f + 0.55f * ext);
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
        public string Label; public bool Led; public bool On; public string? Group; public bool LitText; public bool Arrows; public bool Momentary;
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
                var led = new RectangleF(f.X + 3, f.Y + 2.2f, Math.Min(7f, f.Width * 0.25f), 2.6f);
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

    // ═════════════════════════════════════ EL CONTROL ════════════════════════════════════════

    public sealed class A320OverheadControl : Control
    {
        public const float LW = 1250f, LH = 1275f;

        readonly List<OhSilk> _silk = new();
        List<OhElement> _els = new();
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
            _els = _els.OrderBy(e => e.Z).ToList();
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
            _s = Width / LW;
            _contentH = (int)Math.Ceiling(LH * _s);
            var bmi = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = Width, biHeight = -_contentH, // negatiu = files de dalt a baix
                biPlanes = 1, biBitCount = 32
            };
            _hDib = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out IntPtr bits, IntPtr.Zero, 0);
            if (_hDib == IntPtr.Zero || bits == IntPtr.Zero) { _hDib = IntPtr.Zero; return false; }
            _bmp = new Bitmap(Width, _contentH, Width * 4, PixelFormat.Format32bppPArgb, bits);
            _memDC = CreateCompatibleDC(IntPtr.Zero);
            _oldObj = SelectObject(_memDC, _hDib);
            ClampScroll();
            Redraw(new RectangleF(0, 0, LW, LH), invalidate: false);
            return true;
        }

        void ReleaseCache()
        {
            // Porta les animacions pendents al seu estat final (sense dibuixar: el bitmap s'allibera)
            var pending = _animating.ToArray();
            _bmp?.Dispose();
            _bmp = null;
            if (_memDC != IntPtr.Zero) { SelectObject(_memDC, _oldObj); DeleteDC(_memDC); _memDC = IntPtr.Zero; }
            if (_hDib != IntPtr.Zero) { DeleteObject(_hDib); _hDib = IntPtr.Zero; }
            _hasPendingScroll = false;
            foreach (var el in pending) { for (int i = 0; i < 200 && el.Animating; i++) el.Tick(0.05f); }
            foreach (var t in _tests) t.el.Test = false;
            _tests.Clear();
            _animating.Clear();
            _anim.Stop();
            _flingV = 0;
            _bmp?.Dispose();
            _bmp = null;
        }

        void Redraw(RectangleF logical, bool invalidate = true)
        {
            if (_bmp == null) return;
            var px = Rectangle.FromLTRB(
                (int)Math.Floor(logical.Left * _s) - 1, (int)Math.Floor(logical.Top * _s) - 1,
                (int)Math.Ceiling(logical.Right * _s) + 1, (int)Math.Ceiling(logical.Bottom * _s) + 1);
            px.Intersect(new Rectangle(0, 0, _bmp.Width, _bmp.Height));
            if (px.Width <= 0 || px.Height <= 0) return;
            var lr = RectangleF.FromLTRB(px.Left / _s, px.Top / _s, px.Right / _s, px.Bottom / _s);

            using (var g = Graphics.FromImage(_bmp))
            {
                g.SetClip(px);
                g.FillRectangle(Brushes.Black, px);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.ScaleTransform(_s, _s);
                Oh.PixelScale = _s;
                DrawFrame(g);
                foreach (var s in _silk) if (s.Bounds.IntersectsWith(lr)) s.Draw(g);
                foreach (var e in _els) if (e.DirtyBounds.IntersectsWith(lr)) e.Draw(g);
                DrawLighting(g);
            }
            if (invalidate) Invalidate(new Rectangle(px.X, px.Y - _scrollY, px.Width, px.Height));
        }

        static readonly PointF[] FramePoly =
        {
            new(12, 5), new(1238, 5), new(1238, 1104), new(950, 1266), new(300, 1266), new(12, 1104)
        };

        /// <summary>Llum global del cockpit: una mica més clar a dalt, més fosc a baix i a les vores laterals.</summary>
        static void DrawLighting(Graphics g)
        {
            var full = new RectangleF(0, 0, LW, LH);
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

        static void DrawFrame(Graphics g)
        {
            using var b = new LinearGradientBrush(new RectangleF(0, 0, LW, LH), Color.FromArgb(88, 108, 126), Oh.Frame, 90f);
            g.FillPolygon(b, FramePoly);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (!EnsureBitmap()) { g.Clear(Color.Black); return; }
            var clip = e.ClipRectangle;
            int contentBottom = _contentH - _scrollY;
            int bh = Math.Min(clip.Bottom, contentBottom) - clip.Y;
            if (bh > 0 && clip.Width > 0)
            {
                IntPtr hdc = g.GetHdc();
                try { BitBlt(hdc, clip.X, clip.Y, clip.Width, bh, _memDC, clip.X, clip.Y + _scrollY, SRCCOPY); }
                finally { g.ReleaseHdc(hdc); }
            }
            if (clip.Bottom > contentBottom)
                g.FillRectangle(Brushes.Black, clip.X, Math.Max(clip.Y, contentBottom), clip.Width, clip.Bottom - Math.Max(clip.Y, contentBottom));

            if (DateTime.UtcNow < _barHideAt && _contentH > Height)
            {
                var bar = ScrollBarRect();
                using var bb = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
                g.FillRectangle(bb, bar);
            }
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

        void ClampScroll() => _scrollY = Math.Clamp(_scrollY, 0, Math.Max(0, _contentH - Height));

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
            _down = true; _dragging = false; _flingV = 0; _velY = 0;
            _downPt = e.Location; _downScroll = _scrollY;
            _lastMoveY = e.Y; _lastMoveT = _clock.Elapsed.TotalSeconds;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
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
            var p = new PointF(pt.X / _s, (pt.Y + _scrollY) / _s);
            for (int i = _els.Count - 1; i >= 0; i--)
                if (_els[i].HitTest(p)) { _els[i].Tap(p); return; }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_bmp != null && _bmp.Width != Width) ReleaseCache();
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
        void L(string t, float x, float y, float size = 6.5f, bool bold = true) => _silk.Add(new OhLabel(t, x, y, size, Oh.Silk, bold));
        void VT(string t, float x, float y, float size = 10f) => _silk.Add(new OhVText(t, x, y, size));
        void Box(string t, float x, float y, float size = 6.5f) => _silk.Add(new OhBoxLabel(t, x, y, size, Oh.Silk));
        void Title(string t, float cx, float y, float x0, float x1, float size = 9.5f) => _silk.Add(new OhTitle(t, cx, y, x0, x1, size));
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
            Add(new OhToggle(id, cx, cy, n, index, paddle ? ToggleStyle.Paddle : ToggleStyle.Bat, size) { SpringToCenter = spring });
        OhToggle TogS(string id, float cx, float cy, int n, int index, ToggleStyle style, float size) =>
            Add(new OhToggle(id, cx, cy, n, index, style, size));
        OhRoundButton Round(string id, float cx, float cy, float r) => Add(new OhRoundButton(id, cx, cy, r));
        OhSeg7 Seg(string id, float x, float y, float w, float h, string text, int cells, string suffix = "") =>
            Add(new OhSeg7(id, new RectangleF(x, y, w, h), text, cells, suffix));
        OhKey Key(string id, float x, float y, float w, float h, string label) => Add(new OhKey(id, new RectangleF(x, y, w, h), label));
        OhVolKnob Vol(string id, float cx, float cy, float r) => Add(new OhVolKnob(id, cx, cy, r));

        void Build()
        {
            BuildLeft();
            BuildCenter();
            BuildRight();
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

            // AIR COND — segons la fotografia de referència
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

            L("CABIN PRESS", 800, 1008, 8.5f);
            L("MAN V/S CTL", 700, 1021, 5.5f); L("UP", 719, 1031, 5.5f); L("DN", 719, 1068, 5.5f);
            Tog("PRESS_MAN_VS_CTL", 700, 1050, 3, 1, false, 10, spring: true);
            L("MODE SEL", 760, 1021, 5.5f);
            PB("PRESS_MODE_SEL", 760, 1030, "FAULT", "MAN", 40, 38);
            VT("AUTO", 788, 1049, 5f);
            L("LDG ELEV", 838, 1018, 5.5f);
            float[] le = { -150, -110, -80, -50, -20, 10, 40, 70, 100, 130 };
            _silk.Add(new OhDial(838, 1052, 18.5f, le, new[] { "AUTO", "-2", "0", "2", "4", "6", "8", "10", "12", "14" }, 4f));
            Knob("PRESS_LDG_ELEV", 838, 1052, 16, 0, le);
            L("DITCHING", 900, 1021, 5.5f);
            GrayGuard("PRESS_DITCHING", 900, 1030, "••", "ON", 38, 40);

            Title("EXT LT", 455, 1086, 322, 578);
            // EXT LT — estil fidel a la referència: sòcols foscos, pales/palanques/càpsules LAND
            // EXT LT — proporcions de la referència: sòcols grans i etiquetes enganxades a l'interruptor
            const float es = 14.5f;   // mida dels interruptors
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
            L("ON", 452, 1185, fp); L("OFF", 452, 1197, fp); L("RETRACT", 452, 1209, 7f);
            TogS("EXT_LT_LAND_L", 420, 1197, 3, 2, ToggleStyle.Land, es);
            TogS("EXT_LT_LAND_R", 484, 1197, 3, 2, ToggleStyle.Land, es);
            L("NOSE", 541, 1164, fn); L("T.O", 568, 1185, fp); L("TAXI", 570, 1197, fp); L("OFF", 568, 1209, fp);
            TogS("EXT_LT_NOSE", 541, 1197, 3, 2, ToggleStyle.Bat, es);

            Title("APU", 627, 1090, 590, 664, 8.5f);
            L("MASTER SW", 627, 1104, 6f);
            PB("APU_MASTER_SW", 627, 1112, "FAULT", "ON");
            L("START", 627, 1170, 6f);
            PB("APU_START", 627, 1178, "AVAIL", "ON");

            Title("INT LT", 845, 1090, 796, 930, 8.5f);
            L("OVHD INTEG LT", 704, 1097, 5f);
            float[] integ = { -140, -100, -60, -20, 20, 60, 100, 140 };
            _silk.Add(new OhDial(704, 1128, 18, integ, null, 5f));
            Knob("INT_LT_OVHD_INTEG", 704, 1128, 16, 4, integ, KnobStyle.Round);
            L("OFF", 688, 1150, 5f); L("BRT", 722, 1150, 5f);
            L("ICE IND &", 763, 1095, 5f); L("STBY COMPASS", 763, 1103, 5f);
            Tog("INT_LT_ICE_IND_STBY_COMPASS", 763, 1130, 2, 1, false, 9);
            L("DOME", 815, 1100, 5.5f); L("BRT", 840, 1116, 5f); L("DIM", 840, 1130, 5f); L("OFF", 840, 1144, 5f);
            Tog("INT_LT_DOME", 815, 1130, 3, 2, false, 9);
            L("ANN LT", 877, 1100, 5.5f); L("TEST", 905, 1116, 5f); L("BRT", 905, 1130, 5f); L("DIM", 905, 1144, 5f);
            var ann = Tog("INT_LT_ANN_LT", 877, 1130, 3, 1, false, 9);
            ann.OnChange = t => { _annMode = ((OhToggle)t).Index; Redraw(new RectangleF(0, 0, LW, LH)); };

            Title("SIGNS", 800, 1165, 676, 930, 8.5f);
            L("SEAT BELTS", 700, 1176, 5.5f); L("ON", 700, 1186, 5f); L("OFF", 700, 1232, 5f);
            Tog("SIGNS_SEAT_BELTS", 700, 1208, 2, 1, false, 10);
            L("NO PORTABLE", 750, 1173, 5f); L("ELEC DEVICE", 750, 1181, 5f);
            L("ON", 750, 1189, 4.8f); L("AUTO", 773, 1208, 4.8f); L("OFF", 750, 1232, 5f);
            Tog("SIGNS_NO_PED", 750, 1208, 3, 1, false, 10);
            L("EMER EXIT LT", 852, 1176, 5.5f);
            PB("SIGNS_EMER_EXIT_LT_PB", 812, 1190, "••", "OFF", 38, 36);
            L("ON", 893, 1194, 5f); L("ARM", 895, 1209, 5f); L("OFF", 893, 1224, 5f);
            Tog("SIGNS_EMER_EXIT_LT", 868, 1209, 3, 1, false, 10);
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
            Tog("ACP_INT_RAD", 980, 104, 3, 1, false, 7);
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
            Round("RMP_SEL", 1012, 274, 9);
            L("SEL", 1012, 262, 5f);
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
            L("ON", 1194, 300, 5.5f); L("OFF", 1194, 339, 5.5f);
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
