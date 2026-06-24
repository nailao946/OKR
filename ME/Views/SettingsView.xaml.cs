using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using ME.Data;
using ME.Models;
using ME.Services;

namespace ME.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly BackupService _backupService;
        private readonly SettingsRepository _settingsRepo;

        public SettingsView()
        {
            InitializeComponent();
            _backupService = new BackupService();
            _settingsRepo = new SettingsRepository();
            BackupModePartial.IsChecked = true;
            UpdateBackupPanelVisibility();
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var theme = _settingsRepo.GetValue(SettingsKeys.Theme, "Light");
            foreach (ComboBoxItem item in ThemeCombo.Items)
            {
                if ((string)item.Tag == theme)
                {
                    ThemeCombo.SelectedItem = item;
                    break;
                }
            }

            var borderColor = _settingsRepo.GetValue(SettingsKeys.WindowBorderColor, "#007AFF");
            foreach (ComboBoxItem item in BorderColorCombo.Items)
            {
                if ((string)item.Tag == borderColor)
                {
                    BorderColorCombo.SelectedItem = item;
                    break;
                }
            }
            UpdateBorderColorPreview(borderColor);

            AutoStartToggle.IsChecked = _settingsRepo.GetValue(SettingsKeys.AutoStart, "False") == "True";
            MinimizeToTrayToggle.IsChecked = _settingsRepo.GetValue(SettingsKeys.MinimizeToTray, "False") == "True";
            TrayBalloonToggle.IsChecked = _settingsRepo.GetValue(SettingsKeys.TrayBalloonEnabled, "True") == "True";
            SoundToggle.IsChecked = SoundService.IsEnabled();
            FocusSoundToggle.IsChecked = _settingsRepo.GetValue(SettingsKeys.FocusSoundEnabled, "True") == "True";

            var lastBackup = _settingsRepo.GetValue(SettingsKeys.LastBackupDate, "");
            LastBackupText.Text = string.IsNullOrEmpty(lastBackup) ? "" : $"上次备份: {lastBackup}";
        }

        private void ThemeCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeCombo.SelectedItem is ComboBoxItem item)
            {
                var theme = (string)item.Tag;
                ThemeService.ApplyTheme(theme);
            }
        }

        private void BorderColor_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (BorderColorCombo.SelectedItem is ComboBoxItem item)
            {
                var color = (string)item.Tag;
                _settingsRepo.SetValue(SettingsKeys.WindowBorderColor, color);
                UpdateBorderColorPreview(color);
            }
        }

        private void UpdateBorderColorPreview(string color)
        {
            try
            {
                BorderColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
            catch { }
        }

        private void AutoStartToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isEnabled = AutoStartToggle.IsChecked == true;
            _settingsRepo.SetValue(SettingsKeys.AutoStart, isEnabled ? "True" : "False");
            SetAutoStart(isEnabled);
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;
                var appName = "GoalMap";
                var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (enable) key.SetValue(appName, appPath);
                else key.DeleteValue(appName, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置开机自启失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TrayToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isEnabled = MinimizeToTrayToggle.IsChecked == true;
            _settingsRepo.SetValue(SettingsKeys.MinimizeToTray, isEnabled ? "True" : "False");
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.SetTrayVisible(isEnabled);
        }

        private void TrayBalloon_Changed(object sender, RoutedEventArgs e)
        {
            var isEnabled = TrayBalloonToggle.IsChecked == true;
            _settingsRepo.SetValue(SettingsKeys.TrayBalloonEnabled, isEnabled ? "True" : "False");
        }

        private void SoundToggle_Changed(object sender, RoutedEventArgs e)
        {
            SoundService.SetEnabled(SoundToggle.IsChecked == true);
        }

        private void FocusSound_Changed(object sender, RoutedEventArgs e)
        {
            var isEnabled = FocusSoundToggle.IsChecked == true;
            _settingsRepo.SetValue(SettingsKeys.FocusSoundEnabled, isEnabled ? "True" : "False");
        }

        private void BackupMode_Changed(object sender, RoutedEventArgs e)
        {
            UpdateBackupPanelVisibility();
        }

        private void UpdateBackupPanelVisibility()
        {
            if (PartialBackupPanel != null)
            {
                PartialBackupPanel.Visibility = BackupModePartial.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            if (BackupModeFull.IsChecked == true) BackupAllData();
            else BackupPartialData();
        }

        private void BackupAllData()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON备份文件 (*.json)|*.json",
                FileName = $"me_full_backup_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var sourceDir = Path.Combine(appData, "ME", "JsonData");

                    if (Directory.Exists(sourceDir))
                    {
                        var allData = new Dictionary<string, object>();
                        foreach (var file in Directory.GetFiles(sourceDir, "*.json"))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file);
                            var json = File.ReadAllText(file);
                            var data = JsonSerializer.Deserialize<JsonElement>(json);
                            allData[fileName] = data;
                        }

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        var fullJson = JsonSerializer.Serialize(allData, options);
                        File.WriteAllText(dialog.FileName, fullJson);

                        _settingsRepo.SetValue(SettingsKeys.LastBackupDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        LastBackupText.Text = $"上次备份: {DateTime.Now:yyyy-MM-dd HH:mm}";
                        MessageBox.Show("全部备份成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("没有数据可备份", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BackupPartialData()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON备份文件 (*.json)|*.json",
                FileName = $"me_backup_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var sourceDir = Path.Combine(appData, "ME", "JsonData");

                    if (Directory.Exists(sourceDir))
                    {
                        var backupDir = Path.GetDirectoryName(dialog.FileName);
                        if (!Directory.Exists(backupDir))
                            Directory.CreateDirectory(backupDir);

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

                        foreach (var fileName in selectedFiles)
                        {
                            var sourceFile = Path.Combine(sourceDir, fileName);
                            if (File.Exists(sourceFile))
                            {
                                var destFile = Path.Combine(backupDir, fileName);
                                File.Copy(sourceFile, destFile, true);
                            }
                        }

                        _settingsRepo.SetValue(SettingsKeys.LastBackupDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        LastBackupText.Text = $"上次备份: {DateTime.Now:yyyy-MM-dd HH:mm}";
                        MessageBox.Show("备份成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("没有数据可备份", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON备份文件 (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var targetDir = Path.Combine(appData, "ME", "JsonData");

                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    var selectedFile = dialog.FileName;
                    var json = File.ReadAllText(selectedFile);

                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var hasMultipleKeys = false;
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                hasMultipleKeys = true;
                                break;
                            }
                        }

                        if (hasMultipleKeys)
                        {
                            var allData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                            if (allData != null)
                            {
                                foreach (var kvp in allData)
                                {
                                    var filePath = Path.Combine(targetDir, $"{kvp.Key}.json");
                                    var options = new JsonSerializerOptions { WriteIndented = true };
                                    var dataJson = JsonSerializer.Serialize(kvp.Value, options);
                                    File.WriteAllText(filePath, dataJson);
                                }
                            }
                        }
                        else
                        {
                            var fileName = Path.GetFileNameWithoutExtension(selectedFile);
                            var destFile = Path.Combine(targetDir, $"{fileName}.json");
                            File.Copy(selectedFile, destFile, true);
                        }
                    }

                    MessageBox.Show("导入成功！请重启应用使数据生效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
