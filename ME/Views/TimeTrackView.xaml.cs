using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ME.Data;
using ME.Models;
using ME.Services;

namespace ME.Views
{
    public partial class TimeTrackView : UserControl
    {
        private readonly TimeTimerService _timer;
        private readonly TimeRecordRepository _recordRepo;
        private readonly TimeTagRepository _tagRepo;
        private List<TimeTag> _allTags = new();
        private int _selectedTagId = 0;
        private DispatcherTimer _clockTimer;
        private DateTime _currentMonth;
        private DateTime _selectedDate;

        public TimeTrackView()
        {
            InitializeComponent();

            _timer = SharedTimerService.Timer;
            _recordRepo = new TimeRecordRepository();
            _tagRepo = new TimeTagRepository();
            _currentMonth = DateTime.Now;
            _selectedDate = DateTime.Now;

            _timer.Tick += OnTimerTick;
            SharedTimerService.TimerUpdated += OnSharedTimerUpdated;
            SharedTimerService.RunningStateChanged += OnSharedRunningStateChanged;

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void TimeTrackView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTags();
            LoadRecords();
            GenerateCalendar();
            SharedTimerService.CheckRunningState();
        }

        private void TimeTrackView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                LoadTags();
                LoadRecords();
                GenerateCalendar();
            }
        }

        // ========== TIMER ==========
        private void OnTimerTick(TimeSpan time)
        {
            Dispatcher.BeginInvoke(() => UpdateTimerDisplay(time));
        }

        private void OnSharedTimerUpdated(string time, string tagName, string tagColor)
        {
            Dispatcher.BeginInvoke(() =>
            {
                TimerText.Text = time;
            });
        }

        private void OnSharedRunningStateChanged(bool isRunning)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!isRunning)
                {
                    TimerText.Text = "00:00:00";
                    TimerText.ClearValue(TextBlock.ForegroundProperty);
                }
            });
        }

        private void UpdateTimerDisplay(TimeSpan time)
        {
            if (_timer.Mode == TimeTimerMode.CountDown)
            {
                TimerText.Text = $"{time.Minutes:D2}:{time.Seconds:D2}";
            }
            else
            {
                TimerText.Text = $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
        }

        // ========== MODE SWITCH ==========
        private void CountUpRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_timer == null) return;
            _timer.SetMode(TimeTimerMode.CountUp);
            TimerText.Text = "00:00:00";
        }

        private void CountDownRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_timer == null) return;
            _timer.SetMode(TimeTimerMode.CountDown);
            TimerText.Text = $"{_timer.FocusMinutes:D2}:00";
        }

        // ========== TAGS ==========
        private void LoadTags()
        {
            _allTags = _tagRepo.GetAllTags();
            var selectedTag = _allTags.FirstOrDefault(t => t.IsDefault) ?? _allTags.FirstOrDefault();
            if (selectedTag != null) _selectedTagId = selectedTag.Id;

            TagItemsControl.ItemsSource = _allTags.Select(t => new
            {
                Tag = t,
                Display = t.Name,
                IsSelected = t.Id == _selectedTagId
            });
        }

        private void TagItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is TimeTag tag)
            {
                _selectedTagId = tag.Id;
                if (SharedTimerService.IsRunning)
                {
                    SharedTimerService.StopCurrent();
                    SharedTimerService.StartWithTag(tag.Id);
                }
                else
                {
                    SharedTimerService.StartWithTag(tag.Id);
                }
                LoadTags();
                LoadRecords();
            }
        }

        private void TagItem_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is TimeTag tag)
            {
                var menu = new ContextMenu();

                var editItem = new MenuItem { Header = "编辑标签" };
                editItem.Click += (s, ev) => EditTag(tag);
                menu.Items.Add(editItem);

                if (!tag.IsDefault)
                {
                    var deleteItem = new MenuItem { Header = "删除" };
                    deleteItem.Click += (s, ev) =>
                    {
                        if (MessageBox.Show($"确认删除标签 \"{tag.Name}\"?", "确认",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            _tagRepo.DeleteTag(tag.Id);
                            LoadTags();
                        }
                    };
                    menu.Items.Add(deleteItem);
                }

                menu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TagEditorDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _tagRepo.InsertTag(dialog.Result);
                LoadTags();
            }
        }

        private void EditTag(TimeTag tag)
        {
            var dialog = new TagEditorDialog(tag);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _tagRepo.UpdateTag(dialog.Result);
                LoadTags();
            }
        }

        // ========== RECORDS ==========
        private void LoadRecords()
        {
            var records = _recordRepo.GetRecordsByDate(_selectedDate.ToString("yyyy-MM-dd"));
            RecordsPanel.Children.Clear();

            foreach (var record in records)
            {
                var tag = _allTags.FirstOrDefault(t => t.Id == record.TagId);
                var color = tag?.Color ?? "#808080";
                var tagName = tag?.Name ?? "未知";

                var recordBorder = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    Background = (Brush)FindResource("SystemControlBackgroundChromeMediumBrush"),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    BorderThickness = new Thickness(3, 0, 0, 0)
                };

                var panel = new StackPanel();

                var header = new TextBlock
                {
                    Text = $"{tagName}  {record.StartTime:HH:mm} - {(record.EndTime?.ToString("HH:mm") ?? "进行中")}",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("SystemControlForegroundBaseHighBrush")
                };
                panel.Children.Add(header);

                if (record.EndTime.HasValue)
                {
                    var dur = record.EndTime.Value - record.StartTime;
                    var durText = new TextBlock
                    {
                        Text = $"时长: {(int)dur.TotalHours}小时{dur.Minutes}分钟",
                        FontSize = 11,
                        Foreground = (Brush)FindResource("SystemControlForegroundBaseMediumBrush"),
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    panel.Children.Add(durText);
                }

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 6, 0, 0)
                };

                var editBtn = new Button
                {
                    Content = "编辑",
                    FontSize = 11,
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 0, 6, 0),
                    Tag = record
                };
                editBtn.Click += (s, ev) => EditRecord(record);
                buttonsPanel.Children.Add(editBtn);

                var deleteBtn = new Button
                {
                    Content = "删除",
                    FontSize = 11,
                    Padding = new Thickness(8, 3, 8, 3),
                    Tag = record
                };
                deleteBtn.Click += (s, ev) =>
                {
                    if (MessageBox.Show("确认删除此记录?", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        _recordRepo.DeleteRecord(record.Id);
                        LoadRecords();
                    }
                };
                buttonsPanel.Children.Add(deleteBtn);

                panel.Children.Add(buttonsPanel);
                recordBorder.Child = panel;
                RecordsPanel.Children.Add(recordBorder);
            }
        }

        private void EditRecord(TimeRecord record)
        {
            var dialog = new RecordEditDialog(record, _allTags);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                record.StartTime = dialog.ResultStartTime;
                record.EndTime = dialog.ResultEndTime;
                record.TagId = dialog.ResultTagId;
                _recordRepo.UpdateRecord(record);
                LoadRecords();
            }
        }

        private void ClearRecords_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"确认清空 {_selectedDate:yyyy-MM-dd} 的所有记录?", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _recordRepo.ClearRecordsByDate(_selectedDate.ToString("yyyy-MM-dd"));
                LoadRecords();
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV文件 (*.csv)|*.csv",
                FileName = $"time_records_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var records = _recordRepo.GetAllRecords();
                    var lines = new List<string> { "日期,标签,开始时间,结束时间,时长(分钟)" };
                    foreach (var r in records)
                    {
                        var tag = _allTags.FirstOrDefault(t => t.Id == r.TagId);
                        var dur = r.EndTime.HasValue ? (r.EndTime.Value - r.StartTime).TotalMinutes : 0;
                        lines.Add($"{r.Date},{tag?.Name ?? "未知"},{r.StartTime:HH:mm},{r.EndTime?.ToString("HH:mm") ?? ""},{dur:F0}");
                    }
                    System.IO.File.WriteAllText(dialog.FileName, string.Join("\n", lines), System.Text.Encoding.UTF8);
                    MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ========== CALENDAR ==========
        private void GenerateCalendar()
        {
            CalendarGrid.Children.Clear();
            var year = _currentMonth.Year;
            var month = _currentMonth.Month;
            MonthLabel.Text = $"{year}年{month}月";

            var firstDay = new DateTime(year, month, 1);
            int startOffset = (int)firstDay.DayOfWeek;
            int daysInMonth = DateTime.DaysInMonth(year, month);
            var today = DateTime.Today;

            var recordsThisMonth = _recordRepo.GetRecordsByDateRange(
                $"{year}-{month:D2}-01",
                $"{year}-{month:D2}-{daysInMonth:D2}"
            );
            var datesWithRecords = recordsThisMonth.Select(r => r.Date).ToHashSet();

            for (int i = 0; i < startOffset; i++)
            {
                CalendarGrid.Children.Add(new Border());
            }

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                var dateStr = date.ToString("yyyy-MM-dd");
                bool isToday = date == today;
                bool isSelected = date.Date == _selectedDate.Date;
                bool hasRecord = datesWithRecords.Contains(dateStr);

                var cell = new Grid { Margin = new Thickness(1) };

                var dayBtn = new Button
                {
                    Content = day.ToString(),
                    FontSize = 12,
                    Tag = date,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Padding = new Thickness(0),
                    MinHeight = 30
                };

                if (isSelected)
                {
                    dayBtn.Background = (Brush)FindResource("SystemControlHighlightAccentBrush");
                    dayBtn.Foreground = Brushes.White;
                    dayBtn.FontWeight = FontWeights.Bold;
                    var template = new ControlTemplate(typeof(Button));
                    var borderFactory = new FrameworkElementFactory(typeof(Border));
                    borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                    borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
                    var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                    contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                    borderFactory.AppendChild(contentPresenter);
                    template.VisualTree = borderFactory;
                    dayBtn.Template = template;
                }
                else
                {
                    dayBtn.Background = Brushes.Transparent;
                    dayBtn.Foreground = (Brush)FindResource("SystemControlForegroundBaseHighBrush");
                }

                dayBtn.Click += (s, ev) =>
                {
                    _selectedDate = (DateTime)((Button)s).Tag;
                    GenerateCalendar();
                    LoadRecords();
                };

                cell.Children.Add(dayBtn);

                if (isToday && !isSelected)
                {
                    var dot = new Border
                    {
                        Width = 5, Height = 5,
                        CornerRadius = new CornerRadius(2.5),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 2)
                    };
                    cell.Children.Add(dot);
                }

                if (hasRecord && !isSelected)
                {
                    var indicator = new Border
                    {
                        Width = 4, Height = 4,
                        CornerRadius = new CornerRadius(2),
                        Background = (Brush)FindResource("SystemControlHighlightAccentBrush"),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 2, 2, 0)
                    };
                    cell.Children.Add(indicator);
                }

                CalendarGrid.Children.Add(cell);
            }
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            GenerateCalendar();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            GenerateCalendar();
        }
    }
}
