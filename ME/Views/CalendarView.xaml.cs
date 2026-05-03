using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ME.Data;
using ME.Models;
using ME.Services;

namespace ME.Views
{
    public partial class CalendarView : UserControl
    {
        private DateTime _currentMonth;
        private DateTime _selectedDate;

        public CalendarView()
        {
            InitializeComponent();
            _currentMonth = DateTime.Today;
            _selectedDate = DateTime.Today;
            LoadCalendar();
        }

        private void CalendarView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible) LoadCalendar();
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            LoadCalendar();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            LoadCalendar();
        }

        private void Day_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is DateTime date)
            {
                _selectedDate = date;
                LoadCalendar();
            }
        }

        private void LoadCalendar()
        {
            MonthTitle.Text = _currentMonth.ToString("yyyy年MM月");
            var taskRepo = new TaskRepository();
            var tagRepo = new TagRepository();
            var taskService = new TaskService();
            var allTasks = taskRepo.GetAllTasks();
            var allTags = tagRepo.GetAllTags();

            var days = new List<CalendarDay>();
            var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var startDay = firstDay.AddDays(-(int)firstDay.DayOfWeek);

            for (int i = 0; i < 42; i++)
            {
                var date = startDay.AddDays(i);
                var dayTasks = new List<CalendarTaskBar>();

                foreach (var task in allTasks)
                {
                    if (task.IsDeleted) continue;
                    if (task.ParentTaskId.HasValue) continue;

                    bool showOnThisDate = false;
                    bool isCompletedOnDate = false;

                    // For recurring tasks, use the new logic
                    if (task.Type == TaskType.Recurring && task.RecurringPattern.HasValue)
                    {
                        showOnThisDate = taskService.ShouldShowRecurringTaskOnDate(task, date);
                        if (showOnThisDate)
                        {
                            isCompletedOnDate = taskService.IsRecurringTaskCompletedOnDate(task, date);
                        }
                    }
                    else
                    {
                        // For non-recurring tasks, use the original logic
                        if (task.StartDate.HasValue && task.EndDate.HasValue)
                        {
                            showOnThisDate = task.StartDate.Value.Date <= date.Date &&
                                             task.EndDate.Value.Date >= date.Date;
                        }
                        else if (!task.StartDate.HasValue && task.CreatedAt.Date == date.Date)
                        {
                            showOnThisDate = true;
                        }
                        isCompletedOnDate = task.IsCompleted;
                    }

                    if (showOnThisDate)
                    {
                        var color = GetTaskColor(task, allTags);
                        dayTasks.Add(new CalendarTaskBar
                        {
                            Title = task.Title,
                            Color = new SolidColorBrush(color),
                            Opacity = isCompletedOnDate ? 0.4 : 0.85
                        });
                    }
                }

                days.Add(new CalendarDay
                {
                    Date = date,
                    Day = date.Day.ToString(),
                    IsToday = date.Date == DateTime.Today,
                    IsOtherMonth = date.Month != _currentMonth.Month,
                    IsSelected = date.Date == _selectedDate.Date,
                    Tasks = dayTasks
                });
            }

            CalendarGrid.ItemsSource = days;
            LoadDayTasks(_selectedDate);
        }

        private Color GetTaskColor(TaskItem task, List<GoalTag> allTags)
        {
            if (task.GoalId.HasValue)
            {
                var goalRepo = new GoalRepository();
                var goal = goalRepo.GetGoalById(task.GoalId.Value);
                if (goal != null && goal.TagId.HasValue)
                {
                    var tag = allTags.Find(t => t.Id == goal.TagId.Value);
                    if (tag != null)
                    {
                        try { return (Color)ColorConverter.ConvertFromString(tag.Color); }
                        catch { }
                    }
                }
            }

            switch (task.Type)
            {
                case TaskType.Recurring: return Color.FromRgb(90, 200, 250);
                case TaskType.Quantitative: return Color.FromRgb(88, 86, 214);
                default: return Color.FromRgb(0, 122, 255);
            }
        }

        private void LoadDayTasks(DateTime date)
        {
            SelectedDateTitle.Text = date.ToString("yyyy年MM月dd日") + " 任务";
            DayTaskPanel.Children.Clear();
            var taskRepo = new TaskRepository();
            var tagRepo = new TagRepository();
            var goalRepo = new GoalRepository();
            var taskService = new TaskService();
            var allTasks = taskRepo.GetAllTasks();
            var allTags = tagRepo.GetAllTags();
            var allGoals = goalRepo.GetAllGoals();

            // Separate main tasks and subtasks (both goal-level and task-level)
            var mainTasks = new List<TaskItem>();
            var subtasksMap = new Dictionary<int, List<TaskItem>>();

            foreach (var task in allTasks)
            {
                if (task.IsDeleted) continue;
                bool show = false;
                bool isCompletedOnDate = false;

                // For recurring tasks, use the new logic
                if (task.Type == TaskType.Recurring && task.RecurringPattern.HasValue)
                {
                    show = taskService.ShouldShowRecurringTaskOnDate(task, date);
                    if (show)
                    {
                        isCompletedOnDate = taskService.IsRecurringTaskCompletedOnDate(task, date);
                    }
                }
                else
                {
                    // For non-recurring tasks, use the original logic
                    if (task.StartDate.HasValue && task.StartDate.Value.Date <= date.Date &&
                        task.EndDate.HasValue && task.EndDate.Value.Date >= date.Date)
                        show = true;
                    if (!task.StartDate.HasValue && task.CreatedAt.Date == date.Date)
                        show = true;
                    isCompletedOnDate = task.IsCompleted;
                }

                if (!show) continue;

                // Create a display task with appropriate completion status
                var displayTask = new TaskItem
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Type = task.Type,
                    GoalId = task.GoalId,
                    ParentTaskId = task.ParentTaskId,
                    StartDate = task.StartDate,
                    EndDate = task.EndDate,
                    IsCompleted = isCompletedOnDate,
                    CompletedAt = isCompletedOnDate ? task.CompletedAt : null,
                    IsDeleted = task.IsDeleted,
                    DeletedAt = task.DeletedAt,
                    CreatedAt = task.CreatedAt,
                    UpdatedAt = task.UpdatedAt,
                    Priority = task.Priority,
                    RecurringPattern = task.RecurringPattern,
                    RecurringInterval = task.RecurringInterval,
                    RecurringDaysOfWeek = task.RecurringDaysOfWeek,
                    RecurringDayOfMonth = task.RecurringDayOfMonth,
                    IsRecurringCompleted = task.IsRecurringCompleted,
                    LastCompletedDate = task.LastCompletedDate,
                    QuantitativeMode = task.QuantitativeMode,
                    QuantitativeStart = task.QuantitativeStart,
                    QuantitativeTarget = task.QuantitativeTarget,
                    QuantitativeCurrent = task.QuantitativeCurrent,
                    QuantitativeUnit = task.QuantitativeUnit,
                    QuantitativeDailyMin = task.QuantitativeDailyMin,
                    CountTowardsParent = task.CountTowardsParent
                };

                if (task.ParentTaskId.HasValue)
                {
                    // Task-level subtask: group under parent task
                    if (!subtasksMap.ContainsKey(task.ParentTaskId.Value))
                        subtasksMap[task.ParentTaskId.Value] = new List<TaskItem>();
                    subtasksMap[task.ParentTaskId.Value].Add(displayTask);
                }
                else
                {
                    // Main task (standalone or goal-level)
                    mainTasks.Add(displayTask);
                }
            }

            if (mainTasks.Count == 0)
            {
                DayTaskPanel.Children.Add(new TextBlock
                {
                    Text = "当天没有任务",
                    FontSize = 12,
                    Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            // Build tree display for each main task
            foreach (var task in mainTasks)
            {
                var wrapper = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

                // Get tag color for this task
                string tagColor = null;
                string tagName = null;
                if (task.GoalId.HasValue)
                {
                    var goal = allGoals.Find(g => g.Id == task.GoalId.Value);
                    if (goal != null && goal.TagId.HasValue)
                    {
                        var tag = allTags.Find(t => t.Id == goal.TagId.Value);
                        if (tag != null) { tagColor = tag.Color; tagName = tag.Name; }
                    }
                }

                var progressColor = string.IsNullOrEmpty(tagColor) ? (SolidColorBrush)FindResource("PrimaryBrush")
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString(tagColor));

                // Main task card
                var card = new Border
                {
                    Style = (Style)FindResource("CardStyle"),
                    Padding = new Thickness(10, 8, 10, 8)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Completion circle
                bool isQuant = task.Type == TaskType.Quantitative && task.QuantitativeTarget.HasValue && task.QuantitativeTarget > 0;
                var circle = new Border
                {
                    Width = 20, Height = 20,
                    CornerRadius = isQuant ? new CornerRadius(4) : new CornerRadius(10),
                    BorderThickness = new Thickness(1.5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = isQuant
                        ? (task.IsCompleted ? (SolidColorBrush)FindResource("PrimaryBrush") : progressColor)
                        : (task.IsCompleted ? (SolidColorBrush)FindResource("PrimaryBrush") : Brushes.Transparent),
                    BorderBrush = task.IsCompleted ? (SolidColorBrush)FindResource("PrimaryBrush")
                        : (SolidColorBrush)FindResource("BorderBrush"),
                    Child = new TextBlock
                    {
                        Text = task.IsCompleted ? "✓" : (isQuant ? "+" : ""),
                        Foreground = Brushes.White,
                        FontSize = task.IsCompleted ? 10 : 12,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Grid.SetColumn(circle, 0);
                grid.Children.Add(circle);

                // Text area
                var textPanel = new StackPanel { IsHitTestVisible = false };

                // Tag badge + Name
                var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
                if (!string.IsNullOrEmpty(tagName))
                {
                    namePanel.Children.Add(new Border
                    {
                        CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 8, 0),
                        Background = new SolidColorBrush(string.IsNullOrEmpty(tagColor)
                            ? Color.FromRgb(0, 122, 255)
                            : (Color)ColorConverter.ConvertFromString(tagColor)),
                        Child = new TextBlock { Text = tagName, FontSize = 10, Foreground = Brushes.White }
                    });
                }
                namePanel.Children.Add(new TextBlock
                {
                    Text = task.Title, FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = task.IsCompleted ? (SolidColorBrush)FindResource("SecondaryTextBrush") : (SolidColorBrush)FindResource("TextBrush"),
                    TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null
                });
                textPanel.Children.Add(namePanel);

                if (!task.IsCompleted)
                {
                    // Date range
                    if (task.StartDate.HasValue && task.EndDate.HasValue)
                    {
                        textPanel.Children.Add(new TextBlock
                        {
                            Text = $"{task.StartDate.Value:MM/dd} - {task.EndDate.Value:MM/dd}",
                            FontSize = 10,
                            Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush"),
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                    }

                    // Progress info
                    var infoPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                    if (isQuant)
                    {
                        var pct = task.QuantitativeTarget > 0
                            ? Math.Min((task.QuantitativeCurrent ?? 0) / task.QuantitativeTarget.Value * 100, 100) : 0;
                        infoPanel.Children.Add(new TextBlock
                        {
                            Text = $"{pct:F0}%", FontSize = 10, FontWeight = FontWeights.SemiBold,
                            Foreground = progressColor, Margin = new Thickness(0, 0, 6, 0)
                        });
                        infoPanel.Children.Add(new TextBlock
                        {
                            Text = $"{task.QuantitativeCurrent ?? 0:F0}/{task.QuantitativeTarget.Value:F0}",
                            FontSize = 10, Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush")
                        });
                    }
                    textPanel.Children.Add(infoPanel);

                    // Progress bar
                    if (isQuant)
                    {
                        var pbValue = task.QuantitativeTarget > 0
                            ? Math.Min((task.QuantitativeCurrent ?? 0) / task.QuantitativeTarget.Value * 100, 100) : 0;
                        textPanel.Children.Add(new ProgressBar
                        {
                            Value = pbValue, Maximum = 100, Height = 6,
                            Margin = new Thickness(0, 4, 0, 0),
                            Background = (SolidColorBrush)FindResource("BackgroundBrush"),
                            Foreground = progressColor
                        });
                    }
                }

                Grid.SetColumn(textPanel, 1);
                grid.Children.Add(textPanel);

                card.Child = grid;
                wrapper.Children.Add(card);

                // Subtasks in tree structure
                if (subtasksMap.ContainsKey(task.Id))
                {
                    var subtasks = subtasksMap[task.Id];
                    var subtaskExpander = new Expander
                    {
                        IsExpanded = true,
                        Margin = new Thickness(20, 0, 0, 0),
                        Header = new TextBlock
                        {
                            Text = $"子任务 ({subtasks.Count})",
                            FontSize = 11,
                            Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush")
                        }
                    };
                    var subtaskPanel = new StackPanel();
                    foreach (var sub in subtasks)
                    {
                        var subCard = new Border
                        {
                            Style = (Style)FindResource("CardStyle"),
                            Padding = new Thickness(10, 6, 10, 6),
                            Margin = new Thickness(0, 0, 0, 4)
                        };

                        var subGrid = new Grid();
                        subGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        subGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        // Subtask completion circle (using parent's tag color)
                        var subCircle = new Border
                        {
                            Width = 16, Height = 16,
                            CornerRadius = sub.Type == TaskType.Quantitative ? new CornerRadius(3) : new CornerRadius(8),
                            BorderThickness = new Thickness(1.5),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 8, 0),
                            Background = sub.Type == TaskType.Quantitative ? progressColor : Brushes.Transparent,
                            BorderBrush = sub.IsCompleted ? progressColor : (SolidColorBrush)FindResource("BorderBrush"),
                            Child = new TextBlock
                            {
                                Text = sub.IsCompleted ? "✓" : (sub.Type == TaskType.Quantitative ? "+" : ""),
                                Foreground = Brushes.White, FontSize = sub.IsCompleted ? 8 : 10,
                                FontWeight = FontWeights.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        };
                        Grid.SetColumn(subCircle, 0);
                        subGrid.Children.Add(subCircle);

                        // Subtask text
                        var subTextPanel = new StackPanel { IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Center };
                        subTextPanel.Children.Add(new TextBlock
                        {
                            Text = sub.Title, FontSize = 11,
                            Foreground = sub.IsCompleted ? (SolidColorBrush)FindResource("SecondaryTextBrush") : (SolidColorBrush)FindResource("TextBrush"),
                            TextDecorations = sub.IsCompleted ? TextDecorations.Strikethrough : null
                        });

                        // Quantitative progress for subtask (using parent's tag color)
                        if (sub.Type == TaskType.Quantitative && sub.QuantitativeTarget.HasValue)
                        {
                            var subPb = new ProgressBar
                            {
                                Maximum = 100, Height = 4,
                                Margin = new Thickness(0, 3, 80, 0),
                                Background = (SolidColorBrush)FindResource("BackgroundBrush"),
                                Foreground = progressColor,
                                Value = sub.QuantitativeTarget > 0
                                    ? Math.Min((sub.QuantitativeCurrent ?? 0) / sub.QuantitativeTarget.Value * 100, 100) : 0
                            };
                            subTextPanel.Children.Add(subPb);

                            subTextPanel.Children.Add(new TextBlock
                            {
                                Text = $"{sub.QuantitativeCurrent ?? 0:F0}/{sub.QuantitativeTarget.Value:F0}",
                                FontSize = 9, FontWeight = FontWeights.SemiBold,
                                Foreground = progressColor,
                                Margin = new Thickness(0, 2, 0, 0)
                            });
                        }

                        Grid.SetColumn(subTextPanel, 1);
                        subGrid.Children.Add(subTextPanel);

                        subCard.Child = subGrid;
                        subtaskPanel.Children.Add(subCard);
                    }
                    subtaskExpander.Content = subtaskPanel;
                    wrapper.Children.Add(subtaskExpander);
                }

                DayTaskPanel.Children.Add(wrapper);
            }
        }
    }

    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public string Day { get; set; }
        public bool IsToday { get; set; }
        public bool IsOtherMonth { get; set; }
        public bool IsSelected { get; set; }
        public List<CalendarTaskBar> Tasks { get; set; } = new List<CalendarTaskBar>();
    }

    public class CalendarTaskBar
    {
        public string Title { get; set; }
        public SolidColorBrush Color { get; set; }
        public double Opacity { get; set; }
    }
}