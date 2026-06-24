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
            var dialog = new Window
            {
                Title = "",
                Width = 320, Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = true,
            };
            dialog.Loaded += (s, ev) =>
            {
                // Force remove any system chrome
                var hwnd = new System.Windows.Interop.WindowInteropHelper(dialog).Handle;
                var style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style & ~NativeMethods.WS_SYSMENU);
            };

            var mainBorder = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = (SolidColorBrush)FindResource("BackgroundBrush"),
                BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1),
            };
            mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Opacity = 0.25, BlurRadius = 24, ShadowDepth = 2
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title bar
            var titleBar = new Border
            {
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Background = (SolidColorBrush)FindResource("CardBrush"),
                Padding = new Thickness(16, 0, 16, 0)
            };
            var titleGrid = new Grid();
            titleGrid.Children.Add(new TextBlock
            {
                Text = "选择颜色", FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("TextBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });
            var closeBtn = new Button
            {
                Content = "✕", Padding = new Thickness(8, 2, 8, 2), FontSize = 12,
                Style = (Style)FindResource("SecondaryButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtn.Click += (s, ev) => { dialog.DialogResult = false; };
            titleGrid.Children.Add(closeBtn);
            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // Color palette
            var contentPanel = new StackPanel { Margin = new Thickness(20, 12, 20, 0) };

            var selectedColor = "#FF5722";
            var previewBorder = new Border
            {
                Width = 48, Height = 48, CornerRadius = new CornerRadius(24),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedColor)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12),
                BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(2)
            };
            contentPanel.Children.Add(previewBorder);

            var paletteLabel = new TextBlock
            {
                Text = "选择颜色", FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush"),
                Margin = new Thickness(0, 0, 0, 8)
            };
            contentPanel.Children.Add(paletteLabel);

            var palette = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            var colors = new[]
            {
                "#FF3B30", "#FF9500", "#FFCC00", "#34C759", "#007AFF", "#5856D6",
                "#AF52DE", "#FF2D55", "#5AC8FA", "#A2845E", "#8E8E93", "#636366",
                "#FF6B6B", "#FFA94D", "#FFD93D", "#69DB7C", "#4DABF7", "#9775FA",
                "#F783AC", "#868E96", "#20C997", "#339AF0", "#E64980", "#845EF7",
                "#FF8787", "#FFC078", "#FFE066", "#8CE99A", "#74C0FC", "#B197FC",
                "#FAA2C1", "#ADB5BD", "#66D9E8", "#4DABF7", "#E599F7", "#FFD43B"
            };

            foreach (var c in colors)
            {
                var ball = new Border
                {
                    Width = 28, Height = 28, CornerRadius = new CornerRadius(14),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c)),
                    Margin = new Thickness(0, 0, 6, 6), Cursor = Cursors.Hand,
                    Tag = c,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(2)
                };
                ball.MouseLeftButtonDown += (s, ev) =>
                {
                    selectedColor = ball.Tag as string;
                    previewBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedColor));
                    foreach (var child in palette.Children)
                    {
                        if (child is Border b)
                        {
                            b.BorderBrush = b == ball
                                ? (SolidColorBrush)FindResource("PrimaryBrush")
                                : Brushes.Transparent;
                            b.BorderThickness = b == ball ? new Thickness(3) : new Thickness(2);
                        }
                    }
                };
                palette.Children.Add(ball);
            }
            contentPanel.Children.Add(palette);

            // Hex input
            var hexLabel = new TextBlock
            {
                Text = "或输入十六进制颜色", FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush"),
                Margin = new Thickness(0, 0, 0, 6)
            };
            contentPanel.Children.Add(hexLabel);

            var hexBox = new TextBox
            {
                Text = selectedColor, FontSize = 13,
                Style = (Style)FindResource("InputTextBoxStyle"),
                Height = 36, Margin = new Thickness(0, 0, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            hexBox.TextChanged += (s, ev) =>
            {
                try
                {
                    var clr = (Color)ColorConverter.ConvertFromString(hexBox.Text);
                    selectedColor = hexBox.Text;
                    previewBorder.Background = new SolidColorBrush(clr);
                }
                catch { }
            };
            contentPanel.Children.Add(hexBox);

            Grid.SetRow(contentPanel, 1);
            mainGrid.Children.Add(contentPanel);

            // Buttons
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 12, 20, 16)
            };
            var okBtn = new Button
            {
                Content = "确定", Padding = new Thickness(24, 8, 24, 8),
                Style = (Style)FindResource("PrimaryButtonStyle"), Margin = new Thickness(0, 0, 10, 0),
                FontSize = 13
            };
            var cancelBtn = new Button
            {
                Content = "取消", Padding = new Thickness(24, 8, 24, 8),
                Style = (Style)FindResource("SecondaryButtonStyle"), FontSize = 13
            };
            okBtn.Click += (s, ev) => { dialog.DialogResult = true; };
            cancelBtn.Click += (s, ev) => { dialog.DialogResult = false; };
            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            Grid.SetRow(btnPanel, 2);
            mainGrid.Children.Add(btnPanel);

            mainBorder.Child = mainGrid;
            var wrapper = new Grid();
            wrapper.Children.Add(mainBorder);
            dialog.Content = wrapper;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var clr = (Color)ColorConverter.ConvertFromString(selectedColor);
                    var hex = $"#{clr.R:X2}{clr.G:X2}{clr.B:X2}";
                    _settingsRepo.SetValue(SettingsKeys.WindowBorderColor, hex);
                    ApplyWindowBorderColor(hex);
                    UpdateColorBallSelection();
                }
                catch
                {
                    ConfirmDialog.Show(Window.GetWindow(this), "错误", "无效的颜色格式", "确定");
                }
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

    internal static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int WS_SYSMENU = 0x00080000;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
