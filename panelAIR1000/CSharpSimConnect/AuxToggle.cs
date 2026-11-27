using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace panelAIR1000
{
    /// <summary>
    /// Interruptor lliscant horitzontal (menú AUX, OPTIONS): pista arrodonida blava quan està activat (gris quan no) i
    /// botó rodó blanc que llisca a la dreta / a l'esquerra. Un toc el canvia.
    /// </summary>
    internal sealed class AuxToggle : Control
    {
        bool _on;
        float _pos;   // 0 = esquerra (desactivat) … 1 = dreta (activat), animat
        readonly System.Windows.Forms.Timer _anim = new() { Interval = 15 };
        static readonly Color OnColor = Color.FromArgb(40, 130, 235), OffColor = Color.FromArgb(95, 100, 108);

        public event Action<bool>? Toggled;
        /// <summary>Si és false, els tocs no el canvien (p.ex. en mode calibratge).</summary>
        public Func<bool>? Enabled2;

        public bool On
        {
            get => _on;
            set { if (_on == value) return; _on = value; _anim.Start(); }
        }

        public AuxToggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            _anim.Tick += (s, e) =>
            {
                float t = _on ? 1f : 0f;
                _pos += Math.Sign(t - _pos) * 0.15f;
                if (Math.Abs(_pos - t) < 0.15f) { _pos = t; _anim.Stop(); }
                Invalidate();
            };
        }

        /// <summary>Estat inicial sense animació.</summary>
        public void SetInitial(bool on) { _on = on; _pos = on ? 1f : 0f; Invalidate(); }

        /// <summary>El mateix que un toc a l'interruptor (p.ex. un toc al text del costat).</summary>
        public void PerformClickToggle() => OnClick(EventArgs.Empty);

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (Enabled2 != null && !Enabled2()) return;
            On = !On;
            Toggled?.Invoke(_on);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float h = Height - 2, w = Width - 2;
            var track = new RectangleF(1, 1, w, h);
            using (var path = new GraphicsPath())
            {
                path.AddArc(track.X, track.Y, h, h, 90, 180);
                path.AddArc(track.Right - h, track.Y, h, h, 270, 180);
                path.CloseFigure();
                int r = (int)(OffColor.R + (OnColor.R - OffColor.R) * _pos), gg = (int)(OffColor.G + (OnColor.G - OffColor.G) * _pos), b = (int)(OffColor.B + (OnColor.B - OffColor.B) * _pos);
                using (var tb = new SolidBrush(Color.FromArgb(r, gg, b))) g.FillPath(tb, path);
            }
            float kd = h - 8, kx = track.X + 4 + (w - 8 - kd) * _pos;
            using (var sb = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillEllipse(sb, kx + 1, track.Y + 5, kd, kd);
            using (var kb = new SolidBrush(Color.White)) g.FillEllipse(kb, kx, track.Y + 4, kd, kd);
        }

        protected override void Dispose(bool disposing) { if (disposing) _anim.Dispose(); base.Dispose(disposing); }
    }
}
