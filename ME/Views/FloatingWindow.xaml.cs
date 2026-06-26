using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ME.Data;
using ME.Services;

namespace ME.Views
{
    public partial class FloatingWindow : Window
    {
        private readonly TimeTagRepository _tagRepo;
        private readonly SettingsRepository _settingsRepo;
        private bool _isClosingFromCode;

        public FloatingWindow()
        {
            InitializeComponent();
            _tagRepo = new TimeTagRepository();
            _settingsRepo = new SettingsRepository();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SharedTimerService.TimerUpdated += OnTimerUpdated;
            SharedTimerService.RunningStateChanged += OnRunningStateChanged;

            // Restore position
            var left = _settingsRepo.GetValue("FloatingWindowLeft", "");
            var top = _settingsRepo.GetValue("FloatingWindowTop", "");
            if (double.TryParse(left, out var l) && double.TryParse(top, out var t))
            {
                Left = l;
                Top = t;
            }
            else
            {
                Left = SystemParameters.PrimaryScreenWidth - Width - 20;
                Top = SystemParameters.PrimaryScreenHeight - Height - 100;
            }

            // Restore size
            var w = _settingsRepo.GetValue("FloatingWindowWidth", "");
            var h = _settingsRepo.GetValue("FloatingWindowHeight", "");
            if (double.TryParse(w, out var pw) && double.TryParse(h, out var ph))
            {
                Width = pw;
                Height = ph;
            }

            UpdateDisplay(SharedTimerService.IsRunning);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosingFromCode)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            SharedTimerService.TimerUpdated -= OnTimerUpdated;
            SharedTimerService.RunningStateChanged -= OnRunningStateChanged;
            SavePosition();
        }

        public void ClosePermanent()
        {
            _isClosingFromCode = true;
            Close();
        }

        public void SetSize(double width)
        {
            MaxWidth = width + 100;
            Width = width + 100;
        }

        private void SavePosition()
        {
            _settingsRepo.SetValue("FloatingWindowLeft", Left.ToString("F0"));
            _settingsRepo.SetValue("FloatingWindowTop", Top.ToString("F0"));
            _settingsRepo.SetValue("FloatingWindowWidth", Width.ToString("F0"));
            _settingsRepo.SetValue("FloatingWindowHeight", Height.ToString("F0"));
        }

        private void OnTimerUpdated(string timeStr, string tagName, string tagColor)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TimerText.Text = timeStr;
                TagNameText.Text = tagName;
                try
                {
                    TagDot.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tagColor));
                }
                catch { }
            }));
        }

        private void OnRunningStateChanged(bool isRunning)
        {
            Dispatcher.BeginInvoke(new Action(() => UpdateDisplay(isRunning)));
        }

        private void UpdateDisplay(bool isRunning)
        {
            if (isRunning)
            {
                var tag = _tagRepo.GetTagById(SharedTimerService.SelectedTagId);
                TagNameText.Text = tag?.Name ?? "计时中";
                TimerText.Text = "00:00:00";
                try
                {
                    TagDot.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(tag?.Color ?? "#808080"));
                }
                catch { }
            }
            else
            {
                TagNameText.Text = "未计时";
                TimerText.Text = "00:00:00";
                TagDot.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#808080"));
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ShowContextMenu();
        }

        private void ShowContextMenu()
        {
            var menu = new System.Windows.Controls.ContextMenu();
            try
            {
                menu.Background = (Brush)FindResource("CardBrush");
                menu.BorderBrush = (Brush)FindResource("BorderBrush");
            }
            catch { }

            // Current running tag (stop option)
            if (SharedTimerService.IsRunning)
            {
                var currentTag = _tagRepo.GetTagById(SharedTimerService.SelectedTagId);
                var stopItem = new System.Windows.Controls.MenuItem
                {
                    Header = $"■ 停止 [{currentTag?.Name ?? "未知"}]",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30"))
                };
                stopItem.Click += (s, ev) => SharedTimerService.StopCurrent();
                menu.Items.Add(stopItem);
                menu.Items.Add(new System.Windows.Controls.Separator());
            }

            // All tags
            var tags = _tagRepo.GetAllTags();
            Brush textBrush;
            try { textBrush = (Brush)FindResource("TextBrush"); }
            catch { textBrush = Brushes.White; }

            foreach (var tag in tags)
            {
                var isRunning = SharedTimerService.IsRunning && SharedTimerService.SelectedTagId == tag.Id;
                var item = new System.Windows.Controls.MenuItem
                {
                    Header = (isRunning ? "● " : "○ ") + tag.Name,
                    IsEnabled = !isRunning,
                    Foreground = textBrush
                };
                var tagId = tag.Id;
                item.Click += (s, ev) => SharedTimerService.StartWithTag(tagId);
                menu.Items.Add(item);
            }

            menu.Items.Add(new System.Windows.Controls.Separator());

            // Show main window
            var showMainItem = new System.Windows.Controls.MenuItem { Header = "显示主窗口", Foreground = textBrush };
            showMainItem.Click += (s, ev) =>
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                }
            };
            menu.Items.Add(showMainItem);

            // Close floating window
            var hideItem = new System.Windows.Controls.MenuItem { Header = "隐藏悬浮窗", Foreground = textBrush };
            hideItem.Click += (s, ev) => Hide();
            menu.Items.Add(hideItem);

            menu.PlacementTarget = this;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }
}
