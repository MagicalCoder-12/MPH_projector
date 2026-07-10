namespace ChurchProjector;

public partial class Form1
{
    private void BuildInterface()
    {
        SuspendLayout();
        Text = "MPH Songs";
        MinimumSize = new Size(1120, 720);
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(241, 244, 247);
        Font = new Font("Segoe UI", 9F);

        var header = BuildHeader();
        var tabBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = _darkBrand, Padding = new Padding(10, 5, 0, 5) };
        var backgroundTab = TabButton("▧  Background", false);
        backgroundTab.Dock = DockStyle.Left;
        var bibleTab = TabButton("Bible", false);
        bibleTab.Dock = DockStyle.Left;
        var textTab = TabButton("A  Text", true);
        textTab.Dock = DockStyle.Left;
        tabBar.Controls.Add(bibleTab);
        tabBar.Controls.Add(backgroundTab);
        tabBar.Controls.Add(textTab);

        var ribbonHost = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Color.White, Padding = new Padding(10, 8, 10, 8) };
        var textRibbon = BuildTextRibbon();
        var backgroundRibbon = BuildBackgroundRibbon();
        var bibleRibbon = BuildBibleRibbon();
        backgroundRibbon.Visible = false;
        bibleRibbon.Visible = false;
        ribbonHost.Controls.Add(textRibbon);
        ribbonHost.Controls.Add(backgroundRibbon);
        ribbonHost.Controls.Add(bibleRibbon);
        _songWorkspace = BuildWorkspace();
        _bibleWorkspace = BuildBibleWorkspace();
        _bibleWorkspace.Visible = false;
        var workspaceHost = new Panel { Dock = DockStyle.Fill };
        workspaceHost.Controls.Add(_songWorkspace);
        workspaceHost.Controls.Add(_bibleWorkspace);
        textTab.Click += (_, _) => { RebuildSlides(); SwitchMode(textRibbon, _songWorkspace, textTab, backgroundTab, bibleTab); };
        backgroundTab.Click += (_, _) => { RebuildSlides(); SwitchMode(backgroundRibbon, _songWorkspace, backgroundTab, textTab, bibleTab); };
        bibleTab.Click += (_, _) => SwitchMode(bibleRibbon, _bibleWorkspace, bibleTab, textTab, backgroundTab);

        Controls.Add(workspaceHost);
        Controls.Add(ribbonHost);
        Controls.Add(tabBar);
        Controls.Add(header);
        ResumeLayout();
    }

    private void SwitchMode(Control ribbon, Control workspace, Button active, params Button[] inactive)
    {
        foreach (Control control in ribbon.Parent!.Controls) control.Visible = control == ribbon;
        foreach (Control control in workspace.Parent!.Controls) control.Visible = control == workspace;
        active.BackColor = Color.White;
        active.ForeColor = _darkBrand;
        foreach (var button in inactive)
        {
            button.BackColor = _darkBrand;
            button.ForeColor = Color.White;
        }
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = _brand };
        header.Controls.Add(new Label { Text = "✦", Font = new Font("Segoe UI Symbol", 22F, FontStyle.Bold), ForeColor = Color.FromArgb(255, 210, 64), Location = new Point(14, 7), AutoSize = true });
        header.Controls.Add(new Label { Text = "MPH SONGS", ForeColor = Color.White, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(50, 12), AutoSize = true });
        header.Controls.Add(new Label { Text = "Sunday service · Ready", ForeColor = Color.FromArgb(213, 235, 251), Location = new Point(232, 15), AutoSize = true });
        var projector = Button("▣  Open projector", Color.FromArgb(11, 77, 132), Color.White, 154, 31);
        projector.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        projector.Click += (_, _) => ToggleProjector();
        header.Resize += (_, _) => projector.Location = new Point(header.ClientSize.Width - projector.Width - 16, 8);
        header.Controls.Add(projector);
        return header;
    }

    private Control BuildTextRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        var font = RibbonGroup("Font", 285);
        _fontFamily = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 135 };
        _fontFamily.Items.AddRange(["Segoe UI", "Arial", "Calibri", "Georgia", "Verdana", "Trebuchet MS"]);
        _fontFamily.SelectedItem = _theme.FontFamily;
        _fontFamily.SelectedIndexChanged += (_, _) => { _theme.FontFamily = _fontFamily.Text; RefreshSlides(); };
        _fontSize = new NumericUpDown { Minimum = 18, Maximum = 130, Value = 56, Width = 53 };
        _fontSize.ValueChanged += (_, _) => { _theme.FontSize = (float)_fontSize.Value; RefreshSlides(); };
        _boldButton = Button("B", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _boldButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _boldButton.Click += (_, _) => { _theme.Bold = !_theme.Bold; UpdateBoldButton(); RefreshSlides(); };
        _fontColorButton = Button("A", _theme.TextColor, Color.White, 36, 29);
        _fontColorButton.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _fontColorButton.Click += (_, _) => ChooseTextColor();
        Add(font, _fontFamily, _fontSize, _boldButton, _fontColorButton);

        var layout = RibbonGroup("Slide layout", 290);
        _alignment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 105 };
        _alignment.Items.AddRange(["Left", "Centre", "Right"]);
        _alignment.SelectedItem = "Centre";
        _alignment.SelectedIndexChanged += (_, _) => { _theme.Alignment = _alignment.Text; RefreshSlides(); };
        _maxLines = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 4, Width = 48 };
        _maxLines.ValueChanged += (_, _) => RebuildSlides();
        Add(layout, Field("Align", _alignment), Field("Max. lines", _maxLines), Hint("Blank lines start a new slide"));

        var style = RibbonGroup("Quick style", 270);
        Add(style,
            Preset("Light text", Color.White, Color.FromArgb(28, 34, 45), () => SetTextStyle(Color.White, true)),
            Preset("Warm text", Color.FromArgb(255, 239, 171), Color.FromArgb(71, 48, 40), () => SetTextStyle(Color.FromArgb(255, 239, 171), true)),
            Preset("Dark text", Color.FromArgb(28, 40, 52), Color.FromArgb(232, 240, 245), () => SetTextStyle(Color.FromArgb(28, 40, 52), false)));
        ribbon.Controls.AddRange([font, layout, style]);
        return ribbon;
    }

    private Control BuildBackgroundRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        var colour = RibbonGroup("Colour", 330);
        foreach (var color in new[]
                 {
                     Color.FromArgb(22, 34, 52), Color.FromArgb(12, 79, 105), Color.FromArgb(74, 33, 95),
                     Color.FromArgb(104, 55, 36), Color.FromArgb(49, 80, 58), Color.FromArgb(238, 240, 243)
                 })
        {
            var swatch = Button("", color, Color.White, 36, 36);
            swatch.Click += (_, _) => { _theme.BackgroundColor = color; _theme.BackgroundImage = null; RefreshSlides(); };
            Add(colour, swatch);
        }
        var more = Button("More…", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 56, 36);
        more.Click += (_, _) => ChooseBackgroundColor();
        Add(colour, more);

        var image = RibbonGroup("Image", 250);
        var choose = Button("Choose image…", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 105, 34);
        choose.Click += (_, _) => ChooseBackgroundImage();
        var remove = Button("Remove image", Color.White, Color.FromArgb(31, 48, 68), 96, 34);
        remove.Click += (_, _) => { _theme.BackgroundImage?.Dispose(); _theme.BackgroundImage = null; RefreshSlides(); };
        Add(image, choose, remove, Hint("Images crop to fill each slide"));

        var brightness = RibbonGroup("Brightness", 180);
        _brightness = new TrackBar { Minimum = -75, Maximum = 75, Value = 0, TickFrequency = 25, Width = 145, Height = 38 };
        _brightness.ValueChanged += (_, _) => { _theme.Brightness = _brightness.Value; RefreshSlides(); };
        Add(brightness, _brightness);

        var ratio = RibbonGroup("Aspect ratio", 200);
        foreach (var value in new[] { "16:9", "4:3", "16:10" })
        {
            var choice = Button(value, value == "16:9" ? _brand : Color.White, value == "16:9" ? Color.White : Color.FromArgb(31, 48, 68), 52, 32);
            choice.Click += (_, _) => SetAspectRatio(value, ratio);
            Add(ratio, choice);
        }
        ribbon.Controls.AddRange([colour, image, brightness, ratio]);
        return ribbon;
    }

    private void SetAspectRatio(string value, Panel group)
    {
        _theme.AspectRatio = value;
        foreach (Control control in ((FlowLayoutPanel)group.Tag!).Controls.OfType<Button>())
        {
            var selected = control.Text == value;
            control.BackColor = selected ? _brand : Color.White;
            control.ForeColor = selected ? Color.White : Color.FromArgb(31, 48, 68);
        }
        RefreshSlides();
    }
}
