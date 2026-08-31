using System.Runtime.InteropServices;

namespace ChurchProjector;

public sealed class ProjectorForm : Form
{
    private readonly SlideCanvas _canvas;
    private readonly TransitionOverlay _transitionOverlay;
    private readonly Label _exitHint;
    private readonly System.Windows.Forms.Timer _transitionTimer = new() { Interval = 16 };
    private Screen? _targetScreen;
    private string _text = "";
    private PresentationTheme? _theme;
    private StageMode _stage = StageMode.Slide;
    private Image? _logo;

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
        _transitionOverlay = new TransitionOverlay { Dock = DockStyle.Fill, Alpha = 0 };
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
        Controls.Add(_transitionOverlay);
        Controls.Add(_exitHint);
        _transitionTimer.Tick += (_, _) =>
        {
            _transitionOverlay.Alpha = Math.Max(0, _transitionOverlay.Alpha - 24);
            if (_transitionOverlay.Alpha == 0) _transitionTimer.Stop();
        };
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
        var shouldTransition = !string.IsNullOrEmpty(_text) && !string.Equals(_text, text, StringComparison.Ordinal);
        _text = text;
        _theme = theme;
        Render();
        if (shouldTransition) PlayTransition(theme.SlideTransition);
    }

    public void SetStage(StageMode stage, Image? logo)
    {
        _stage = stage;
        _logo = logo;
        Render();
    }

    private void Render()
    {
        if (_theme is null) return;
        _canvas.Stage = _stage;
        _canvas.LogoImage = _logo;
        _canvas.Theme = _theme;
        _canvas.SlideText = _text;
        _canvas.Invalidate();
    }

    private void PlayTransition(string transition)
    {
        _transitionTimer.Stop();
        if (string.Equals(transition, "None", StringComparison.OrdinalIgnoreCase))
        {
            _transitionOverlay.Alpha = 0;
            return;
        }
        _transitionOverlay.Alpha = string.Equals(transition, "Cross fade", StringComparison.OrdinalIgnoreCase) ? 120 : 80;
        _transitionOverlay.BringToFront();
        _exitHint.BringToFront();
        _transitionTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _transitionTimer.Dispose();
        base.Dispose(disposing);
    }

    private sealed class TransitionOverlay : Control
    {
        private int _alpha;

        public int Alpha
        {
            get => _alpha;
            set { _alpha = Math.Clamp(value, 0, 255); Invalidate(); }
        }

        public TransitionOverlay()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_alpha == 0) return;
            using var brush = new SolidBrush(Color.FromArgb(_alpha, Color.Black));
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }
}
