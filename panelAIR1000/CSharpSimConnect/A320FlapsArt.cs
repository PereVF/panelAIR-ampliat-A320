using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace panelAIR1000
{
    /// <summary>
    /// Dibuix de la palanca FLAPS (dissenyat amb Pere a la URL de proves):
    /// voltant de la hucha del color del pedestal, bombat (el punt més alt al 2) i amb ombra als costats;
    /// ranura, hucha i coll idèntics als de l'SPEED BRAKE; subpom (gallet) i pom negres a sobre.
    /// Totes les mides en unitats lògiques del pedestal (1 px a 4K = 1/3,072).
    /// </summary>
    internal static class FlapsArt
    {
        const float Px = 1f / 3.072f;
        // ajustos de Pere (lliscants de la URL)
        const float PomDy = 1f * Px;          // pom respecte el coll
        const float PomCut = 0.80f;           // alçada del pom (retallat des de dalt)
        const float SubScale = 1.01f;         // mida subpom
        const float SubDy = -8f * Px;         // subpom eix Y
        const float SubRound = 0.83f;         // arrodonit subpom
        const float Shadow = 50f;             // ombra del voltant (0..100)

        public struct Geo
        {
            public float Cx, W, SlotW, PlateW, Gap, Top, Bot, Gw, Gh;
            public float[] Det;
            public RectangleF Surround => RectangleF.FromLTRB(Cx - SlotW / 2 - Gap - PlateW, Top, Cx + SlotW / 2 + Gap + PlateW, Bot);
            public RectangleF Slot => RectangleF.FromLTRB(Cx - SlotW / 2, Top + W * 0.04f, Cx + SlotW / 2, Bot - W * 0.02f);
            /// <summary>centre del pom (sense l'ajust de Pere) quan el coll és a l'alçada y de la hucha</summary>
            public float GripCy(float y) => y - (Gh / 2 + Gh * 0.44f - SlotW * 0.28f * 0.7f);
        }

        /// <summary>Geometria a partir de la guia i les ratlles originals: el conjunt es retalla 27 px per baix i tot
        /// s'escala des de dalt (mateixa distància a la vora del mòdul a dalt i a baix).</summary>
        public static Geo Build(RectangleF gate, float[] det0, float gw, float gh)
        {
            float W = gate.Width, cx = gate.X + W / 2;
            float top = det0[0] - W * 0.27f, bot = det0[^1] + W * 0.16f;
            float cut = 27f * Px, kk = (bot - top - cut) / (bot - top);
            float M(float v) => top + (v - top) * kk;
            return new Geo
            {
                Cx = cx, W = W, SlotW = W * 0.22f, PlateW = W * 0.30f, Gap = W * 0.015f,
                Top = top, Bot = M(bot), Gw = gw, Gh = gh, Det = det0.Select(M).ToArray()
            };
        }

        /// <summary>Rectangle del pom (per tocar-lo) amb el coll a l'alçada y.</summary>
        public static RectangleF PomRect(Geo q, float y)
        {
            float gBottom = q.GripCy(y) + PomDy + q.Gh / 2, h = q.Gh * PomCut;
            return RectangleF.FromLTRB(q.Cx - q.Gw / 2, gBottom - h, q.Cx + q.Gw / 2, gBottom + q.Gh * 0.22f * SubScale);
        }

        /// <summary>Tot el que es pot pintar (inclosa l'ombra i el pom a 0).</summary>
        public static RectangleF Extent(Geo q)
        {
            var s = q.Surround;
            float side = 3.2f + 40f * Px * 0.35f + 40f * Px * 1.3f + 2f;
            var p0 = PomRect(q, q.Det[0]);
            return RectangleF.FromLTRB(s.Left - side, Math.Min(s.Top, p0.Top) - 2, s.Right + side, Math.Max(s.Bottom, PomRect(q, q.Det[^1]).Bottom) + 2);
        }

        static GraphicsPath Round(RectangleF r, float rad)
        {
            var p = new GraphicsPath(); float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            if (d <= 0.01f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }

        public static void Draw(Graphics g, Geo q, string[] lab, float y)
        {
            var sm = g.SmoothingMode; var th = g.TextRenderingHint;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            var sr = q.Surround; float peak = q.Det[2];
            DrawShadow(g, sr, peak);
            DrawSurround(g, sr, peak);
            SbSlot(g, q.Slot, q.Cx, q.SlotW / 2);
            DrawMarks(g, q, lab);
            float gcy = q.GripCy(y), gBottom0 = gcy + q.Gh / 2;   // (coll i llengüetes: sense l'ajust del pom)
            SbNeck(g, q.Slot, q.Cx, q.SlotW / 2, q.SlotW * 0.28f, gBottom0 - q.Gh * 0.12f, gBottom0 + q.Gh * 0.44f, q.Gw);
            foreach (int sg in new[] { -1, 1 })
            {
                var ear = new RectangleF(q.Cx + sg * q.SlotW * 0.28f - (sg > 0 ? 0 : q.SlotW * 0.42f), gBottom0 - q.Gh * 0.02f, q.SlotW * 0.42f, q.Gh * 0.07f);
                using var ep = Round(ear, q.Gh * 0.03f);
                using var eb = new LinearGradientBrush(new RectangleF(0, ear.Y - 0.01f, 1, ear.Height + 0.02f), Color.FromArgb(70, 72, 76), Color.FromArgb(12, 12, 14), 90f);
                g.FillPath(eb, ep);
            }
            float gBottom = gBottom0 + PomDy;
            DrawSub(g, q, gBottom);
            DrawPom(g, q, gBottom);
            g.SmoothingMode = sm; g.TextRenderingHint = th;
        }

        // ── ombra del voltant: només als costats (ni a dalt ni a baix), més ampla on el bombat és més alt ──
        static void DrawShadow(Graphics g, RectangleF r, float peak)
        {
            if (Shadow <= 0) return;
            float L = Shadow / 100f * 40f * Px, span = Math.Max(peak - r.Top, r.Bottom - peak);
            float H(float yy) { float t = (yy - peak) / span; return MathF.Sqrt(Math.Max(0f, 1f - t * t)); }
            var st = g.Save();
            g.SetClip(RectangleF.FromLTRB(r.Left - 100, r.Top, r.Right + 100, r.Bottom), CombineMode.Intersect);
            // difuminat (com el shadowBlur del navegador): capes cada cop més amples i més fluixes
            const int N = 14; float spread = L * 1.0f, dx = L * 0.25f;
            for (int k = N; k >= 1; k--)
            {
                float e0 = spread * k / N;
                var pts = new List<PointF>();
                for (int i = 0; i <= 40; i++) { float yy = r.Top + r.Height * i / 40f, e = 3.2f * (0.15f + 0.85f * H(yy)) + L * 0.35f * H(yy) + e0; pts.Add(new PointF(r.Right + e + dx, yy)); }
                for (int i = 40; i >= 0; i--) { float yy = r.Top + r.Height * i / 40f, e = 3.2f * (0.15f + 0.85f * H(yy)) + L * 0.35f * H(yy) + e0; pts.Add(new PointF(r.Left - e + dx, yy)); }
                using var b = new SolidBrush(Color.FromArgb((int)(0.55f * 255 / N * 1.15f), 0, 0, 0));
                g.FillPolygon(b, pts.ToArray());
            }
            g.Restore(st);
        }

        // ── voltant: peça rectangular del color del pedestal, bombada (més alta al 2) ──
        static void DrawSurround(Graphics g, RectangleF r, float peakY)
        {
            float span = Math.Max(peakY - r.Top, r.Bottom - peakY);
            float H(float yy) { float t = (yy - peakY) / span; return MathF.Sqrt(Math.Max(0f, 1f - t * t)); }
            const float wallMax = 3.2f;
            foreach (int sg in new[] { -1, 1 })
            {
                float ex = sg < 0 ? r.Left : r.Right;
                var pts = new List<PointF>();
                for (int i = 0; i <= 40; i++) pts.Add(new PointF(ex, r.Top + r.Height * i / 40f));
                for (int i = 40; i >= 0; i--) { float yy = r.Top + r.Height * i / 40f; pts.Add(new PointF(ex + sg * wallMax * (0.15f + 0.85f * H(yy)), yy)); }
                using var wb = new SolidBrush(sg < 0 ? Color.FromArgb(58, 70, 82) : Color.FromArgb(44, 54, 64));
                g.FillPolygon(wb, pts.ToArray());
            }
            using (var fb = new LinearGradientBrush(new RectangleF(r.X, r.Top - 0.01f, r.Width, r.Height + 0.02f), Color.Black, Color.Black, 90f))
            {
                var cols = new Color[17]; var pos = new float[17];
                for (int i = 0; i <= 16; i++)
                {
                    float u = i / 16f, yy = r.Top + r.Height * u, h = H(yy), side = yy < peakY ? 1f : 0.85f, k = 0.80f + 0.30f * h * side;
                    cols[i] = Color.FromArgb(Math.Min(255, (int)(96 * k)), Math.Min(255, (int)(114 * k)), Math.Min(255, (int)(130 * k))); pos[i] = u;
                }
                fb.InterpolationColors = new ColorBlend { Colors = cols, Positions = pos };
                g.FillRectangle(fb, r);
            }
            using var hp = new Pen(Color.FromArgb(140, 150, 168, 184), 0.6f);
            g.DrawLine(hp, r.Left, r.Top + 0.3f, r.Right, r.Top + 0.3f);
            g.DrawLine(hp, r.Left, r.Bottom - 0.3f, r.Right, r.Bottom - 0.3f);
        }

        // ── serigrafia: ratlla vertical, ratlletes i 0 / 1 / 2 / 3 / FULL a cada costat ──
        static void DrawMarks(Graphics g, Geo q, string[] lab)
        {
            var det = q.Det;
            using var wp = new Pen(Color.FromArgb(240, 244, 247), 0.9f);
            using var sp2 = new Pen(Color.FromArgb(120, 20, 26, 34), 0.9f);
            using var font = new Font("Arial", 6.4f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fontS = new Font("Arial", 4.8f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var shb = new SolidBrush(Color.FromArgb(150, 20, 26, 34));
            using var wb = new SolidBrush(Color.FromArgb(244, 247, 250));
            foreach (int sg in new[] { -1, 1 })
            {
                float inner = q.Cx + sg * (q.SlotW / 2 + q.Gap);
                float lineX = inner + sg * q.PlateW * 0.18f, tickL = q.PlateW * 0.16f;
                g.DrawLine(sp2, lineX + 0.4f, det[0] - 1.5f + 0.4f, lineX + 0.4f, det[^1] + 0.4f);
                g.DrawLine(wp, lineX, det[0] - 1.5f, lineX, det[^1]);
                for (int i = 0; i < det.Length; i++)
                {
                    float ty = det[i];
                    g.DrawLine(sp2, lineX + 0.4f, ty + 0.4f, lineX + sg * tickL + 0.4f, ty + 0.4f);
                    g.DrawLine(wp, lineX, ty, lineX + sg * tickL, ty);
                    var f = lab[i].Length > 2 ? fontS : font;
                    var sz = g.MeasureString(lab[i], f);
                    float tx = sg > 0 ? lineX + tickL + 0.8f : lineX - tickL - 0.8f - sz.Width, tyy = ty - sz.Height / 2;
                    g.DrawString(lab[i], f, shb, tx + 0.4f, tyy + 0.4f);
                    g.DrawString(lab[i], f, wb, tx, tyy);
                }
            }
        }

        // ── ranura, hucha i coll: còpia de l'SPEED BRAKE ──
        static void SbSlot(Graphics g, RectangleF slot, float cx, float hw)
        {
            using var sp = Round(slot, hw * 0.8f);
            using (var sb = new LinearGradientBrush(new RectangleF(slot.X - 0.01f, 0, slot.Width + 0.02f, 1), Color.Black, Color.Black, 0f)
            { InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(10, 11, 12), Color.FromArgb(46, 48, 50), Color.FromArgb(30, 31, 33), Color.FromArgb(46, 48, 50), Color.FromArgb(10, 11, 12) }, Positions = new[] { 0f, 0.22f, 0.5f, 0.78f, 1f } } })
                g.FillPath(sb, sp);
            using (var tx = new Pen(Color.FromArgb(28, 0, 0, 0), 0.5f))
                for (float ty = slot.Y + 2f; ty < slot.Bottom - 2f; ty += 1.6f)
                    g.DrawLine(tx, slot.X + hw * 0.25f, ty, slot.Right - hw * 0.25f, ty + 0.4f);
            var groove = RectangleF.FromLTRB(cx - hw * 0.25f, slot.Y + hw * 0.5f, cx + hw * 0.25f, slot.Bottom - hw * 0.5f);
            using (var gp0 = Round(groove, hw * 0.35f)) using (var gb0 = new SolidBrush(Color.FromArgb(4, 4, 5))) g.FillPath(gb0, gp0);
            using (var lip = new Pen(Color.FromArgb(120, Oh.PlateHi), 0.6f)) { var st0 = g.Save(); g.TranslateTransform(0.5f, 0.5f); g.DrawPath(lip, sp); g.Restore(st0); }
            using (var ep = new Pen(Color.FromArgb(220, 8, 10, 12), 0.8f)) g.DrawPath(ep, sp);
        }

        static void SbNeck(Graphics g, RectangleF slot, float cx, float hw, float shw, float stemTop, float stemBot, float gw)
        {
            var stem = RectangleF.FromLTRB(cx - shw, stemTop, cx + shw, stemBot);
            float entryY = stem.Bottom - shw * 0.7f;
            var lens = new RectangleF(cx - shw * 1.3f, entryY - shw * 1.5f, shw * 2.6f, shw * 3.0f);
            using var slotPath = Round(slot, hw * 0.8f);
            var grooveR = RectangleF.FromLTRB(cx - hw * 0.25f, slot.Y, cx + hw * 0.25f, slot.Bottom);
            float ecx = cx, ecy = lens.Y + lens.Height / 2f, gwh = hw * 0.25f, fr = gwh * 2f * 0.45f;
            List<PointF> Side(float a, float b, int sg)
            {
                var pts = new List<PointF>();
                if (gwh >= a * 0.98f) return pts;
                float ti = MathF.Acos(gwh / a);
                PointF E(float t) => new(ecx + sg * a * MathF.Cos(t), ecy + b * MathF.Sin(t));
                float spd = MathF.Sqrt(a * a * MathF.Sin(ti) * MathF.Sin(ti) + b * b * MathF.Cos(ti) * MathF.Cos(ti));
                float dt = Math.Min(fr / Math.Max(spd, 0.01f), ti * 0.8f);
                void Fillet(PointF p0, PointF c, PointF p1)
                { for (int i = 0; i <= 8; i++) { float u = i / 8f, v = 1 - u; pts.Add(new PointF(v * v * p0.X + 2 * u * v * c.X + u * u * p1.X, v * v * p0.Y + 2 * u * v * c.Y + u * u * p1.Y)); } }
                var cTop = E(-ti); var cBot = E(ti);
                Fillet(new PointF(cTop.X, cTop.Y - fr), cTop, E(-ti + dt));
                for (int i = 1; i < 24; i++) { float t = -ti + dt + (2 * (ti - dt)) * i / 24f; pts.Add(E(t)); }
                Fillet(E(ti - dt), cBot, new PointF(cBot.X, cBot.Y + fr));
                return pts;
            }
            void Run(Pen pen, List<PointF> pts, Func<PointF, bool> keep)
            {
                var run = new List<PointF>();
                foreach (var p in pts) { if (keep(p)) run.Add(p); else { if (run.Count > 1) g.DrawLines(pen, run.ToArray()); run.Clear(); } }
                if (run.Count > 1) g.DrawLines(pen, run.ToArray());
            }
            float la = lens.Width / 2f, lb2 = lens.Height / 2f;
            var sR = Side(la, lb2, 1); var sL = Side(la, lb2, -1);
            var stClip = g.Save();
            g.SetClip(slotPath, CombineMode.Intersect);
            g.SetClip(grooveR, CombineMode.Exclude);
            if (sR.Count > 2)
            {
                var hole = new List<PointF>(sR); var rl = new List<PointF>(sL); rl.Reverse(); hole.AddRange(rl);
                using (var lb = new SolidBrush(Color.FromArgb(4, 4, 5))) g.FillPolygon(lb, hole.ToArray());
                using (var rp = new Pen(Color.FromArgb(150, 92, 96, 102), shw * 0.18f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
                { Run(rp, sR, p => p.Y <= ecy - lb2 * 0.34f); Run(rp, sL, p => p.Y <= ecy - lb2 * 0.34f); }
                using (var rp2 = new Pen(Color.FromArgb(200, 8, 8, 9), shw * 0.12f) { LineJoin = LineJoin.Round })
                { g.DrawLines(rp2, sR.ToArray()); g.DrawLines(rp2, sL.ToArray()); }
            }
            g.Restore(stClip);
            float sry = shw * lb2 / la;
            using (var stp = new GraphicsPath())
            {
                float rr = Math.Min(gw * 0.14f, shw);
                stp.AddArc(stem.X, stem.Y, rr * 2, rr * 2, 180, 90);
                stp.AddArc(stem.Right - rr * 2, stem.Y, rr * 2, rr * 2, 270, 90);
                stp.AddArc(new RectangleF(stem.X, entryY - sry, stem.Width, sry * 2), 0, 180);
                stp.CloseFigure();
                using (var stb = new LinearGradientBrush(new RectangleF(stem.X - 0.01f, 0, stem.Width + 0.02f, 1), Color.FromArgb(10, 10, 12), Color.FromArgb(50, 52, 56), 0f)) g.FillPath(stb, stp);
                var fade = RectangleF.FromLTRB(stem.X - 0.3f, entryY - shw * 0.6f, stem.Right + 0.3f, entryY + sry + 0.3f);
                var stF = g.Save(); g.SetClip(stp, CombineMode.Intersect);
                using (var fb = new LinearGradientBrush(new RectangleF(fade.X, fade.Y - 0.01f, fade.Width, fade.Height + 0.02f), Color.FromArgb(0, 4, 4, 5), Color.FromArgb(235, 4, 4, 5), 90f))
                    g.FillRectangle(fb, fade);
                g.Restore(stF);
            }
            var stClip2 = g.Save();
            g.SetClip(slotPath, CombineMode.Intersect);
            g.SetClip(grooveR, CombineMode.Exclude);
            using (var lip = new Pen(Color.FromArgb(220, 40, 42, 45), shw * 0.22f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            { Run(lip, sR, p => p.Y >= ecy + lb2 * 0.42f); Run(lip, sL, p => p.Y >= ecy + lb2 * 0.42f); }
            using (var lipHi = new Pen(Color.FromArgb(120, 110, 114, 120), shw * 0.08f) { LineJoin = LineJoin.Round })
            {
                var iR = Side(la - shw * 0.08f, lb2 - shw * 0.08f, 1); var iL = Side(la - shw * 0.08f, lb2 - shw * 0.08f, -1);
                Run(lipHi, iR, p => p.Y >= ecy + lb2 * 0.64f); Run(lipHi, iL, p => p.Y >= ecy + lb2 * 0.64f);
            }
            g.Restore(stClip2);
        }

        // ── subpom: gallet pla i quadrat, sota el pom; el gruix (cara del davant) segueix l'arrodonit ──
        static void DrawSub(Graphics g, Geo q, float gBottom)
        {
            float sc = SubScale, w = q.Gw * 0.78f * sc, top = gBottom - q.Gh * 0.25f * sc, bot = gBottom + q.Gh * 0.22f * sc;
            float h = bot - top, cy = (top + bot) / 2 + SubDy;
            var r = new RectangleF(q.Cx - w / 2, cy - h / 2, w, h);
            float rad = Math.Min(w, h) / 2 * SubRound, band = q.Gh * 0.07f * sc;
            var rt = new RectangleF(r.X, r.Y, r.Width, h - band); float radT = Math.Min(rad, rt.Height / 2);
            using var outer = Round(r, rad);
            using (var b = new LinearGradientBrush(new RectangleF(r.X - 0.01f, 0, r.Width + 0.02f, 1), Color.Black, Color.Black, 0f)
            { InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(2, 2, 3), Color.FromArgb(8, 9, 11), Color.FromArgb(6, 7, 9), Color.FromArgb(1, 1, 2) }, Positions = new[] { 0f, 0.35f, 0.7f, 1f } } })
                g.FillPath(b, outer);
            var st = g.Save(); g.SetClip(outer, CombineMode.Intersect);
            using (var tp = Round(rt, radT))
            {
                using (var b = new LinearGradientBrush(new RectangleF(r.X - 0.01f, 0, r.Width + 0.02f, 1), Color.Black, Color.Black, 0f)
                { InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(10, 11, 13), Color.FromArgb(30, 33, 40), Color.FromArgb(24, 26, 32), Color.FromArgb(8, 8, 10) }, Positions = new[] { 0f, 0.35f, 0.7f, 1f } } })
                    g.FillPath(b, tp);
                g.SetClip(RectangleF.FromLTRB(r.X - 1, rt.Bottom - Math.Max(radT, 1f), r.Right + 1, r.Bottom + 1), CombineMode.Intersect);
                using var lp = new Pen(Color.FromArgb(90, 120, 135, 160), 0.5f); g.DrawPath(lp, tp);
            }
            g.Restore(st);
            using (var pe = new Pen(Color.FromArgb(230, 2, 2, 3), 0.6f)) g.DrawPath(pe, outer);
        }

        // ── pom: forma arrodonida (cap costat pla), retallat des de dalt; reflexos fixos com el pom sencer ──
        static PointF[] Squircle(float x0, float y0, float x1, float y1, float bh)
        {
            float ax = (x1 - x0) / 2, ay = bh / 2, mx = x0 + ax, my = (y0 + y1) / 2, d = ((y1 - y0) - bh) / 2;
            var pts = new PointF[96];
            for (int i = 0; i < 96; i++)
            {
                double a = i * 2 * Math.PI / 96, co = Math.Cos(a), si = Math.Sin(a);
                float u = ay * (float)(Math.Sign(si) * Math.Pow(Math.Abs(si), 2 / 3.0));
                float u2 = Math.Sign(u) * Math.Max(0, Math.Abs(u) + d);
                pts[i] = new PointF(mx + ax * (float)(Math.Sign(co) * Math.Pow(Math.Abs(co), 2 / 3.0)), my + u2);
            }
            return pts;
        }
        static void DrawPom(Graphics g, Geo q, float gBottom)
        {
            float GH0 = q.Gh, gh = GH0 * PomCut, x0 = q.Cx - q.Gw / 2, x1 = q.Cx + q.Gw / 2, y1 = gBottom, y0 = y1 - gh;
            float fh = GH0 * 0.10f, faceTop = y1 - fh;
            var whole = Squircle(x0, y0, x1, y1, GH0);
            using (var b = new LinearGradientBrush(new RectangleF(x0, faceTop - 0.01f, q.Gw, fh + 0.02f), Color.FromArgb(28, 30, 36), Color.FromArgb(4, 4, 6), 90f)) g.FillPolygon(b, whole);
            using (var b = new LinearGradientBrush(new RectangleF(x0 - 0.01f, 0, q.Gw + 0.02f, 1), Color.Black, Color.Black, 0f)
            { InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(120, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(140, 0, 0, 0) }, Positions = new[] { 0f, 0.25f, 0.72f, 1f } } })
                g.FillPolygon(b, whole);
            using var pe = new Pen(Color.FromArgb(230, 2, 2, 3), 0.6f) { LineJoin = LineJoin.Round };
            g.DrawPolygon(pe, whole);
            var top = Squircle(x0, y0, x1, faceTop, GH0 - fh);
            using (var b = new LinearGradientBrush(new RectangleF(x0 - 0.01f, 0, q.Gw + 0.02f, 1), Color.Black, Color.Black, 0f)
            { InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(8, 9, 12), Color.FromArgb(26, 30, 40), Color.FromArgb(40, 46, 60), Color.FromArgb(22, 25, 33), Color.FromArgb(6, 6, 8) }, Positions = new[] { 0f, 0.3f, 0.5f, 0.74f, 1f } } })
                g.FillPolygon(b, top);
            float hy0 = y1 - GH0;   // reflex calculat sobre el pom sencer (no canvia en retallar)
            using (var b = new LinearGradientBrush(new RectangleF(x0, hy0 - 0.01f, q.Gw, faceTop - hy0 + 0.02f), Color.FromArgb(60, 120, 140, 170), Color.FromArgb(0, 120, 140, 170), 90f))
                g.FillPolygon(b, top);
            g.DrawPolygon(pe, top);
            var low = new PointF[33]; Array.Copy(top, 8, low, 0, 33);
            using var ap = new Pen(Color.FromArgb(120, 140, 160, 185), 0.5f); g.DrawLines(ap, low);
        }
    }
}
