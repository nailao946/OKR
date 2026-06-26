using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ME.Data;
using ME.Models;
using ME.Services;

namespace ME.Views
{
    public partial class FloatingWindow : Window
    {
        private readonly TimeTagRepository _tagRepo;
        private readonly SettingsRepository _settingsRepo;
        private readonly TaskRepository _taskRepo;
        private readonly TaskService _taskService;
        private bool _isClosingFromCode;
        private bool _isExpanded;

        // Drag state
        private bool _isDragging;
        private Point _dragStartPoint;
        private const double DragThreshold = 3.0;

        // Edge snap
        private const double SnapThreshold = 20.0;

        public FloatingWindow()
        {
            InitializeComponent();
            _tagRepo = new TimeTagRepository();
            _settingsRepo = new SettingsRepository();
            _taskRepo = new TaskRepository();
            _taskService = new TaskService();
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
                Left = SystemParameters.PrimaryScreenWidth - 200;
                Top = SystemParameters.PrimaryScreenHeight - 120;
            }

            UpdateDisplay(SharedTimerService.IsRunning);
            LoadTagChips();
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

        private void SavePosition()
        {
            _settingsRepo.SetValue("FloatingWindowLeft", Left.ToString("F0"));
            _settingsRepo.SetValue("FloatingWindowTop", Top.ToString("F0"));
        }

        // ─── Expand / Collapse ─────────────────────────────────────────

        private void ToggleExpand()
        {
            if (_isExpanded)
                Collapse();
            else
                Expand();
        }

        private void Expand()
        {
            _isExpanded = true;
            CollapsedPanel.Visibility = Visibility.Collapsed;
            ExpandedPanel.Visibility = Visibility.Visible;
            LoadTaskList();
            StartDotPulse();
        }

        private void Collapse()
        {
            _isExpanded = false;
            ExpandedPanel.Visibility = Visibility.Collapsed;
            CollapsedPanel.Visibility = Visibility.Visible;
        }

        // ─── Pill click → expand ───────────────────────────────────────

        private void Pill_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Begin drag tracking
            _isDragging = false;
            _dragStartPoint = e.GetPosition(this);
            MouseMove += Pill_MouseMove;
            MouseLeftButtonUp += Pill_MouseLeftButtonUp;
            CaptureMouse();
            e.Handled = true;
        }

        private void Pill_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            if (!_isDragging)
            {
                if (Math.Abs(pos.X - _dragStartPoint.X) > DragThreshold ||
                    Math.Abs(pos.Y - _dragStartPoint.Y) > DragThreshold)
                {
                    _isDragging = true;
                }
            }

