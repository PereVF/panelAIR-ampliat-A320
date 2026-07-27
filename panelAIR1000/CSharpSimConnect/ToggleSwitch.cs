using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimConnect3
{
    /// <summary>Interruptor lliscant senzill (on/off), dibuixat a mà com la resta d'elements custom de l'app.</summary>
    public class ToggleSwitch : Control
    {
        private bool _checked;
        public bool Checked
        {
            get => _checked;
            set { if (_checked == value) return; _checked = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
        }

        public event EventHandler? CheckedChanged;

        public ToggleSwitch()
        {
            Size = new Size(54, 30);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var trackColor = _checked ? Color.FromArgb(235, 91, 37) : Color.FromArgb(150, 150, 150);
            using (var trackBrush = new SolidBrush(trackColor))
            using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2))
                g.FillPath(trackBrush, path);

            int thumbD = Height - 6;
            int thumbX = _checked ? Width - thumbD - 3 : 3;
            using var thumbBrush = new SolidBrush(Color.White);
            g.FillEllipse(thumbBrush, thumbX, 3, thumbD, thumbD);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
