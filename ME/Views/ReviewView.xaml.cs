using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ME.Data;
using ME.Models;

namespace ME.Views
{
    public partial class ReviewView : UserControl
    {
        private bool _isWeekly = true;
        private readonly TaskCompletionRepository _completionRepo;
        private readonly TimeRecordRepository _timeRecordRepo;
        private readonly TimeTagRepository _timeTagRepo;
        private SortedDictionary<string, DailyData> _lastDailyData = new SortedDictionary<string, DailyData>();
        private bool _hasAnimated = false;

        public ReviewView()
        {
            InitializeComponent();
            _completionRepo = new TaskCompletionRepository();
            _timeRecordRepo = new TimeRecordRepository();
            _timeTagRepo = new TimeTagRepository();
            UpdateButtonStyles();
            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible)
                {
                    LoadData();
                    if (!_hasAnimated) { _hasAnimated = true; AnimateEntrance(); }
                }
            };
            this.SizeChanged += (s, e) =>
            {
                if (this.IsVisible) DrawLineChart(_lastDailyData);
            };
            LoadData();
        }

        private void Weekly_Click(object sender, RoutedEventArgs e)
        {
            _isWeekly = true;
            UpdateButtonStyles();
            LoadData();
        }

        private void Monthly_Click(object sender, RoutedEventArgs e)
        {
            _isWeekly = false;
            UpdateButtonStyles();
            LoadData();
        }

        private void UpdateButtonStyles()
        {
            if (_isWeekly)
            {
                WeeklyBtn.Style = (Style)FindResource("PrimaryButtonStyle");
                MonthlyBtn.Style = (Style)FindResource("SecondaryButtonStyle");
            }
            else
            {
                WeeklyBtn.Style = (Style)FindResource("SecondaryButtonStyle");
                MonthlyBtn.Style = (Style)FindResource("PrimaryButtonStyle");
            }
        }

        private void AnimateEntrance()
        {
            // Animate the ring after a short delay
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                // Re-trigger ring animation with current rate
                var rateText = CompletionRateText.Text.Replace("%", "");
                if (double.TryParse(rateText, out var rate))
                    AnimateRateRing(rate);
            };
            timer.Start();
        }

        private void LoadData()
        {
            var taskRepo = new TaskRepository();
            var goalRepo = new GoalRepository();
            var allTasks = taskRepo.GetAllTasks();

            var now = DateTime.Now;
            var startDate = _isWeekly
                ? now.Date.AddDays(-(int)now.DayOfWeek)
                : new DateTime(now.Year, now.Month, 1);
            var startStr = startDate.ToString("yyyy-MM-dd");
            var endStr = now.ToString("yyyy-MM-dd");

            DateRangeText.Text = $"{startDate:MM/dd} — {now:MM/dd}";

            int completed = 0, total = 0;
            var dailyData = new SortedDictionary<string, DailyData>();
            for (var d = startDate; d <= now; d = d.AddDays(1))
                dailyData[d.ToString("MM/dd")] = new DailyData { Date = d.ToString("MM/dd") };

            var allCompletions = _completionRepo.GetAll();
            var periodCompletions = allCompletions
                .Where(c => c.Date.CompareTo(startStr) >= 0 && c.Date.CompareTo(endStr) <= 0)
                .ToList();
            completed = periodCompletions.Count;

            foreach (var task in allTasks)
            {
                if (!task.IsDeleted) total++;
                var taskDayCompletions = allCompletions
                    .Where(c => c.TaskId == task.Id && c.Date.CompareTo(startStr) >= 0 && c.Date.CompareTo(endStr) <= 0);
                foreach (var c in taskDayCompletions)
                {
                    var dayKey = DateTime.Parse(c.Date).ToString("MM/dd");
                    if (dailyData.ContainsKey(dayKey))
                        dailyData[dayKey].Completed++;
                }
            }

            // Completion rate
            var days = Math.Max(1, DaysBetween(startDate, now) + 1);
            var rate = total > 0 ? (double)completed / (total * days) * 100 : 0;
            rate = Math.Min(rate, 100);
            CompletionRateText.Text = $"{rate:F0}%";
            AnimateRateRing(rate);

            CompletedCountText.Text = completed.ToString();

            // Previous period comparison
            var prevStart = _isWeekly ? startDate.AddDays(-7) : startDate.AddMonths(-1);
            var prevEnd = startDate.AddDays(-1);
            var prevCompletions = allCompletions
                .Where(c => c.Date.CompareTo(prevStart.ToString("yyyy-MM-dd")) >= 0
                         && c.Date.CompareTo(prevEnd.ToString("yyyy-MM-dd")) <= 0)
                .Count();
            var diff = completed - prevCompletions;
            if (prevCompletions > 0 || completed > 0)
            {
                var sign = diff >= 0 ? "+" : "";
                CompletedTrendText.Text = $"较上期{sign}{diff}";
                CompletedTrendText.Foreground = diff >= 0
                    ? (Brush)FindResource("AccentGreenBrush")
                    : new SolidColorBrush(Color.FromRgb(255, 59, 48));
            }

            // Time invested
            var timeRecords = _timeRecordRepo.GetRecordsByDateRange(startStr, endStr);
            var totalMinutes = timeRecords.Sum(r => r.Duration.TotalMinutes);
            var hours = (int)(totalMinutes / 60);
            var mins = (int)(totalMinutes % 60);
            TimeInvestedText.Text = hours > 0 ? $"{hours}h{mins:D2}" : $"{mins}m";
            var avgDaily = totalMinutes / days;
            TimeDailyAvgText.Text = $"日均 {(int)(avgDaily / 60)}h{(int)(avgDaily % 60):D2}m";

            // Streak
            int streak = 0, bestStreak = 0, tempStreak = 0;
            var checkDate = DateTime.Today;
            while (checkDate >= startDate)
            {
                var dateKey = checkDate.ToString("MM/dd");
                if (dailyData.ContainsKey(dateKey) && dailyData[dateKey].Completed > 0)
                {
                    streak++;
                    checkDate = checkDate.AddDays(-1);
                }
                else break;
            }
            foreach (var kv in dailyData)
            {
                if (kv.Value.Completed > 0) { tempStreak++; if (tempStreak > bestStreak) bestStreak = tempStreak; }
                else tempStreak = 0;
            }
            StreakDaysText.Text = $"{streak}";
            StreakBestText.Text = $"最长 {bestStreak} 天";

            // Activity heatmap
            BuildHeatmap(dailyData, startDate, now);

            // Time allocation
            BuildTimeAllocation(timeRecords);

            // Chart
            _lastDailyData = dailyData;
            DrawLineChart(dailyData);

            // Goal progress
            BuildGoalProgress(goalRepo);
        }

        private int DaysBetween(DateTime a, DateTime b) => Math.Max(0, (int)(b.Date - a.Date).TotalDays);

        private void AnimateRateRing(double rate)
        {
            // Circumference = 2πr, r = (48-4)/2 = 22, C ≈ 138.23
            // DashArray value = C / StrokeThickness = 138.23 / 4 ≈ 34.56
            var dashLen = 34.56;
            var targetOffset = dashLen * (1 - rate / 100);
            var anim = new DoubleAnimation(dashLen, targetOffset, TimeSpan.FromSeconds(0.8))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            RateRing.BeginAnimation(Ellipse.StrokeDashOffsetProperty, anim);
        }

        private void BuildHeatmap(SortedDictionary<string, DailyData> dailyData, DateTime startDate, DateTime now)
        {
            HeatmapContainer.Child = null;

            // Show past 30 days as a grid of colored squares
            var grid = new UniformGrid();
            grid.Columns = 7; // 7 days per row

            var lookback = _isWeekly ? 28 : 60;
            var heatmapStart = now.Date.AddDays(-lookback + 1);

            int maxCompleted = 1;
            for (var d = heatmapStart; d <= now; d = d.AddDays(1))
            {
                var key = d.ToString("MM/dd");
                if (dailyData.ContainsKey(key) && dailyData[key].Completed > maxCompleted)
                    maxCompleted = dailyData[key].Completed;
            }

            for (var d = heatmapStart; d <= now; d = d.AddDays(1))
            {
                var key = d.ToString("MM/dd");
                var count = dailyData.ContainsKey(key) ? dailyData[key].Completed : 0;
                var intensity = maxCompleted > 0 ? (double)count / maxCompleted : 0;

                Color cellColor;
                if (count == 0)
                    cellColor = Color.FromArgb(30, 142, 142, 147);
                else if (intensity < 0.25)
                    cellColor = Color.FromArgb(80, 52, 199, 89);
                else if (intensity < 0.5)
                    cellColor = Color.FromArgb(140, 52, 199, 89);
                else if (intensity < 0.75)
                    cellColor = Color.FromArgb(200, 52, 199, 89);
                else
                    cellColor = Color.FromRgb(52, 199, 89);

                var cell = new Border
                {
                    Width = 16, Height = 16,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(cellColor),
                    Margin = new Thickness(1),
                    ToolTip = $"{d:MM/dd}: {count} 项完成"
                };
                grid.Children.Add(cell);
            }

            HeatmapContainer.Child = grid;
        }

        private void BuildTimeAllocation(List<TimeRecord> records)
        {
            TimeAllocPanel.Children.Clear();
            var tags = _timeTagRepo.GetAllTags();

            var tagTimes = new Dictionary<int, double>();
            foreach (var r in records)
            {
                if (!tagTimes.ContainsKey(r.TagId)) tagTimes[r.TagId] = 0;
                tagTimes[r.TagId] += r.Duration.TotalMinutes;
            }

            if (tagTimes.Count == 0)
            {
                TimeAllocPanel.Children.Add(new TextBlock
                {
                    Text = "暂无时间记录",
                    FontSize = 12,
                    Foreground = (Brush)FindResource("SecondaryTextBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            var maxMinutes = tagTimes.Values.Max();
            var sorted = tagTimes.OrderByDescending(kv => kv.Value).Take(6);
            int idx = 0;

            foreach (var kv in sorted)
            {
                var tag = tags.FirstOrDefault(t => t.Id == kv.Key);
                var name = tag?.Name ?? "未标记";
                Color tagColor;
                try { tagColor = (Color)ColorConverter.ConvertFromString(tag?.Color ?? "#808080"); }
                catch { tagColor = Color.FromRgb(128, 128, 128); }

                var mins = kv.Value;
                var h = (int)(mins / 60);
                var m = (int)(mins % 60);
                var timeStr = h > 0 ? $"{h}h{m:D2}m" : $"{m}m";
                var pct = maxMinutes > 0 ? mins / maxMinutes : 0;

                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });

                var dotRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                dotRow.Children.Add(new Border
                {
                    Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(tagColor),
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                dotRow.Children.Add(new TextBlock
                {
                    Text = name,
                    FontSize = 11,
                    Foreground = (Brush)FindResource("TextBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                });
                row.Children.Add(dotRow);

                // Animated bar
                var barBg = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.FromArgb(30, tagColor.R, tagColor.G, tagColor.B)),
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(6, 0, 6, 0)
                };
                var bar = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(tagColor),
                    Width = 0, // Start at 0, animate to target
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                barBg.Child = bar;
                Grid.SetColumn(barBg, 1);
                row.Children.Add(barBg);

                row.Children.Add(new TextBlock
                {
                    Text = timeStr,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(tagColor),
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Right
                });
                Grid.SetColumn(row.Children[row.Children.Count - 1], 2);

                TimeAllocPanel.Children.Add(row);

                // Animate bar width with staggered delay
                var targetWidth = pct * 100;
                var delay = TimeSpan.FromMilliseconds(200 + idx * 80);
                var widthAnim = new DoubleAnimation(0, targetWidth, TimeSpan.FromSeconds(0.5))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    BeginTime = delay
                };
                bar.BeginAnimation(Border.WidthProperty, widthAnim);
                idx++;
            }
        }

        private void DrawLineChart(SortedDictionary<string, DailyData> dailyData)
        {
            ChartCanvas.Children.Clear();
            if (dailyData.Count == 0) return;

            var canvasWidth = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : ChartGrid.ActualWidth;
            if (canvasWidth < 10) canvasWidth = 600;
            var canvasHeight = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight : 180;

            int maxVal = 1;
            foreach (var kv in dailyData)
                if (kv.Value.Completed > maxVal) maxVal = kv.Value.Completed;

            var padding = 40;
            var chartWidth = canvasWidth - padding * 2;
            var chartHeight = canvasHeight - padding * 2;

            var points = new List<Point>();
            var entries = new List<KeyValuePair<string, DailyData>>(dailyData);
            var step = entries.Count > 1 ? chartWidth / (entries.Count - 1) : chartWidth;

            for (int i = 0; i < entries.Count; i++)
            {
                var x = padding + i * step;
                var y = padding + chartHeight - (entries[i].Value.Completed / (double)maxVal * chartHeight);
                points.Add(new Point(x, y));

                var dateLabel = new TextBlock
                {
                    Text = entries[i].Key,
                    FontSize = 9,
                    Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush")
                };
                Canvas.SetLeft(dateLabel, x - 15);
                Canvas.SetTop(dateLabel, canvasHeight - 18);
                ChartCanvas.Children.Add(dateLabel);

                if (entries[i].Value.Completed > 0)
                {
                    var valueLabel = new TextBlock
                    {
                        Text = entries[i].Value.Completed.ToString(),
                        FontSize = 9,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (SolidColorBrush)FindResource("PrimaryBrush")
                    };
                    Canvas.SetLeft(valueLabel, x - 5);
                    Canvas.SetTop(valueLabel, y - 16);
                    ChartCanvas.Children.Add(valueLabel);
                }
            }

            for (int i = 0; i <= 4; i++)
            {
                var y = padding + i * chartHeight / 4;
                var line = new Line
                {
                    X1 = padding, Y1 = y, X2 = canvasWidth - padding, Y2 = y,
                    Stroke = (SolidColorBrush)FindResource("BorderBrush"),
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                ChartCanvas.Children.Add(line);
            }

            if (points.Count > 1)
            {
                var areaFigure = new PathFigure { StartPoint = new Point(points[0].X, padding + chartHeight) };
                foreach (var p in points) areaFigure.Segments.Add(new LineSegment(p, true));
                areaFigure.Segments.Add(new LineSegment(new Point(points[points.Count - 1].X, padding + chartHeight), true));
                areaFigure.IsClosed = true;
                var areaGeometry = new PathGeometry();
                areaGeometry.Figures.Add(areaFigure);
                ChartCanvas.Children.Add(new Path
                {
                    Data = areaGeometry,
                    Fill = new SolidColorBrush(Color.FromArgb(30, 0, 122, 255)),
                    Stroke = Brushes.Transparent
                });
            }

            if (points.Count > 1)
            {
                var lineFigure = new PathFigure { StartPoint = points[0] };
                for (int i = 1; i < points.Count; i++) lineFigure.Segments.Add(new LineSegment(points[i], true));
                var lineGeometry = new PathGeometry();
                lineGeometry.Figures.Add(lineFigure);
                ChartCanvas.Children.Add(new Path
                {
                    Data = lineGeometry,
                    Stroke = (SolidColorBrush)FindResource("PrimaryBrush"),
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round
                });
            }

            foreach (var p in points)
            {
                var dot = new Ellipse
                {
                    Width = 6, Height = 6,
                    Fill = (SolidColorBrush)FindResource("PrimaryBrush"),
                    Stroke = (Brush)FindResource("CardBrush"),
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(dot, p.X - 3);
                Canvas.SetTop(dot, p.Y - 3);
                ChartCanvas.Children.Add(dot);
            }
        }

        private void BuildGoalProgress(GoalRepository goalRepo)
        {
            var allGoals = goalRepo.GetAllGoals();
            var goalList = new List<GoalDisplay>();
            var colors = new[]
            {
                Color.FromRgb(0, 122, 255),
                Color.FromRgb(52, 199, 89),
                Color.FromRgb(255, 149, 0),
                Color.FromRgb(175, 82, 222),
                Color.FromRgb(255, 59, 48)
            };
            int colorIdx = 0;
            foreach (var goal in allGoals)
            {
                if (!goal.IsDeleted && !goal.IsArchived)
                {
                    var color = colors[colorIdx % colors.Length];
                    goalList.Add(new GoalDisplay
                    {
                        Name = goal.Name,
                        Progress = goal.Progress,
                        ProgressText = $"{goal.Progress:F0}%",
                        ColorBrush = new SolidColorBrush(color)
                    });
                    colorIdx++;
                }
            }
            GoalProgressList.ItemsSource = goalList;
        }

        private class DailyData
        {
            public string Date { get; set; }
            public int Completed { get; set; }
        }
    }

    public class GoalDisplay
    {
        public string Name { get; set; }
        public double Progress { get; set; }
        public string ProgressText { get; set; }
        public Brush ColorBrush { get; set; }
    }
}
