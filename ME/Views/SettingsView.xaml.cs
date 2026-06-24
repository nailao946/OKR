using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ME.Data;
using ME.Models;
using ME.Services;

namespace ME.Views
{
    public partial class SettingsView : System.Windows.Controls.UserControl
    {
        private readonly BackupService _backupService;
        private readonly SettingsRepository _settingsRepo;

        private static readonly List<ColorBallDef> ColorBalls = new List<ColorBallDef>
        {
            new ColorBallDef("#007AFF", "默认蓝"),
            new ColorBallDef("#34C759", "森林绿"),
            new ColorBallDef("#FF3B30", "珊瑚红"),
            new ColorBallDef("#FF9500", "琥珀橙"),
            new ColorBallDef("#5856D6", "靛蓝紫"),
            new ColorBallDef("CUSTOM", "自定义"),
        };

        public SettingsView()
        {
            InitializeComponent();
            _backupService = new BackupService();
            _settingsRepo = new SettingsRepository();
            BackupModePartial.IsChecked = true;
            UpdateBackupPanelVisibility();
            BuildColorBalls();
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void BuildColorBalls()
        {
            ColorBallsPanel.Children.Clear();
            foreach (var def in ColorBalls)
            {
                var ball = new Border
                {
                    Width = 30, Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = Cursors.Hand,
                    Tag = def.Color,
                    ToolTip = def.Name,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(2),
                };

                if (def.Color == "CUSTOM")
                {
                    ball.Background = (SolidColorBrush)FindResource("CardBrush");
                    ball.Child = new TextBlock
                    {
                        Text = "+",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = (SolidColorBrush)FindResource("PrimaryBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                }
                else
                {
                    ball.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(def.Color));
                }

                ball.MouseLeftButtonDown += ColorBall_Click;
                ColorBallsPanel.Children.Add(ball);
            }
            UpdateColorBallSelection();
        }

        private void UpdateColorBallSelection()
        {
            var currentColor = _settingsRepo.GetValue(SettingsKeys.WindowBorderColor, "#007AFF");
            var isPreset = ColorBalls.Any(b => b.Color == currentColor);
            foreach (var child in ColorBallsPanel.Children)
            {
                if (child is Border ball)
                {
                    var ballColor = ball.Tag as string;
                    bool isSelected;
                    if (ballColor == "CUSTOM")
                        isSelected = !isPreset;
                    else
                        isSelected = ballColor == currentColor;
                    ball.BorderBrush = isSelected
                        ? (SolidColorBrush)FindResource("PrimaryBrush")
                        : Brushes.Transparent;
                    ball.BorderThickness = isSelected ? new Thickness(3) : new Thickness(2);

                    if (ballColor == "CUSTOM" && !isPreset)
                    {
                        try
                        {
                            ball.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(currentColor));
                            if (ball.Child is TextBlock tb) tb.Text = "";
                        }
                        catch { }
                    }
                    else if (ballColor == "CUSTOM")
                    {
                        ball.Background = (SolidColorBrush)FindResource("CardBrush");
                        if (ball.Child is TextBlock tb) tb.Text = "+";
                    }
                }
            }
        }

        private void ColorBall_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border ball)
            {
                var color = ball.Tag as string;
                if (color == "CUSTOM")
                {
                    ShowCustomColorDialog();
                    return;
                }
                _settingsRepo.SetValue(SettingsKeys.WindowBorderColor, color);
                ApplyWindowBorderColor(color);
                UpdateColorBallSelection();
            }
        }

        private void ShowCustomColorDialog()
        {
            var colorDialog = new System.Windows.Forms.ColorDialog
            {
                FullOpen = true,
                AnyColor = true,
                SolidColorOnly = false
            };

            var currentColor = _settingsRepo.GetValue(SettingsKeys.WindowBorderColor, "#007AFF");
            try
            {
                var clr = (Color)ColorConverter.ConvertFromString(currentColor);
                colorDialog.Color = System.Drawing.Color.FromArgb(clr.R, clr.G, clr.B);
            }
            catch { }

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = colorDialog.Color;
                var hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                _settingsRepo.SetValue(SettingsKeys.WindowBorderColor, hex);
                ApplyWindowBorderColor(hex);
                UpdateColorBallSelection();
            }
        }

