using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ME.ViewModels;
using ME.Services;
using System.Collections.Generic;
using System.Text.Json;

namespace ME.Views
{
    public partial class SettingsView : UserControl
    {
        public static event Action ThemeChanged;

        private readonly BackupService _backupService;
        private readonly SettingsViewModel _viewModel;

        public SettingsView()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;
            _backupService = new BackupService();
            SoundToggle.IsChecked = SoundService.IsEnabled();
            AutoStartToggle.IsChecked = _viewModel.AutoStart;
            BackupModePartial.IsChecked = true;
            UpdateBackupPanelVisibility();
        }

        private void SoundToggle_Changed(object sender, RoutedEventArgs e)
        {
            SoundService.SetEnabled(SoundToggle.IsChecked == true);
        }

        private void AutoStartToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isEnabled = AutoStartToggle.IsChecked == true;
            _viewModel.AutoStart = isEnabled;
            SetAutoStart(isEnabled);
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key == null) return;
                
                var appName = "GoalMap";
                var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                
                if (enable)
                {
                    key.SetValue(appName, appPath);
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"设置开机自启失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            if (BackupModeFull.IsChecked == true)
            {
                BackupAllData();
            }
            else
            {
                BackupPartialData();
            }
        }

        private void BackupAllData()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON备份文件 (*.json)|*.json",
                FileName = $"me_full_backup_{System.DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    var sourceDir = System.IO.Path.Combine(appData, "ME", "JsonData");

                    if (System.IO.Directory.Exists(sourceDir))
                    {
                        var allData = new Dictionary<string, object>();
                        
                        foreach (var file in System.IO.Directory.GetFiles(sourceDir, "*.json"))
                        {
                            var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                            var json = System.IO.File.ReadAllText(file);
                            var data = JsonSerializer.Deserialize<JsonElement>(json);
                            allData[fileName] = data;
                        }

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        var fullJson = JsonSerializer.Serialize(allData, options);
                        System.IO.File.WriteAllText(dialog.FileName, fullJson);
                        
                        MessageBox.Show("全部备份成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("没有数据可备份", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (System.Exception ex)
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
                FileName = $"me_backup_{System.DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    var sourceDir = System.IO.Path.Combine(appData, "ME", "JsonData");

                    if (System.IO.Directory.Exists(sourceDir))
                    {
                        var backupDir = System.IO.Path.GetDirectoryName(dialog.FileName);
                        if (!System.IO.Directory.Exists(backupDir))
                            System.IO.Directory.CreateDirectory(backupDir);

                        var selectedFiles = new List<string>();
                        if (BackupGoals.IsChecked == true) selectedFiles.Add("goals.json");
                        if (BackupTasks.IsChecked == true) selectedFiles.Add("tasks.json");
                        if (BackupVisions.IsChecked == true) selectedFiles.Add("visions.json");
                        if (BackupReviews.IsChecked == true) selectedFiles.Add("reviews.json");
                        if (BackupFocus.IsChecked == true) selectedFiles.Add("focus_sessions.json");
                        if (BackupSettings.IsChecked == true) selectedFiles.Add("settings.json");
                        if (BackupTags.IsChecked == true) selectedFiles.Add("tags.json");

                        foreach (var fileName in selectedFiles)
                        {
                            var sourceFile = System.IO.Path.Combine(sourceDir, fileName);
                            if (System.IO.File.Exists(sourceFile))
                            {
                                var destFile = System.IO.Path.Combine(backupDir, fileName);
                                System.IO.File.Copy(sourceFile, destFile, true);
                            }
                        }
                        
                        MessageBox.Show("备份成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("没有数据可备份", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (System.Exception ex)
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
                    var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    var targetDir = System.IO.Path.Combine(appData, "ME", "JsonData");

                    if (!System.IO.Directory.Exists(targetDir))
                        System.IO.Directory.CreateDirectory(targetDir);

                    var selectedFile = dialog.FileName;
                    var json = System.IO.File.ReadAllText(selectedFile);
                    
                    // Check if it's a full backup (contains multiple keys)
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
                            // Full backup restore
                            var allData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                            if (allData != null)
                            {
                                foreach (var kvp in allData)
                                {
                                    var filePath = System.IO.Path.Combine(targetDir, $"{kvp.Key}.json");
                                    var options = new JsonSerializerOptions { WriteIndented = true };
                                    var dataJson = JsonSerializer.Serialize(kvp.Value, options);
                                    System.IO.File.WriteAllText(filePath, dataJson);
                                }
                            }
                        }
                        else
                        {
                            // Single file restore
                            var fileName = System.IO.Path.GetFileNameWithoutExtension(selectedFile);
                            var destFile = System.IO.Path.Combine(targetDir, $"{fileName}.json");
                            System.IO.File.Copy(selectedFile, destFile, true);
                        }
                    }
                    
                    MessageBox.Show("导入成功！请重启应用使数据生效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}