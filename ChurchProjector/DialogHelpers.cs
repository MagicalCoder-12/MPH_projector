namespace ChurchProjector;

/// <summary>
/// Small, consistently styled prompts used across the application.
/// </summary>
public static class DialogHelpers
{
    private static readonly Color Brand = Color.FromArgb(22, 113, 180);
    private static readonly Color TextColour = Color.FromArgb(31, 48, 68);
    private static readonly Color Muted = Color.FromArgb(86, 99, 114);

    /// <summary>
    /// Shows a modal form with a single text input. Returns the trimmed value,
    /// or null when the user cancels.
    /// </summary>
    public static string? PromptText(IWin32Window owner, string title, string message, string initialValue = "")
    {
        using var dialog = new Form
        {
            Text = title,
            ClientSize = new Size(420, 150),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.White
        };
        var label = new Label { Text = message, AutoSize = true, Location = new Point(14, 12), ForeColor = TextColour };
        var input = new TextBox { Text = initialValue, Location = new Point(14, 38), Width = 376 };
        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(244, 84),
            Width = 70,
            Height = 30,
            BackColor = Brand,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(320, 84),
            Width = 70,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        dialog.Controls.AddRange([label, input, ok, cancel]);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        return dialog.ShowDialog(owner) == DialogResult.OK ? input.Text.Trim() : null;
    }

    /// <summary>
    /// Shows a yes/no confirmation. Returns true only when the user confirms.
    /// </summary>
    public static bool Confirm(IWin32Window owner, string title, string message, bool destructive = false)
    {
        var icon = destructive ? MessageBoxIcon.Warning : MessageBoxIcon.Question;
        var result = MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, icon);
        return result == DialogResult.Yes;
    }
}
