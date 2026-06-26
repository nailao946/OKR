using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ME.Services
{
    public class ToolStripThemeRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _bgColor;
        private readonly Color _textColor;
        private readonly Color _hoverColor;
        private readonly Color _separatorColor;
        private readonly Color _borderColor;

        public ToolStripThemeRenderer(bool isDark)
            : base(new ThemeColorTable(isDark))
        {
            if (isDark)
            {
                _bgColor = Color.FromArgb(44, 44, 46);
                _textColor = Color.FromArgb(242, 242, 247);
                _hoverColor = Color.FromArgb(0, 122, 255);
                _separatorColor = Color.FromArgb(58, 58, 60);
                _borderColor = Color.FromArgb(58, 58, 60);
            }
            else
            {
                _bgColor = Color.FromArgb(255, 255, 255);
                _textColor = Color.FromArgb(28, 28, 30);
                _hoverColor = Color.FromArgb(0, 122, 255);
                _separatorColor = Color.FromArgb(229, 229, 234);
                _borderColor = Color.FromArgb(229, 229, 234);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_bgColor))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(_borderColor))
            {
                var rect = e.AffectedBounds;
                rect.Width -= 1;
                rect.Height -= 1;
                using (var path = GetRoundedRect(rect, 8))
                    e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(Point.Empty, e.Item.Size);
            if (e.Item.Selected)
            {
                using (var brush = new SolidBrush(_hoverColor))
                {
                    using (var path = GetRoundedRect(rect, 4))
                        e.Graphics.FillPath(brush, path);
                }
            }
            else
            {
                using (var brush = new SolidBrush(_bgColor))
                    e.Graphics.FillRectangle(brush, rect);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Color.White : _textColor;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var rect = new Rectangle(Point.Empty, e.Item.Size);
            int y = rect.Height / 2;
            using (var pen = new Pen(_separatorColor, 1))
            {
                e.Graphics.DrawLine(pen, 16, y, rect.Width - 16, y);
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = _textColor;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_bgColor))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        private static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class ThemeColorTable : ProfessionalColorTable
    {
        private readonly bool _isDark;

        public ThemeColorTable(bool isDark) => _isDark = isDark;

        public override Color ToolStripDropDownBackground => _isDark
            ? Color.FromArgb(44, 44, 46) : Color.FromArgb(255, 255, 255);

        public override Color ImageMarginGradientBegin => _isDark
            ? Color.FromArgb(44, 44, 46) : Color.FromArgb(255, 255, 255);

        public override Color ImageMarginGradientMiddle => _isDark
            ? Color.FromArgb(44, 44, 46) : Color.FromArgb(255, 255, 255);

        public override Color ImageMarginGradientEnd => _isDark
            ? Color.FromArgb(44, 44, 46) : Color.FromArgb(255, 255, 255);

        public override Color MenuBorder => _isDark
            ? Color.FromArgb(58, 58, 60) : Color.FromArgb(229, 229, 234);

        public override Color MenuItemBorder => _isDark
            ? Color.FromArgb(58, 58, 60) : Color.FromArgb(229, 229, 234);

        public override Color MenuItemSelected => _isDark
            ? Color.FromArgb(0, 122, 255) : Color.FromArgb(0, 122, 255);

        public override Color MenuItemSelectedGradientBegin => _isDark
            ? Color.FromArgb(0, 122, 255) : Color.FromArgb(0, 122, 255);

        public override Color MenuItemSelectedGradientEnd => _isDark
            ? Color.FromArgb(0, 122, 255) : Color.FromArgb(0, 122, 255);

        public override Color MenuItemPressedGradientBegin => _isDark
            ? Color.FromArgb(44, 44, 46) : Color.FromArgb(255, 255, 255);

        public override Color MenuItemPressedGradientEnd => _isDark
            ? Color.FromArgb(44, 44, 46) : Color.FromArgb(255, 255, 255);

        public override Color MenuStripGradientBegin => _isDark
            ? Color.FromArgb(44, 44, 46) : Color.FromArgb(255, 255, 255);

        public override Color MenuStripGradientEnd => _isDark
            ? Color.FromArgb(44, 44, 46) : Color.FromArgb(255, 255, 255);

        public override Color SeparatorDark => _isDark
            ? Color.FromArgb(58, 58, 60) : Color.FromArgb(229, 229, 234);

        public override Color SeparatorLight => _isDark
            ? Color.FromArgb(58, 58, 60) : Color.FromArgb(229, 229, 234);

        public override Color CheckBackground => _isDark
            ? Color.FromArgb(0, 122, 255) : Color.FromArgb(0, 122, 255);

        public override Color CheckSelectedBackground => _isDark
            ? Color.FromArgb(0, 122, 255) : Color.FromArgb(0, 122, 255);

        public override Color CheckPressedBackground => _isDark
            ? Color.FromArgb(0, 100, 220) : Color.FromArgb(0, 100, 220);
    }
}
