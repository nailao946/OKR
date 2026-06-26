using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ME.Data;
using ME.Models;
using ME.Services;
using ME.ViewModels;
using ME.Views;
using Forms = System.Windows.Forms;

namespace ME
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm;
        private GoalsView _goalsView;
        private TasksView _tasksView;
        private CalendarView _calendarView;
        private MapView _mapView;
        private DashboardView _dashboardView;
        private ReviewView _reviewView;
        private RecycleBinView _recycleBinView;
        private SettingsView _settingsView;
        private TimeTrackView _timeTrackView;
        private UserControl _currentView;
        private Forms.NotifyIcon _notifyIcon;
        private bool _isDarkTheme;
        private FloatingWindow _floatingWindow;
        private Forms.ToolStripMenuItem _floatingMenuItem;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainWindowViewModel();
            DataContext = _vm;

            _isDarkTheme = ThemeService.IsDarkMode();
            UpdateThemeButton();
            SetupTrayIcon();
            ApplyWindowBorderColor();
            InitFloatingWindow();

            ThemeService.ThemeChanged += (theme) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _isDarkTheme = ThemeService.IsDarkMode();
                    UpdateThemeButton();
                    ApplyWindowBorderColor();
                    RebuildTrayMenu();
                });
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ThemeService.Initialize();
            UpdateView(0);
        }

        private void UpdateThemeButton()
        {
            ThemeToggleBtn.Content = _isDarkTheme ? "☀️" : "🌙";
        }

        private void ApplyWindowBorderColor()
        {
            try
            {
                var settingsRepo = new SettingsRepository();
                var colorStr = settingsRepo.GetValue(SettingsKeys.WindowBorderColor, "#007AFF");
                if (WindowBorder != null)
                {
                    if (colorStr == "NONE")
                    {
                        WindowBorder.BorderThickness = new Thickness(0);
                    }
                    else
                    {
                        WindowBorder.BorderThickness = new Thickness(1);
                        WindowBorder.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr));
                    }
                }
            }
            catch { }
        }

        // ========== CUSTOM CHROME ==========
        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            var newTheme = _isDarkTheme ? "Light" : "Dark";
            ThemeService.ApplyTheme(newTheme);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                Hide();
                var settingsRepo = new SettingsRepository();
                var showBalloon = settingsRepo.GetValue(SettingsKeys.TrayBalloonEnabled, "True");
                if (showBalloon == "True")
                {
                    _notifyIcon.ShowBalloonTip(2000, "目标地图", "已最小化到系统托盘", Forms.ToolTipIcon.Info);
                }
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            MaximizeBtn.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Maximize_Click(sender, e);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Maximize_Click(sender, e);
                return;
            }
            DragMove();
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
        }

        // ========== TRAY ICON ==========
        private void SetupTrayIcon()
        {
            try
            {
                _notifyIcon = new Forms.NotifyIcon();
                _notifyIcon.Text = "目标地图";

                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hobby_working_dailyroutine_life_time_management_icon_142245.ico");
                if (File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    try
                    {
                        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    }
                    catch
                    {
                        _notifyIcon.Icon = SystemIcons.Application;
                    }
                }

                RebuildTrayMenu();

                _notifyIcon.DoubleClick += (s, ev) => { Show(); WindowState = WindowState.Normal; Activate(); };

                var settingsRepo = new SettingsRepository();
                var minimizeToTray = settingsRepo.GetValue(SettingsKeys.MinimizeToTray, "False");
                _notifyIcon.Visible = minimizeToTray == "True";
            }
            catch
            {
            }
        }

        private void RebuildTrayMenu()
        {
            var isDark = ThemeService.IsDarkMode();
            var menu = new Forms.ContextMenuStrip();
            menu.Renderer = new ToolStripThemeRenderer(isDark);
            menu.Padding = new System.Windows.Forms.Padding(4);
            menu.Font = new System.Drawing.Font("Segoe UI", 10f);

            var showItem = new Forms.ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, ev) => { Show(); WindowState = WindowState.Normal; Activate(); };
            menu.Items.Add(showItem);

            _floatingMenuItem = new Forms.ToolStripMenuItem("显示悬浮窗");
            _floatingMenuItem.Click += (s, ev) => ToggleFloatingWindow();
            menu.Items.Add(_floatingMenuItem);

            menu.Items.Add(new Forms.ToolStripSeparator());

            var exitItem = new Forms.ToolStripMenuItem("退出");
            exitItem.Click += (s, ev) => { _notifyIcon.Visible = false; CloseFloatingWindowPermanent(); Application.Current.Shutdown(); };
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        public void SetTrayVisible(bool visible)
        {
            if (_notifyIcon != null)
                _notifyIcon.Visible = visible;
        }

        // ========== FLOATING WINDOW ==========
        private void InitFloatingWindow()
        {
            var settingsRepo = new SettingsRepository();
            var enabled = settingsRepo.GetValue(SettingsKeys.FloatingWindowEnabled, "False");
            if (enabled == "True")
            {
                ShowFloatingWindow();
            }
        }

        public void ShowFloatingWindow()
        {
            if (_floatingWindow == null)
            {
                _floatingWindow = new FloatingWindow();
                _floatingWindow.Closed += (s, ev) => _floatingWindow = null;
            }
            _floatingWindow.Show();
            if (_floatingMenuItem != null)
                _floatingMenuItem.Text = "隐藏悬浮窗";
        }

        public void HideFloatingWindow()
        {
            _floatingWindow?.Hide();
            if (_floatingMenuItem != null)
                _floatingMenuItem.Text = "显示悬浮窗";
        }

        public void ToggleFloatingWindow()
        {
            if (_floatingWindow != null && _floatingWindow.IsVisible)
            {
                HideFloatingWindow();
            }
            else
            {
                ShowFloatingWindow();
            }
        }

        private void CloseFloatingWindowPermanent()
        {
            if (_floatingWindow != null)
            {
                _floatingWindow.ClosePermanent();
                _floatingWindow = null;
            }
        }

        // ========== NAVIGATION ==========
        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedIndex >= 0)
            {
                UpdateView(NavList.SelectedIndex);
            }
        }

        private void UpdateView(int index)
        {
            if (_currentView != null)
                _currentView.Visibility = Visibility.Collapsed;

            switch (index)
            {
                case 0: ShowView(ref _tasksView, () => new TasksView(), "任务列表"); break;
                case 1: ShowView(ref _goalsView, () => new GoalsView(), "目标管理"); break;
                case 2: ShowView(ref _calendarView, () => new CalendarView(), "日历视图"); break;
                case 3: ShowView(ref _mapView, () => new MapView(), "目标地图"); break;
                case 4: ShowView(ref _dashboardView, () => new DashboardView(), "数据看板"); break;
                case 5: ShowView(ref _reviewView, () => new ReviewView(), "定期盘点"); break;
                case 6: ShowView(ref _timeTrackView, () => new TimeTrackView(), "时间追踪"); break;
                case 7: ShowView(ref _settingsView, () => new SettingsView(), "设置"); break;
            }
        }

        private void ShowView<T>(ref T view, Func<T> create, string title) where T : UserControl
        {
            if (view == null)
            {
                view = create();
                view.Visibility = Visibility.Collapsed;
                view.Opacity = 0;
                ContentGrid.Children.Add(view);
            }

            if (_currentView != null && _currentView != view)
            {
                var oldView = _currentView;
                // Slide out + fade out
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                var slideOut = new DoubleAnimation(0, -12, TimeSpan.FromSeconds(0.15))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                var oldTransform = oldView.RenderTransform as TranslateTransform ?? new TranslateTransform();
                oldView.RenderTransform = oldTransform;

                fadeOut.Completed += (s, e) =>
                {
                    oldView.Visibility = Visibility.Collapsed;
                    oldView.Opacity = 1;
                    oldView.BeginAnimation(UIElement.OpacityProperty, null);
                    oldTransform.BeginAnimation(TranslateTransform.YProperty, null);
                    oldTransform.Y = 0;
                };
                oldView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                oldTransform.BeginAnimation(TranslateTransform.YProperty, slideOut);
            }

            // Slide in + fade in
            view.Visibility = Visibility.Visible;
            view.Opacity = 0;
            var transform = view.RenderTransform as TranslateTransform ?? new TranslateTransform();
            view.RenderTransform = transform;
            transform.Y = 16;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var slideIn = new DoubleAnimation(16, 0, TimeSpan.FromSeconds(0.25))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            view.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            transform.BeginAnimation(TranslateTransform.YProperty, slideIn);

            _currentView = view;
            TitleText.Text = title;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            ApplyWindowBorderColor();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            WindowBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
        }

        protected override void OnClosed(EventArgs e)
        {
            SharedTimerService.StopCurrent();
            CloseFloatingWindowPermanent();
            _notifyIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