            if (_isDragging)
            {
                var screenPos = PointToScreen(pos);
                Left = screenPos.X - _dragStartPoint.X;
                Top = screenPos.Y - _dragStartPoint.Y;
            }
        }

        private void Pill_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseMove -= Pill_MouseMove;
            MouseLeftButtonUp -= Pill_MouseLeftButtonUp;
            ReleaseMouseCapture();

            if (_isDragging)
            {
                _isDragging = false;
                SnapToEdge();
            }
            else
            {
                // It was a click, not a drag → expand
                ToggleExpand();
            }
        }

        // ─── Header click → collapse (from expanded state) ─────────────

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Collapse();
            e.Handled = true;
        }

        // ─── Edge snapping ─────────────────────────────────────────────

        private void SnapToEdge()
        {
            var screen = SystemParameters.WorkArea;
            var w = ActualWidth;
            var h = ActualHeight;

            if (Left < screen.Left + SnapThreshold)
                Left = screen.Left;
            else if (Left + w > screen.Right - SnapThreshold)
                Left = screen.Right - w;

            if (Top < screen.Top + SnapThreshold)
                Top = screen.Top;
            else if (Top + h > screen.Bottom - SnapThreshold)
                Top = screen.Bottom - h;
        }

        // ─── Right-click context menu ──────────────────────────────────

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ShowContextMenu();
        }

        private void ShowContextMenu()
        {
            var menu = new ContextMenu();
            try
            {
                menu.Background = (Brush)FindResource("CardBrush");
                menu.BorderBrush = (Brush)FindResource("BorderBrush");
            }
            catch { }

            Brush textBrush;
            try { textBrush = (Brush)FindResource("TextBrush"); }
            catch { textBrush = Brushes.White; }

            // Stop current
            if (SharedTimerService.IsRunning)
            {
                var currentTag = _tagRepo.GetTagById(SharedTimerService.SelectedTagId);
                var stopItem = new MenuItem
                {
                    Header = $"■ 停止 [{currentTag?.Name ?? "未知"}]",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30"))
                };
                stopItem.Click += (s, ev) => SharedTimerService.StopCurrent();
                menu.Items.Add(stopItem);
                menu.Items.Add(new Separator());
            }

            // Show main window
            var showItem = new MenuItem { Header = "显示主窗口", Foreground = textBrush };
            showItem.Click += (s, ev) =>
            {
                var main = Application.Current.MainWindow;
                if (main != null)
                {
                    main.Show();
                    main.WindowState = WindowState.Normal;
                    main.Activate();
                }
            };
            menu.Items.Add(showItem);

            // Hide
            var hideItem = new MenuItem { Header = "隐藏悬浮窗", Foreground = textBrush };
            hideItem.Click += (s, ev) => Hide();
            menu.Items.Add(hideItem);

            menu.PlacementTarget = this;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // ─── Timer events ──────────────────────────────────────────────

        private void OnTimerUpdated(string timeStr, string tagName, string tagColor)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TimerText.Text = timeStr;
                ExpTimerText.Text = timeStr;
                TagNameText.Text = tagName;
                ExpTagNameText.Text = tagName;
                SetTagDotColor(tagColor);
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
                var name = tag?.Name ?? "计时中";
                var color = tag?.Color ?? "#808080";
                TagNameText.Text = name;
                ExpTagNameText.Text = name;
                TimerText.Text = "00:00:00";
                ExpTimerText.Text = "00:00:00";
                SetTagDotColor(color);
            }
            else
            {
                TagNameText.Text = "未计时";
                ExpTagNameText.Text = "未计时";
                TimerText.Text = "00:00:00";
                ExpTimerText.Text = "00:00:00";
                SetTagDotColor("#808080");
            }
            if (_isExpanded) LoadTaskList();
            UpdateTagChipStates();
        }

        private void SetTagDotColor(string colorStr)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorStr);
                var brush = new SolidColorBrush(color);
                TagDot.Background = brush;
                ExpTagDot.Background = brush;
                TagDotGlow.Color = color;
            }
            catch { }
        }

        // ─── Dot pulse animation ───────────────────────────────────────

        private void StartDotPulse()
        {
            if (!SharedTimerService.IsRunning) return;
            var anim = new DoubleAnimation(0, 6, TimeSpan.FromSeconds(0.8))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            var opacityAnim = new DoubleAnimation(0.5, 0, TimeSpan.FromSeconds(0.8))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            TagDotGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, anim);
            TagDotGlow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
        }

        // ─── Task list ─────────────────────────────────────────────────

        private void LoadTaskList()
        {
            TaskListPanel.Children.Clear();

            var tasks = _taskRepo.GetTodayTasks();
            if (tasks.Count == 0)
            {
                TaskListPanel.Children.Add(new TextBlock
                {
                    Text = "今天没有待办任务",
                    FontSize = 12,
                    Foreground = (Brush)FindResource("SecondaryTextBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
                return;
            }

            foreach (var task in tasks)
            {
                var row = CreateTaskRow(task);
                TaskListPanel.Children.Add(row);
            }
        }

        private Border CreateTaskRow(TaskItem task)
        {
            Brush textBrush, secondaryBrush, cardBrush;
            try
            {
                textBrush = (Brush)FindResource("TextBrush");
                secondaryBrush = (Brush)FindResource("SecondaryTextBrush");
                cardBrush = (Brush)FindResource("CardBrush");
            }
            catch
            {
                textBrush = Brushes.White;
                secondaryBrush = new SolidColorBrush(Color.FromRgb(174, 174, 178));
                cardBrush = new SolidColorBrush(Color.FromRgb(44, 44, 46));
            }

            var border = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand,
                Tag = task.Id
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            // Checkbox
            var cb = new CheckBox
            {
                IsChecked = task.IsCompleted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = task.Id
            };
            cb.Checked += TaskCheckBox_Changed;
            cb.Unchecked += TaskCheckBox_Changed;
            Grid.SetColumn(cb, 0);

            // Title
            var title = new TextBlock
            {
                Text = task.Title,
                FontSize = 13,
                Foreground = task.IsCompleted ? secondaryBrush : textBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            if (task.IsCompleted)
                title.TextDecorations.Add(TextDecorations.Strikethrough[0]);
            Grid.SetColumn(title, 1);

            // Task type badge
            var badge = CreateTaskBadge(task);
            if (badge != null)
            {
                Grid.SetColumn(badge, 2);
                grid.Children.Add(badge);
            }

            grid.Children.Add(cb);
            grid.Children.Add(title);

            border.Child = grid;

            // Double-click to start timer for this task's tag
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    // Find the task's associated tag (if any)
                    // For now, just toggle expand back
                    Collapse();
                }
            };

            return border;
        }

        private Border CreateTaskBadge(TaskItem task)
        {
            string text;
            Color color;

            switch (task.Type)
            {
                case TaskType.Recurring:
                    var count = _taskService.GetCustomRecurringCountOnDate(task.Id, DateTime.Today);
                    var target = task.RecurringTargetCount ?? 1;
                    text = $"{count}/{target}";
                    color = Color.FromRgb(0, 122, 255); // blue
                    break;
                case TaskType.Quantitative:
                    var cur = task.QuantitativeCurrent ?? 0;
                    var tgt = task.QuantitativeTarget ?? 0;
                    text = $"{cur}/{tgt}{task.QuantitativeUnit ?? ""}";
                    color = Color.FromRgb(52, 199, 89); // green
                    break;
                case TaskType.Periodic:
                    text = "定期";
                    color = Color.FromRgb(255, 149, 0); // orange
                    break;
                default:
                    return null;
            }

            return new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B)),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(6, 0, 0, 0),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(color),
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        private void TaskCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is int taskId)
            {
                var task = _taskRepo.GetTaskById(taskId);
                if (task == null) return;

                if (cb.IsChecked == true)
                {
                    task.IsCompleted = true;
                    task.CompletedAt = DateTime.Now;

                    // Handle recurring
                    if (task.Type == TaskType.Recurring)
                    {
                        _taskService.RecordCustomRecurringCompletion(task.Id, DateTime.Today);
                    }
                }
                else
                {
                    task.IsCompleted = false;
                    task.CompletedAt = null;
                }

                _taskRepo.UpdateTask(task);
                LoadTaskList();
            }
        }

        // ─── Tag chips ─────────────────────────────────────────────────

        private void LoadTagChips()
        {
            TagChipsPanel.Children.Clear();
            var tags = _tagRepo.GetAllTags();

            foreach (var tag in tags)
            {
                var chip = CreateTagChip(tag);
                TagChipsPanel.Children.Add(chip);
            }
        }

        private Border CreateTagChip(TimeTag tag)
        {
            Brush textBrush;
            try { textBrush = (Brush)FindResource("TextBrush"); }
            catch { textBrush = Brushes.White; }

            var isRunning = SharedTimerService.IsRunning && SharedTimerService.SelectedTagId == tag.Id;
            Color tagColor;
            try { tagColor = (Color)ColorConverter.ConvertFromString(tag.Color); }
            catch { tagColor = Color.FromRgb(128, 128, 128); }

            var chip = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = isRunning
                    ? new SolidColorBrush(tagColor)
                    : new SolidColorBrush(Color.FromArgb(30, tagColor.R, tagColor.G, tagColor.B)),
                BorderBrush = isRunning
                    ? new SolidColorBrush(tagColor)
                    : new SolidColorBrush(Color.FromArgb(60, tagColor.R, tagColor.G, tagColor.B)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = Cursors.Hand,
                Tag = tag.Id,
                Child = new TextBlock
                {
                    Text = (isRunning ? "● " : "") + tag.Name,
                    FontSize = 11,
                    Foreground = isRunning ? Brushes.White : textBrush,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            chip.MouseLeftButtonDown += (s, e) =>
            {
                if (SharedTimerService.IsRunning && SharedTimerService.SelectedTagId == tag.Id)
                {
                    SharedTimerService.StopCurrent();
                }
                else
                {
                    SharedTimerService.StartWithTag(tag.Id);
                }
                LoadTagChips();
                e.Handled = true;
            };

            return chip;
        }

        private void UpdateTagChipStates()
        {
            if (_isExpanded) LoadTagChips();
        }
    }
}
