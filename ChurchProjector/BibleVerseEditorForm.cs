namespace ChurchProjector;

/// <summary>
/// Modal editor used to create or update a single Bible verse.
/// </summary>
public sealed class BibleVerseEditorForm : Form
{
    private static readonly Color Brand = Color.FromArgb(22, 113, 180);
    private static readonly Color Muted = Color.FromArgb(86, 99, 114);

    private readonly ComboBox _book = new()
    {
        DropDownStyle = ComboBoxStyle.DropDown,
        Width = 180
    };
    private readonly NumericUpDown _chapter = new() { Minimum = 1, Maximum = 200, Width = 64 };
    private readonly NumericUpDown _verse = new() { Minimum = 1, Maximum = 200, Width = 64 };
    private readonly RichTextBox _text = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 11F)
    };

    public string Book => _book.Text.Trim();
    public int Chapter => (int)_chapter.Value;
    public int VerseNumber => (int)_verse.Value;
    public string VerseText => _text.Text.Trim();

    public BibleVerseEditorForm(string translationName, BibleVerse? existing = null)
    {
        Text = existing is null ? "New Bible verse" : "Edit Bible verse";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 360);
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.White;

        // Translation banner
        var banner = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.FromArgb(232, 242, 251) };
        banner.Controls.Add(new Label
        {
            Text = "Translation:  " + translationName,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(52, 94, 130),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(6, 0, 0, 0)
        });

        // Book / Chapter / Verse row
        var fields = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 64,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };
        _book.Items.AddRange(BibleBooks.Canonical);
        _book.DropDownWidth = 220;
        fields.Controls.Add(Labeled("Book", _book, 196));
        fields.Controls.Add(Labeled("Chapter", _chapter, 92));
        fields.Controls.Add(Labeled("Verse", _verse, 92));

        // Buttons
        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width = 84,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 6, 0),
            UseVisualStyleBackColor = false
        };
        var save = new Button
        {
            Text = "Save verse",
            Width = 96,
            Height = 32,
            BackColor = Brand,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        save.Click += (_, _) =>
        {
            if (!ValidateInput()) return;
            DialogResult = DialogResult.OK;
            Close();
        };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 12, 0)
        };
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);

        // Main content
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(14, 0, 14, 8),
            BackColor = Color.White
        };
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        container.Controls.Add(banner, 0, 0);
        container.Controls.Add(fields, 0, 1);
        container.Controls.Add(_text, 0, 2);

        Controls.Add(container);
        Controls.Add(actions);
        AcceptButton = save;
        CancelButton = cancel;

        if (existing is not null)
        {
            _book.Text = existing.Book;
            _chapter.Value = Math.Clamp(existing.Chapter, 1, 200);
            _verse.Value = Math.Clamp(existing.Verse, 1, 200);
            _text.Text = existing.Text;
            _text.SelectAll();
        }
        else if (BibleBooks.Canonical.Length > 0)
        {
            _book.Text = BibleBooks.Canonical[0];
        }

        Shown += (_, _) => _book.Focus();
    }

    private static FlowLayoutPanel Labeled(string caption, Control input, int width)
    {
        var panel = new FlowLayoutPanel
        {
            Width = width,
            Height = 58,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 8, 0)
        };
        panel.Controls.Add(new Label { Text = caption, ForeColor = Muted, AutoSize = true });
        panel.Controls.Add(input);
        return panel;
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Book))
        {
            MessageBox.Show(this, "Enter a book name.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            _book.Focus();
            return false;
        }
        if (string.IsNullOrWhiteSpace(VerseText))
        {
            MessageBox.Show(this, "Enter the verse text.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            _text.Focus();
            return false;
        }
        return true;
    }
}
