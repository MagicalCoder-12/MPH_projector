using System.Runtime.InteropServices;

namespace ChurchProjector;

public sealed class ProjectorForm : Form
{
    private readonly SlideCanvas _canvas;
    private readonly Label _exitHint;
    private Screen? _targetScreen;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr HwndTopMost = new(-1);

    public ProjectorForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        KeyPreview = true;
        StartPosition = FormStartPosition.Manual;
        Shown += (_, _) => FitToScreen();
        DpiChanged += (_, _) => FitToScreen();
        _canvas = new SlideCanvas { Dock = DockStyle.Fill, BackColor = Color.Black };
        _exitHint = new Label
        {
            Text = "Press Esc to close projector",
            AutoSize = true,
            ForeColor = Color.FromArgb(180, Color.White),
            BackColor = Color.FromArgb(80, Color.Black),
            Font = new Font("Segoe UI", 11F),
            Padding = new Padding(8, 5, 8, 5),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Visible = false
        };
        Controls.Add(_canvas);
        Controls.Add(_exitHint);
        Resize += (_, _) => _exitHint.Location = new Point(ClientSize.Width - _exitHint.Width - 18, 18);
        MouseMove += (_, _) => { _exitHint.Visible = true; _exitHint.BringToFront(); };
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    public Screen? TargetScreen
    {
        set
        {
            _targetScreen = value;
            FitToScreen();
        }
    }

    private void FitToScreen()
    {
        if (_targetScreen is null || !IsHandleCreated) return;
        var bounds = _targetScreen.Bounds;
        SetWindowPos(Handle, HwndTopMost, bounds.X, bounds.Y, bounds.Width, bounds.Height, SwpNoActivate | SwpShowWindow);
    }

    public void SetSlide(string text, PresentationTheme theme)
    {
        _canvas.Theme = theme;
        _canvas.SlideText = text;
        _canvas.Invalidate();
    }
}
