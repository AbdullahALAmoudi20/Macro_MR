using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MacroMR
{
    public partial class SettingsPage : Page
    {
        private string tempTriggerKey = string.Empty;
        private string tempKey1 = string.Empty;
        private string tempKey2 = string.Empty;
        private string tempKey3 = string.Empty;
        private string tempKey4 = string.Empty;
        private string tempKey5 = string.Empty;

        public SettingsPage()
        {
            InitializeComponent();
            RefreshSettings();
        }

        private void RefreshSettings()
        {
            tempTriggerKey = Properties.Settings.Default.MainKey;
            tempKey1 = Properties.Settings.Default.Key1;
            tempKey2 = Properties.Settings.Default.Key2;
            tempKey3 = Properties.Settings.Default.Key3;
            tempKey4 = Properties.Settings.Default.Key4;
            tempKey5 = Properties.Settings.Default.Key5;

            MacroSpeedSlider.Value = Properties.Settings.Default.MacroSpeed;
            SpeedValueText.Text = $"Speed: {MacroSpeedSlider.Value:0.0} ms";

            TriggerKeyButton.Content = string.IsNullOrWhiteSpace(tempTriggerKey) ? "Press a key" : tempTriggerKey;
            Key1Button.Content = string.IsNullOrWhiteSpace(tempKey1) ? "Press a key" : tempKey1;
            Key2Button.Content = string.IsNullOrWhiteSpace(tempKey2) ? "Press a key" : tempKey2;
            Key3Button.Content = string.IsNullOrWhiteSpace(tempKey3) ? "Press a key" : tempKey3;
            Key4Button.Content = string.IsNullOrWhiteSpace(tempKey4) ? "Press a key" : tempKey4;
            Key5Button.Content = string.IsNullOrWhiteSpace(tempKey5) ? "Press a key" : tempKey5;

            InfiniteMacroCheckbox.IsChecked = Properties.Settings.Default.InfiniteMacro;
            Key1Checkbox.IsChecked = Properties.Settings.Default.EnableKey1;
            Key2Checkbox.IsChecked = Properties.Settings.Default.EnableKey2;
            Key3Checkbox.IsChecked = Properties.Settings.Default.EnableKey3;
            Key4Checkbox.IsChecked = Properties.Settings.Default.EnableKey4;
            Key5Checkbox.IsChecked = Properties.Settings.Default.EnableKey5;
        }

        private void MacroSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedValueText != null)
                SpeedValueText.Text = $"Speed: {e.NewValue:0.0} ms";
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MainKey = tempTriggerKey;
            Properties.Settings.Default.Key1 = tempKey1;
            Properties.Settings.Default.Key2 = tempKey2;
            Properties.Settings.Default.Key3 = tempKey3;
            Properties.Settings.Default.Key4 = tempKey4;
            Properties.Settings.Default.Key5 = tempKey5;

            Properties.Settings.Default.MacroSpeed = MacroSpeedSlider.Value;
            Properties.Settings.Default.InfiniteMacro = InfiniteMacroCheckbox.IsChecked == true;
            Properties.Settings.Default.EnableKey1 = Key1Checkbox.IsChecked == true;
            Properties.Settings.Default.EnableKey2 = Key2Checkbox.IsChecked == true;
            Properties.Settings.Default.EnableKey3 = Key3Checkbox.IsChecked == true;
            Properties.Settings.Default.EnableKey4 = Key4Checkbox.IsChecked == true;
            Properties.Settings.Default.EnableKey5 = Key5Checkbox.IsChecked == true;

            Properties.Settings.Default.Save();

            // Notify the MainWindow to reload settings so they take effect immediately.
            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.ReloadSettings();
            }

            // Show temporary in-app message instead of MessageBox
            var originalForeground = SpeedValueText.Foreground;
            SpeedValueText.Text = "✅ Settings Saved!";
            SpeedValueText.Foreground = Brushes.LimeGreen;

            await Task.Delay(2500);

            SpeedValueText.Text = $"Speed: {MacroSpeedSlider.Value:0.0} ms";
            SpeedValueText.Foreground = originalForeground;
        }

        private void TriggerKeyButton_Click(object sender, RoutedEventArgs e) => CaptureKey(TriggerKeyButton, key => tempTriggerKey = key);
        private void Key1Button_Click(object sender, RoutedEventArgs e) => CaptureKey(Key1Button, key => tempKey1 = key);
        private void Key2Button_Click(object sender, RoutedEventArgs e) => CaptureKey(Key2Button, key => tempKey2 = key);
        private void Key3Button_Click(object sender, RoutedEventArgs e) => CaptureKey(Key3Button, key => tempKey3 = key);
        private void Key4Button_Click(object sender, RoutedEventArgs e) => CaptureKey(Key4Button, key => tempKey4 = key);
        private void Key5Button_Click(object sender, RoutedEventArgs e) => CaptureKey(Key5Button, key => tempKey5 = key);

        private void CaptureKey(Button targetButton, Action<string> saveKey)
        {
            targetButton.Content = "Press...";
            this.Focus();

            KeyEventHandler handler = null;
            handler = (s, e) =>
            {
                string key = e.Key.ToString().ToUpper();
                saveKey(key);
                targetButton.Content = key;
                this.KeyDown -= handler;
            };
            this.KeyDown += handler;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.MainFrame.Content = null;
                mw.HomePanel.Visibility = Visibility.Visible;
            }
        }
    }
}
