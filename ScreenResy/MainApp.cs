using System.Diagnostics;

namespace ScreenResy
{
    public partial class MainApp : Form
    {
        private sealed record Choice<T>(string Text, T Value)
        {
            public override string ToString() => Text;
        }

        private IReadOnlyList<DisplayMode> modes = [];
        private bool loading;

        public MainApp()
        {
            InitializeComponent();

            Text = $"{AppInfo.Name} {AppInfo.Version}";
            labelAbout.Text = $"{AppInfo.Name} {AppInfo.Version}  ·  {AppInfo.Copyright.Replace("(C)", "©")}";
            linkAuthor.Text = AppInfo.AuthorUrl;

            LoadDisplays(selectDeviceName: null);
        }

        private Display? SelectedDisplay => (comboDisplay.SelectedItem as Choice<Display>)?.Value;

        /// <summary>Fills the display list, then the settings of the selected display.</summary>
        private void LoadDisplays(string? selectDeviceName)
        {
            loading = true;
            try
            {
                comboDisplay.Items.Clear();
                IReadOnlyList<Display> displays = Display.GetAll();
                foreach (Display display in displays)
                    comboDisplay.Items.Add(new Choice<Display>(display.ToString(), display));

                Display selected = displays.FirstOrDefault(d => d.DeviceName == selectDeviceName)
                    ?? displays.FirstOrDefault(d => d.IsPrimary)
                    ?? displays[0];
                Select(comboDisplay, selected);
            }
            catch (DisplayException ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                loading = false;
            }

            LoadSettings();
        }

        /// <summary>Shows what the selected display supports, with its current settings selected.</summary>
        private void LoadSettings()
        {
            Display? display = SelectedDisplay;
            comboResolution.Items.Clear();
            comboRefresh.Items.Clear();
            comboScale.Items.Clear();
            labelCurrent.Text = "";
            buttonApply.Enabled = false;
            if (display is null)
                return;

            loading = true;
            try
            {
                DisplayMode current = display.GetCurrentMode();
                modes = display.GetModes();
                string summary = $"Current:  {current.Width} × {current.Height},  {current.Frequency} Hz";

                foreach (var (width, height) in modes.Select(m => (m.Width, m.Height)).Distinct())
                    comboResolution.Items.Add(new Choice<Size>($"{width} × {height}", new Size(width, height)));
                Select(comboResolution, new Size(current.Width, current.Height));
                LoadRefreshRates(current.Frequency);

                // Scale comes from an undocumented part of Windows, so the rest must work without it.
                try
                {
                    ScaleInfo scale = display.GetScale();
                    foreach (int percent in scale.Supported)
                        comboScale.Items.Add(new Choice<int>(percent == scale.Recommended ? $"{percent}% (recommended)" : $"{percent}%", percent));
                    Select(comboScale, scale.Current);
                    summary += $",  {scale.Current}%";
                }
                catch (DisplayException)
                {
                }
                comboScale.Enabled = comboScale.Items.Count > 0;

                labelCurrent.Text = summary;
                buttonApply.Enabled = comboResolution.SelectedItem is not null;
            }
            catch (DisplayException ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                loading = false;
            }
        }

        /// <summary>Lists the refresh rates of the selected resolution, keeping the preferred one if it has it.</summary>
        private void LoadRefreshRates(int preferred)
        {
            comboRefresh.Items.Clear();
            if (comboResolution.SelectedItem is not Choice<Size> resolution)
                return;

            foreach (DisplayMode mode in modes.Where(m => m.Width == resolution.Value.Width && m.Height == resolution.Value.Height))
                comboRefresh.Items.Add(new Choice<int>($"{mode.Frequency} Hz", mode.Frequency));

            Select(comboRefresh, preferred);
            if (comboRefresh.SelectedItem is null && comboRefresh.Items.Count > 0)
                comboRefresh.SelectedIndex = 0; // the highest
        }

        private static void Select<T>(ComboBox combo, T value)
        {
            combo.SelectedItem = combo.Items.OfType<Choice<T>>().FirstOrDefault(c => EqualityComparer<T>.Default.Equals(c.Value, value));
        }

        private void comboDisplay_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!loading)
                LoadSettings();
        }

        private void comboResolution_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (loading)
                return;

            int preferred = (comboRefresh.SelectedItem as Choice<int>)?.Value ?? 0;
            LoadRefreshRates(preferred);
        }

        private void buttonApply_Click(object? sender, EventArgs e)
        {
            if (SelectedDisplay is not Display display
                || comboResolution.SelectedItem is not Choice<Size> resolution
                || comboRefresh.SelectedItem is not Choice<int> refresh)
                return;

            var mode = new DisplayMode(resolution.Value.Width, resolution.Value.Height, refresh.Value);
            int? scale = (comboScale.SelectedItem as Choice<int>)?.Value;

            try
            {
                Apply(display, mode, scale);
            }
            catch (DisplayException ex)
            {
                ShowError(ex.Message);
            }

            LoadDisplays(display.DeviceName);
        }

        private void Apply(Display display, DisplayMode mode, int? scale)
        {
            DisplayMode previousMode = display.GetCurrentMode();
            int? previousScale = scale is null ? null : display.GetScale().Current;
            bool changeMode = mode != previousMode;
            bool changeScale = scale != previousScale;

            if (!changeMode)
            {
                if (changeScale)
                    display.SetScale(scale!.Value);
                return;
            }

            // The new mode is applied without being saved. If the monitor can't show it, nobody can
            // confirm it, and restoring the saved mode puts the picture back.
            display.SetMode(mode.Width, mode.Height, mode.Frequency, persist: false);
            bool scaleChanged = false;
            bool keep = false;
            try
            {
                if (changeScale)
                {
                    display.SetScale(scale!.Value);
                    scaleChanged = true;
                }

                using var confirm = new KeepChangesDialog();
                keep = confirm.ShowDialog(this) == DialogResult.OK;
                if (keep)
                    display.SetMode(mode.Width, mode.Height, mode.Frequency, persist: true);
            }
            finally
            {
                if (!keep)
                {
                    display.RestoreSavedMode();
                    if (scaleChanged)
                        display.SetScale(previousScale!.Value);
                }
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void linkAuthor_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(AppInfo.AuthorUrl) { UseShellExecute = true });
        }

        private void buttonClose_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
