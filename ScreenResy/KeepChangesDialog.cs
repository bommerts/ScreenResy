namespace ScreenResy;

/// <summary>
/// Asks whether to keep a new display mode and answers "no" by itself after a few seconds, so a
/// mode the monitor cannot show never leaves the user stuck with a blank screen.
/// DialogResult.OK means keep.
/// </summary>
internal sealed class KeepChangesDialog : Form
{
    private const int TimeoutSeconds = 15;

    private readonly Label message = new() { AutoSize = true, Margin = new Padding(3, 3, 3, 14) };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private int secondsLeft = TimeoutSeconds;

    public KeepChangesDialog()
    {
        // Listed first so it gets the focus: a stray Enter should undo the change, not confirm it.
        var revert = new Button { Text = "&Revert", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(96, 27) };
        var keep = new Button { Text = "&Keep changes", DialogResult = DialogResult.OK, AutoSize = true, MinimumSize = new Size(96, 27) };

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Right,
            Margin = Padding.Empty,
        };
        buttons.Controls.Add(revert);
        buttons.Controls.Add(keep);

        // Everything sizes itself to its text, so the dialog fits at any scale or font.
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
        };
        layout.Controls.Add(message, 0, 0);
        layout.Controls.Add(buttons, 0, 1);

        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);
        Controls.Add(layout);
        CancelButton = revert;
        ControlBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Keep these display settings?";
        TopMost = true;
        UpdateMessage();
        ResumeLayout(true);

        timer.Tick += (_, _) =>
        {
            if (--secondsLeft <= 0)
                DialogResult = DialogResult.Cancel;
            else
                UpdateMessage();
        };
        timer.Start();
    }

    private void UpdateMessage()
    {
        message.Text = $"Reverting to the previous display settings in {secondsLeft} second{(secondsLeft == 1 ? "" : "s")}.";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            timer.Dispose();
        base.Dispose(disposing);
    }
}