        private void ApplyWindowBorderColor(string colorStr)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    var border = mainWindow.FindName("WindowBorder") as System.Windows.Controls.Border;
                    if (border != null)
                    {
                        border.BorderThickness = new Thickness(1);
                        border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
                    }
                }
            }
            catch { }
        }

        private void LoadSettings()
        {
            var theme = _settingsRepo.GetValue(SettingsKeys.Theme, "Light");
            foreach (ComboBoxItem item in ThemeCombo.Items)
            {
                if (item.Tag?.ToString() == theme)
                {
                    ThemeCombo.SelectedItem = item;
                    break;
                }
            }

            var borderColor = _settingsRepo.GetValue(SettingsKeys.WindowBorderColor, "#007AFF");
            UpdateColorBallSelection();

            AutoStartToggle.IsChecked = _settingsRepo.GetValue(SettingsKeys.AutoStart, "False") == "True";
            MinimizeToTrayToggle.IsChecked = _settingsRepo.GetValue(SettingsKeys.MinimizeToTray, "False") == "True";
            TrayBalloonToggle.IsChecked = _settingsRepo.GetValue(SettingsKeys.TrayBalloonEnabled, "True") == "True";
            SoundToggle.IsChecked = SoundService.IsEnabled();
            FocusSoundToggle.IsChecked = _settingsRepo.GetValue(SettingsKeys.FocusSoundEnabled, "True") == "True";
            LastBackupText.Text = _settingsRepo.GetValue(SettingsKeys.LastBackupDate, "");
        }

        private void ThemeCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeCombo.SelectedItem is ComboBoxItem item)
            {
                var theme = item.Tag?.ToString() ?? "Light";
                ThemeService.ApplyTheme(theme);
            }
        }

        private void AutoStartToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isEnabled = AutoStartToggle.IsChecked == true;
            _settingsRepo.SetValue(SettingsKeys.AutoStart, isEnabled.ToString());
            SetAutoStart(isEnabled);
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enable)
                        key.SetValue("GoalMap", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    else
                        key.DeleteValue("GoalMap", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置开机启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TrayToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isEnabled = MinimizeToTrayToggle.IsChecked == true;
            _settingsRepo.SetValue(SettingsKeys.MinimizeToTray, isEnabled.ToString());
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.SetTrayVisible(isEnabled);
        }

        private void TrayBalloon_Changed(object sender, RoutedEventArgs e)
        {
            var isEnabled = TrayBalloonToggle.IsChecked == true;
            _settingsRepo.SetValue(SettingsKeys.TrayBalloonEnabled, isEnabled.ToString());
        }

        private void SoundToggle_Changed(object sender, RoutedEventArgs e)
        {
            SoundService.SetEnabled(SoundToggle.IsChecked == true);
            _settingsRepo.SetValue(SettingsKeys.SoundEnabled, (SoundToggle.IsChecked == true).ToString());
        }

        private void FocusSound_Changed(object sender, RoutedEventArgs e)
        {
            _settingsRepo.SetValue(SettingsKeys.FocusSoundEnabled, (FocusSoundToggle.IsChecked == true).ToString());
        }

        private void BackupMode_Changed(object sender, RoutedEventArgs e)
        {
            UpdateBackupPanelVisibility();
        }

        private void UpdateBackupPanelVisibility()
        {
            if (PartialBackupPanel != null)
                PartialBackupPanel.Visibility = BackupModePartial.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            if (BackupModeFull.IsChecked == true)
                BackupAllData();
            else
                BackupPartialData();
        }

        private void BackupAllData()
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    FileName = $"me_full_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                if (dlg.ShowDialog() == true)
                {
                    var dataDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ME", "JsonData");
                    var merged = new Dictionary<string, object>();
                    if (Directory.Exists(dataDir))
                    {
                        foreach (var file in Directory.GetFiles(dataDir, "*.json"))
                        {
                            var json = File.ReadAllText(file);
                            var doc = JsonDocument.Parse(json);
                            var key = Path.GetFileNameWithoutExtension(file);
                            merged[key] = doc.RootElement;
                        }
                    }
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(merged, options));
                    _settingsRepo.SetValue(SettingsKeys.LastBackupDate, $"上次备份: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    LastBackupText.Text = $"上次备份: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    MessageBox.Show("备份成功!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupPartialData()
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    FileName = $"me_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                if (dlg.ShowDialog() == true)
                {
                    var dataDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ME", "JsonData");
                    var selectedFiles = new List<string>();
                    if (BackupGoals.IsChecked == true) selectedFiles.Add("goals.json");
                    if (BackupTasks.IsChecked == true) selectedFiles.Add("tasks.json");
                    if (BackupVisions.IsChecked == true) selectedFiles.Add("visions.json");
                    if (BackupReviews.IsChecked == true) selectedFiles.Add("reviews.json");
                    if (BackupFocus.IsChecked == true) selectedFiles.Add("focus_sessions.json");
                    if (BackupSettings.IsChecked == true) selectedFiles.Add("settings.json");
                    if (BackupTags.IsChecked == true) selectedFiles.Add("tags.json");
                    if (BackupTimeRecords.IsChecked == true) selectedFiles.Add("time_records.json");
                    if (BackupTimeTags.IsChecked == true) selectedFiles.Add("time_tags.json");

                    var merged = new Dictionary<string, object>();
                    foreach (var file in selectedFiles)
                    {
                        var path = Path.Combine(dataDir, file);
                        if (File.Exists(path))
                        {
                            var json = File.ReadAllText(path);
                            var doc = JsonDocument.Parse(json);
                            var key = Path.GetFileNameWithoutExtension(file);
                            merged[key] = doc.RootElement;
                        }
                    }
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(merged, options));
                    _settingsRepo.SetValue(SettingsKeys.LastBackupDate, $"上次备份: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    LastBackupText.Text = $"上次备份: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    MessageBox.Show("备份成功!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json"
                };
                if (dlg.ShowDialog() == true)
                {
                    var json = File.ReadAllText(dlg.FileName);
                    var doc = JsonDocument.Parse(json);
                    var dataDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ME", "JsonData");
                    Directory.CreateDirectory(dataDir);

                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            var filePath = Path.Combine(dataDir, prop.Name + ".json");
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            File.WriteAllText(filePath, JsonSerializer.Serialize(prop.Value, options));
                        }
                    }
                    else
                    {
                        File.Copy(dlg.FileName, Path.Combine(dataDir, Path.GetFileName(dlg.FileName)), true);
                    }

                    MessageBox.Show("导入成功! 请重启应用以加载数据。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class ColorBallDef
        {
            public string Color { get; set; }
            public string Name { get; set; }
            public ColorBallDef(string c, string n) { Color = c; Name = n; }
        }
    }
}
