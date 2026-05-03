using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ME.Data;
using ME.Models;

namespace ME.Views
{
    public partial class ReviewView : UserControl
    {
        private bool _isWeekly = true;

        public ReviewView()
        {
            InitializeComponent();
            UpdateButtonStyles();
            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible)
                    LoadData();
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

        private void LoadData()
        {
            var taskRepo = new TaskRepository();
            var goalRepo = new GoalRepository();
            var allTasks = taskRepo.GetAllTasks();
            var allGoals = goalRepo.GetAllGoals();

            var now = DateTime.Now;
            var startDate = _isWeekly
                ? now.Date.AddDays(-(int)now.DayOfWeek)
                : new DateTime(now.Year, now.Month, 1);

            int completed = 0, pending = 0, total = 0;
            var dailyData = new SortedDictionary<string, DailyData>();

            for (var d = startDate; d <= now; d = d.AddDays(1))
            {
                dailyData[d.ToString("MM/dd")] = new DailyData { Date = d.ToString("MM/dd") };
            }

            foreach (var task in allTasks)
            {
                if (task.IsCompleted && task.CompletedAt.HasValue && task.CompletedAt.Value >= startDate)
                {
                    completed++;
                    var dayKey = task.CompletedAt.Value.ToString("MM/dd");
                    if (dailyData.ContainsKey(dayKey))
                        dailyData[dayKey].Completed++;
                }
                else if (!task.IsCompleted)
                {
                    pending++;
                }
                total++;
            }

            var rate = total > 0 ? (double)completed / total * 100 : 0;
            CompletionRateText.Text = $"{rate:F0}%";
            CompletedCountText.Text = completed.ToString();
            PendingCountText.Text = pending.ToString();
            CheckinCountText.Text = completed.ToString();

            // Draw line chart
            DrawLineChart(dailyData);

            // Goal progress
            var goalTagRepo = new TagRepository();
            var tags = goalTagRepo.GetAllTags();
            var goalList = new List<GoalDisplay>();
            foreach (var goal in allGoals)
            {
                if (!goal.IsDeleted && !goal.IsArchived)
                {
                    goalList.Add(new GoalDisplay
                    {
                        Name = goal.Name,
                        Progress = goal.Progress,
                        ProgressText = $"{goal.Progress:F0}%"
                    });
                }
            }
            GoalProgressList.ItemsSource = goalList;
        }

        private void DrawLineChart(SortedDictionary<string, DailyData> dailyData)
        {
            ChartCanvas.Children.Clear();

            if (dailyData.Count == 0) return;

            var canvasWidth = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : 600;
            var canvasHeight = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight : 180;

            // Get max value for scaling
            int maxVal = 1;
            foreach (var kv in dailyData)
            {
                if (kv.Value.Completed > maxVal) maxVal = kv.Value.Completed;
            }

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

                // Date label
                var dateLabel = new TextBlock
                {
                    Text = entries[i].Key,
                    FontSize = 9,
                    Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush")
                };
                Canvas.SetLeft(dateLabel, x - 15);
                Canvas.SetTop(dateLabel, canvasHeight - 18);
                ChartCanvas.Children.Add(dateLabel);

                // Value label
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

            // Draw grid lines
            for (int i = 0; i <= 4; i++)
            {
                var y = padding + i * chartHeight / 4;
                var line = new Line
                {
                    X1 = padding,
                    Y1 = y,
                    X2 = canvasWidth - padding,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(229, 229, 234)),
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                ChartCanvas.Children.Add(line);
            }

            // Draw area fill
            if (points.Count > 1)
            {
                var areaFigure = new PathFigure
                {
                    StartPoint = new Point(points[0].X, padding + chartHeight)
                };
                foreach (var p in points)
                    areaFigure.Segments.Add(new LineSegment(p, true));
                areaFigure.Segments.Add(new LineSegment(new Point(points[points.Count - 1].X, padding + chartHeight), true));
                areaFigure.IsClosed = true;

                var areaGeometry = new PathGeometry();
                areaGeometry.Figures.Add(areaFigure);

                var areaPath = new Path
                {
                    Data = areaGeometry,
                    Fill = new SolidColorBrush(Color.FromArgb(30, 0, 122, 255)),
                    Stroke = Brushes.Transparent
                };
                ChartCanvas.Children.Add(areaPath);
            }

            // Draw line
            if (points.Count > 1)
            {
                var lineFigure = new PathFigure
                {
                    StartPoint = points[0]
                };
                for (int i = 1; i < points.Count; i++)
                {
                    lineFigure.Segments.Add(new LineSegment(points[i], true));
                }

                var lineGeometry = new PathGeometry();
                lineGeometry.Figures.Add(lineFigure);

                var linePath = new Path
                {
                    Data = lineGeometry,
                    Stroke = (SolidColorBrush)FindResource("PrimaryBrush"),
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round
                };
                ChartCanvas.Children.Add(linePath);
            }

            // Draw dots
            foreach (var p in points)
            {
                var dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = (SolidColorBrush)FindResource("PrimaryBrush"),
                    Stroke = Brushes.White,
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(dot, p.X - 3);
                Canvas.SetTop(dot, p.Y - 3);
                ChartCanvas.Children.Add(dot);
            }
        }

        private class DailyData
        {
            public string Date { get; set; }
            public int Completed { get; set; }
        }
    }

    public class DailyDisplay
    {
        public string Date { get; set; }
        public string Rate { get; set; }
        public double ProgressWidth { get; set; }
    }

    public class GoalDisplay
    {
        public string Name { get; set; }
        public double Progress { get; set; }
        public string ProgressText { get; set; }
    }
}
