using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace Sentinel.Services;

public sealed class TrayNotificationService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    public event EventHandler? OpenRequested;
    public event EventHandler? ScanDownloadsRequested;
    public event EventHandler? QuitRequested;

    public TrayNotificationService()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sentinel.ico");
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Sentinel",
            Visible = true,
            Icon = File.Exists(iconPath) ? new Icon(iconPath) : BuildIcon(),
            ContextMenuStrip = BuildContextMenu()
        };
        _notifyIcon.ContextMenuStrip.Items.Add("Open Sentinel", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
        _notifyIcon.ContextMenuStrip.Items.Add("Scan Downloads", null, (_, _) => ScanDownloadsRequested?.Invoke(this, EventArgs.Empty));
        _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add("Quit", null, (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty));
        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public void NotifySuspiciousDownload(string fileName, string severity, int score, string explanation)
    {
        _notifyIcon.BalloonTipTitle = $"Sentinel detected a {severity.ToLowerInvariant()} download";
        _notifyIcon.BalloonTipText = $"{fileName}\nThreat score: {score}/100\n{explanation}";
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Warning;
        _notifyIcon.ShowBalloonTip(10000);
    }

    private static Forms.ContextMenuStrip BuildContextMenu()
    {
        var menu = new Forms.ContextMenuStrip
        {
            BackColor = Color.FromArgb(24, 27, 35),
            ForeColor = Color.FromArgb(232, 232, 232),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Padding = new Forms.Padding(6),
            Renderer = new SentinelMenuRenderer()
        };
        menu.Opening += (_, _) =>
        {
            foreach (Forms.ToolStripItem item in menu.Items)
            {
                item.BackColor = Color.FromArgb(24, 27, 35);
                item.ForeColor = Color.FromArgb(232, 232, 232);
                item.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
                item.Margin = new Forms.Padding(2);
                item.Padding = new Forms.Padding(8, 5, 18, 5);
            }
        };
        return menu;
    }

    private static Icon BuildIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.FromArgb(20, 20, 20));
        using var shieldBrush = new SolidBrush(Color.FromArgb(12, 32, 66));
        using var white = new SolidBrush(Color.FromArgb(232, 232, 232));
        using var bluePen = new Pen(Color.FromArgb(26, 174, 255), 4);
        using var whitePen = new Pen(Color.FromArgb(232, 232, 232), 3);
        var shield = new System.Drawing.Point[]
        {
            new(32, 5), new(55, 13), new(54, 30), new(49, 43),
            new(41, 53), new(32, 60), new(23, 53), new(15, 43),
            new(10, 30), new(9, 13)
        };
        graphics.FillPolygon(shieldBrush, shield);
        graphics.DrawPolygon(new Pen(Color.FromArgb(8, 18, 35), 2), shield);
        graphics.DrawBezier(whitePen, 15, 28, 22, 18, 42, 18, 49, 28);
        graphics.DrawBezier(whitePen, 49, 28, 42, 38, 22, 38, 15, 28);
        graphics.FillEllipse(white, 29, 25, 6, 6);
        graphics.DrawArc(bluePen, 27, 31, 20, 20, 20, 75);
        graphics.DrawArc(bluePen, 23, 38, 28, 28, 18, 68);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private sealed class SentinelMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        public SentinelMenuRenderer() : base(new SentinelColorTable())
        {
        }

        protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(42, 51, 66));
            var rect = new System.Drawing.Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            e.Graphics.DrawRectangle(pen, rect);
        }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            var selected = e.Item.Selected || e.Item.Pressed;
            using var brush = new SolidBrush(selected ? Color.FromArgb(45, 52, 68) : Color.FromArgb(24, 27, 35));
            e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(0, 0, e.Item.Width, e.Item.Height));
        }

        protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(42, 51, 66));
            e.Graphics.DrawLine(pen, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2);
        }
    }

    private sealed class SentinelColorTable : Forms.ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(24, 27, 35);
        public override Color ImageMarginGradientBegin => Color.FromArgb(24, 27, 35);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(24, 27, 35);
        public override Color ImageMarginGradientEnd => Color.FromArgb(24, 27, 35);
        public override Color MenuBorder => Color.FromArgb(42, 51, 66);
        public override Color MenuItemBorder => Color.FromArgb(68, 77, 95);
        public override Color MenuItemSelected => Color.FromArgb(45, 52, 68);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(45, 52, 68);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(45, 52, 68);
    }
}
