using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ME.Core;
using ME.Data;
using ME.Models;
using ME.Services;

namespace ME.Views
{
    public partial class DashboardView : UserControl
    {
        private DateTime _currentMonth;
        private TaskItem _selectedTask;

        public DashboardView()
        {
            InitializeComponent();
            _currentMonth = DateTime.Today;
            LoadData();
            
            // Subscribe to task completion events
            EventAggregator.Instance.Subscribe<string>(OnTaskCompleted);
        }

        private void OnTaskCompleted(string message)
        {
            if (message == "TaskCompleted" && this.IsVisible)
            {
                // Refresh calendar and stats
                if (_selectedTask != null)
                {
                    LoadCalendar();
                    LoadStats();
                }
            }
        }

        private void DashboardView_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                LoadData();
                // Refresh calendar if task is selected
                if (_selectedTask != null)
                {
                    LoadCalendar();
                    LoadStats();
                }
            }
        }

        private void LoadData()
        {
            var taskRepo = new TaskRepository();
            var goalRepo = new GoalRepository();
            var tagRepo = new TagRepository();
            var allTasks = taskRepo.GetAllTasks();
            var allGoals = goalRepo.GetAllGoals();
            var allTags = tagRepo.GetAllTags();

            // Load tasks into list
            var taskDisplayList = new List<TaskDisplayItem>();
            foreach (var task in allTasks)
            {
                if (!task.IsDeleted && !task.ParentTaskId.HasValue)
                {
                    var item = new TaskDisplayItem
                    {
                        Task = task,
                        Title = task.Title,
                        Description = task.Description ?? "",
                        TypeText = GetTypeText(task)
                    };

                    // Get tag info
                    if (task.GoalId.HasValue)
                    {
                        var goal = allGoals.Find(g => g.Id == task.GoalId.Value);
                        if (goal != null && goal.TagId.HasValue)
                        {
                            var tag = allTags.Find(t => t.Id == goal.TagId.Value);
                            if (tag != null)
                            {
                                item.TagName = tag.Name;
                                item.TagColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tag.Color));
                                item.TagVisibility = Visibility.Visible;
                            }
                        }
                    }
                    
                    if (item.TagColorBrush == null)
                    {
                        item.TagColorBrush = new SolidColorBrush(Color.FromRgb(0, 122, 255));
                        item.TagVisibility = Visibility.Collapsed;
                    }

                    // Set progress for quantitative tasks
                    if (task.Type == TaskType.Quantitative && task.QuantitativeTarget.HasValue && task.QuantitativeTarget > 0)
                    {
                        item.ProgressVisibility = Visibility.Visible;
                        var current = task.QuantitativeCurrent ?? 0;
                        var target = task.QuantitativeTarget.Value;
                        item.ProgressValue = Math.Min(current / target * 100, 100);
                        item.ProgressText = $"{current:F0}/{target:F0}";
                        item.ProgressBrush = new SolidColorBrush(Color.FromRgb(0, 122, 255));
                    }
                    // Set progress for custom recurring tasks
                    else if (task.Type == TaskType.Recurring && task.RecurringPattern == RecurringPattern.Custom &&
                             task.RecurringTargetCount.HasValue && task.RecurringTargetCount > 1)
                    {
                        item.ProgressVisibility = Visibility.Visible;
                        var current = task.RecurringCurrentCount ?? 0;
                        var target = task.RecurringTargetCount.Value;
                        item.ProgressValue = target > 0 ? Math.Min((double)current / target * 100, 100) : 0;
                        item.ProgressText = $"{current}/{target}";
                        item.ProgressBrush = new SolidColorBrush(Color.FromRgb(52, 199, 89));
                    }
                    else
                    {
                        item.ProgressVisibility = Visibility.Collapsed;
                        item.ProgressValue = 0;
                        item.ProgressText = "";
                        item.ProgressBrush = new SolidColorBrush(Color.FromRgb(0, 122, 255));
                    }

                    taskDisplayList.Add(item);
                }
            }
            TaskListBox.ItemsSource = taskDisplayList;

            // Select first task if available
            if (taskDisplayList.Count > 0 && TaskListBox.SelectedIndex < 0)
            {
                TaskListBox.SelectedIndex = 0;
            }
        }

        private string GetTypeText(TaskItem task)
        {
            switch (task.Type)
            {
                case TaskType.Recurring:
                    if (task.RecurringPattern.HasValue)
                    {
                        switch (task.RecurringPattern.Value)
                        {
                            case RecurringPattern.Daily: return "每天";
                            case RecurringPattern.Weekday: return "工作日";
                            case RecurringPattern.Weekend: return "周末";
                            case RecurringPattern.Weekly: return "每周";
                            case RecurringPattern.Monthly: return "每月";
                            case RecurringPattern.Interval: return $"每{task.RecurringInterval}天";
                            case RecurringPattern.Custom: return "自定义";
                            default: return "循环";
                        }
                    }
                    return "循环";
                case TaskType.Quantitative: return "量化";
                default: return "单次";
            }
        }

        private void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaskListBox.SelectedItem is TaskDisplayItem item)
            {
                _selectedTask = item.Task;
                LoadCalendar();
                LoadStats();
            }
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

        private void Day_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Day click handler - can be extended for future functionality
        }

        private void LoadStats()
        {
            if (_selectedTask == null)
            {
                CheckInRateText.Text = "0%";
                RemainingDaysText.Text = "0";
                StreakDaysText.Text = "0天";
                return;
            }

            var taskService = new TaskService();
            var taskRepo = new TaskRepository();
            
            // Get fresh task data
            var freshTask = taskRepo.GetTaskById(_selectedTask.Id);
            if (freshTask == null)
            {
                CheckInRateText.Text = "0%";
                RemainingDaysText.Text = "0";
                StreakDaysText.Text = "0天";
                return;
            }
            
            // Calculate check-in rate from start date to today
            int totalDays = 0;
            int checkedDays = 0;
            var startDate = freshTask.StartDate ?? freshTask.CreatedAt;
            var endDate = DateTime.Today; // Only count up to today
            
            for (var date = startDate.Date; date <= endDate; date = date.AddDays(1))
            {
                bool shouldShow = false;
                bool isCompleted = false;
                
                if (freshTask.Type == TaskType.Quantitative)
                {
                    // For quantitative tasks, show on date range
                    if (freshTask.StartDate.HasValue && freshTask.EndDate.HasValue)
                        shouldShow = date.Date >= freshTask.StartDate.Value.Date && date.Date <= freshTask.EndDate.Value.Date;
                    else if (freshTask.StartDate.HasValue)
                        shouldShow = date.Date == freshTask.StartDate.Value.Date;
                    
                    // Only today counts as completed if there's progress
                    if (shouldShow && date.Date == DateTime.Today && freshTask.QuantitativeCurrent.HasValue && freshTask.QuantitativeCurrent > 0)
                        isCompleted = true;
                }
                else if (freshTask.Type == TaskType.Recurring && freshTask.RecurringPattern.HasValue)
                {
                    shouldShow = taskService.ShouldShowRecurringTaskOnDate(freshTask, date);
                    if (shouldShow)
                        isCompleted = taskService.IsRecurringTaskCompletedOnDate(freshTask, date);
                }
                
                if (shouldShow)
                {
                    totalDays++;
                    if (isCompleted) checkedDays++;
                }
            }
            
            double checkInRate = totalDays > 0 ? (double)checkedDays / totalDays * 100 : 0;
            CheckInRateText.Text = $"{checkInRate:F0}%";

            // Calculate remaining days
            int remainingDays = 0;
            if (freshTask.EndDate.HasValue)
            {
                remainingDays = Math.Max(0, (freshTask.EndDate.Value.Date - DateTime.Today).Days);
            }
            RemainingDaysText.Text = remainingDays.ToString();

            // Calculate streak days (consecutive days from today going backwards)
            int streakDays = 0;
            var currentDate = DateTime.Today;
            while (currentDate >= startDate.Date)
            {
                bool shouldShow = false;
                bool isCompleted = false;
                
                if (freshTask.Type == TaskType.Quantitative)
                {
                    if (freshTask.StartDate.HasValue && freshTask.EndDate.HasValue)
                        shouldShow = currentDate.Date >= freshTask.StartDate.Value.Date && currentDate.Date <= freshTask.EndDate.Value.Date;
                    else if (freshTask.StartDate.HasValue)
                        shouldShow = currentDate.Date == freshTask.StartDate.Value.Date;
                    
                    // Only today counts as completed if there's progress
                    if (shouldShow && currentDate.Date == DateTime.Today && freshTask.QuantitativeCurrent.HasValue && freshTask.QuantitativeCurrent > 0)
                        isCompleted = true;
                }
                else if (freshTask.Type == TaskType.Recurring && freshTask.RecurringPattern.HasValue)
                {
                    shouldShow = taskService.ShouldShowRecurringTaskOnDate(freshTask, currentDate);
                    if (shouldShow)
                        isCompleted = taskService.IsRecurringTaskCompletedOnDate(freshTask, currentDate);
                }
                
                if (shouldShow)
                {
                    if (isCompleted)
                        streakDays++;
                    else
                        break;
                }
                currentDate = currentDate.AddDays(-1);
            }
            StreakDaysText.Text = $"{streakDays}天";
        }

        private void LoadCalendar()
        {
            MonthTitle.Text = _currentMonth.ToString("yyyy年MM月");
            
            var taskService = new TaskService();
            var taskRepo = new TaskRepository();
            var days = new List<CalendarDayDisplay>();
            
            var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var startDay = firstDay.AddDays(-(int)firstDay.DayOfWeek + 1); // Start from Monday
            
            if (startDay > firstDay)
                startDay = startDay.AddDays(-7);

            // Get fresh task data from repository
            TaskItem freshTask = null;
            if (_selectedTask != null)
            {
                freshTask = taskRepo.GetTaskById(_selectedTask.Id);
            }

            for (int i = 0; i < 42; i++)
            {
                var date = startDay.AddDays(i);
                bool isCurrentMonth = date.Month == _currentMonth.Month;
                bool isToday = date.Date == DateTime.Today;
                bool shouldShow = false;
                bool isCompleted = false;
                
                if (freshTask != null)
                {
                    // For quantitative tasks
                    if (freshTask.Type == TaskType.Quantitative)
                    {
                        // Show on date range
                        if (freshTask.StartDate.HasValue && freshTask.EndDate.HasValue)
                        {
                            shouldShow = date.Date >= freshTask.StartDate.Value.Date && date.Date <= freshTask.EndDate.Value.Date;
                        }
                        else if (freshTask.StartDate.HasValue)
                        {
                            shouldShow = date.Date == freshTask.StartDate.Value.Date;
                        }
                        
                        // Only today turns green if there's any progress
                        if (shouldShow && isToday && freshTask.QuantitativeCurrent.HasValue && freshTask.QuantitativeCurrent > 0)
                        {
                            isCompleted = true;
                        }
                    }
                    // For recurring tasks
                    else if (freshTask.Type == TaskType.Recurring && freshTask.RecurringPattern.HasValue)
                    {
                        shouldShow = taskService.ShouldShowRecurringTaskOnDate(freshTask, date);
                        
                        if (shouldShow)
                        {
                            // For custom recurring tasks, check if any progress was made on this date
                            if (freshTask.RecurringPattern == RecurringPattern.Custom && 
                                freshTask.RecurringCurrentCount.HasValue && freshTask.RecurringCurrentCount > 0 &&
                                freshTask.LastCompletedDate.HasValue && freshTask.LastCompletedDate.Value.Date == date.Date)
                            {
                                isCompleted = true;
                            }
                            else
                            {
                                isCompleted = taskService.IsRecurringTaskCompletedOnDate(freshTask, date);
                            }
                        }
                    }
                }
                
                bool isPast = date.Date < DateTime.Today;
                
                var dayDisplay = new CalendarDayDisplay
                {
                    Date = date,
                    Day = date.Day.ToString(),
                    IsCurrentMonth = isCurrentMonth,
                    IsToday = isToday
                };

                if (!isCurrentMonth)
                {
                    // Other month days - gray
                    dayDisplay.BackgroundBrush = Brushes.Transparent;
                    dayDisplay.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 235));
                    dayDisplay.ForegroundBrush = new SolidColorBrush(Color.FromRgb(200, 200, 205));
                }
                else if (shouldShow && isCompleted)
                {
                    // Completed - green (including today if completed)
                    dayDisplay.BackgroundBrush = new SolidColorBrush(Color.FromRgb(52, 199, 89));
                    dayDisplay.BorderBrush = new SolidColorBrush(Color.FromRgb(52, 199, 89));
                    dayDisplay.ForegroundBrush = Brushes.White;
                }
                else if (isToday)
                {
                    // Today - blue border (only if not completed)
                    dayDisplay.BackgroundBrush = Brushes.Transparent;
                    dayDisplay.BorderBrush = (SolidColorBrush)FindResource("PrimaryBrush");
                    dayDisplay.ForegroundBrush = (SolidColorBrush)FindResource("TextBrush");
                }
                else if (shouldShow && isPast && !isCompleted)
                {
                    // Missed - red
                    dayDisplay.BackgroundBrush = new SolidColorBrush(Color.FromRgb(255, 59, 48));
                    dayDisplay.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 59, 48));
                    dayDisplay.ForegroundBrush = Brushes.White;
                }
                else if (shouldShow && !isPast)
                {
                    // Future task day - light blue border
                    dayDisplay.BackgroundBrush = Brushes.Transparent;
                    dayDisplay.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 255));
                    dayDisplay.ForegroundBrush = (SolidColorBrush)FindResource("TextBrush");
                }
                else
                {
                    // Normal day
                    dayDisplay.BackgroundBrush = Brushes.Transparent;
                    dayDisplay.BorderBrush = Brushes.Transparent;
                    dayDisplay.ForegroundBrush = isCurrentMonth 
                        ? (SolidColorBrush)FindResource("TextBrush") 
                        : new SolidColorBrush(Color.FromRgb(200, 200, 205));
                }

                days.Add(dayDisplay);
            }

            CalendarGrid.ItemsSource = days;
        }
    }

    public class TaskDisplayItem
    {
        public TaskItem Task { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TypeText { get; set; }
        public string TagName { get; set; }
        public SolidColorBrush TagColorBrush { get; set; }
        public Visibility TagVisibility { get; set; }
        public Visibility ProgressVisibility { get; set; }
        public double ProgressValue { get; set; }
        public string ProgressText { get; set; }
        public SolidColorBrush ProgressBrush { get; set; }
    }

    public class CalendarDayDisplay
    {
        public DateTime Date { get; set; }
        public string Day { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public Brush BackgroundBrush { get; set; }
        public Brush BorderBrush { get; set; }
        public Brush ForegroundBrush { get; set; }
    }
}
