using System;
using System.Collections.Generic;
using System.Linq;
using ME.Data;
using ME.Models;

namespace ME.Services
{
    public class TaskService
    {
        private readonly TaskRepository _repo;
        private readonly GoalService _goalService;

        public TaskService()
        {
            _repo = new TaskRepository();
            _goalService = new GoalService();
        }

        public List<TaskItem> GetAllTasks() => _repo.GetAllTasks();

        public List<TaskItem> GetTasksByGoalId(int goalId) => _repo.GetTasksByGoalId(goalId);

        public List<TaskItem> GetTasksByType(TaskType type) => _repo.GetTasksByType(type);

        public List<TaskItem> GetTodayTasks() => _repo.GetTodayTasks();

        public TaskItem GetTaskById(int id) => _repo.GetTaskById(id);

        public int CreateTask(TaskItem task) => _repo.InsertTask(task);

        public void UpdateTask(TaskItem task) => _repo.UpdateTask(task);

        public void DeleteTask(int id) => _repo.SoftDeleteTask(id);

        public void RestoreTask(int id) => _repo.RestoreTask(id);

        public void PermanentlyDeleteTask(int id) => _repo.PermanentlyDeleteTask(id);

        public void CompleteTask(int taskId)
        {
            var task = _repo.GetTaskById(taskId);
            if (task != null)
            {
                task.IsCompleted = true;
                task.CompletedAt = DateTime.Now;
                task.LastCompletedDate = DateTime.Today;
                _repo.UpdateTask(task);

                if (task.GoalId.HasValue)
                    RecalculateGoalProgress(task.GoalId.Value);
            }
        }

        public void UncompleteTask(int taskId)
        {
            var task = _repo.GetTaskById(taskId);
            if (task != null)
            {
                task.IsCompleted = false;
                task.CompletedAt = null;
                _repo.UpdateTask(task);

                if (task.GoalId.HasValue)
                    RecalculateGoalProgress(task.GoalId.Value);
            }
        }

        public void UpdateQuantitativeProgress(int taskId, double value)
        {
            var task = _repo.GetTaskById(taskId);
            if (task != null && task.QuantitativeMode.HasValue)
            {
                if (task.QuantitativeMode.Value == QuantitativeMode.Accumulate)
                    task.QuantitativeCurrent = (task.QuantitativeCurrent ?? 0) + value;
                else
                    task.QuantitativeCurrent = value;

                _repo.UpdateTask(task);

                if (task.GoalId.HasValue)
                    RecalculateGoalProgress(task.GoalId.Value);
            }
        }

        public bool ShouldShowRecurringTaskOnDate(TaskItem task, DateTime date)
        {
            if (task.Type != TaskType.Recurring || !task.RecurringPattern.HasValue)
                return false;

            // Check if task is within its date range
            if (task.StartDate.HasValue && date.Date < task.StartDate.Value.Date)
                return false;
            if (task.EndDate.HasValue && date.Date > task.EndDate.Value.Date)
                return false;

            // If no start date, use creation date
            if (!task.StartDate.HasValue && date.Date < task.CreatedAt.Date)
                return false;

            switch (task.RecurringPattern.Value)
            {
                case RecurringPattern.Daily:
                    return true;

                case RecurringPattern.Weekday:
                    return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;

                case RecurringPattern.Weekend:
                    return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

                case RecurringPattern.Weekly:
                    if (string.IsNullOrEmpty(task.RecurringDaysOfWeek))
                        return false;
                    var selectedDays = task.RecurringDaysOfWeek.Split(',').Select(int.Parse).ToList();
                    // Convert DayOfWeek (0=Sunday) to our format (0=Monday, 6=Sunday)
                    int dayIndex = ((int)date.DayOfWeek + 6) % 7;
                    return selectedDays.Contains(dayIndex);

                case RecurringPattern.Monthly:
                    if (task.IsLastDayOfMonth)
                    {
                        // Check if date is the last day of its month
                        var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
                        return date.Day == lastDay;
                    }
                    else if (task.RecurringDayOfMonth.HasValue)
                    {
                        return date.Day == task.RecurringDayOfMonth.Value;
                    }
                    return false;

                case RecurringPattern.Interval:
                    var startDate = task.StartDate ?? task.CreatedAt;
                    int interval = task.RecurringInterval ?? 1;
                    var daysDiff = (date.Date - startDate.Date).Days;
                    return daysDiff >= 0 && daysDiff % interval == 0;

                case RecurringPattern.Custom:
                    // Custom tasks show every day
                    return true;

                default:
                    return false;
            }
        }

        public bool IsRecurringTaskCompletedOnDate(TaskItem task, DateTime date)
        {
            if (task.Type != TaskType.Recurring)
                return task.IsCompleted;

            // For custom recurring tasks, check if current count reached target
            if (task.RecurringPattern == RecurringPattern.Custom && task.RecurringTargetCount.HasValue && task.RecurringTargetCount > 1)
            {
                // If completed today and last completed date matches
                if (task.IsCompleted && task.LastCompletedDate.HasValue && task.LastCompletedDate.Value.Date == date.Date)
                    return true;
                
                // Check if current count reached target for today
                if (task.RecurringCurrentCount.HasValue && task.RecurringTargetCount.HasValue && 
                    task.RecurringCurrentCount >= task.RecurringTargetCount &&
                    task.LastCompletedDate.HasValue && task.LastCompletedDate.Value.Date == date.Date)
                    return true;
                
                return false;
            }

            // For other recurring tasks, check if completed on this specific date
            if (task.LastCompletedDate.HasValue && task.LastCompletedDate.Value.Date == date.Date)
                return true;

            return false;
        }

        public List<TaskItem> GetTasksForDate(DateTime date)
        {
            var allTasks = _repo.GetAllTasks();
            var result = new List<TaskItem>();

            foreach (var task in allTasks)
            {
                if (task.IsDeleted) continue;

                // For recurring tasks, check if they should show on this date
                if (task.Type == TaskType.Recurring && task.RecurringPattern.HasValue)
                {
                    if (ShouldShowRecurringTaskOnDate(task, date))
                    {
                        result.Add(task);
                    }
                }
                // For non-recurring tasks, check date range
                else if (task.StartDate.HasValue && task.EndDate.HasValue)
                {
                    if (task.StartDate.Value.Date <= date.Date && task.EndDate.Value.Date >= date.Date)
                        result.Add(task);
                }
                else if (task.StartDate.HasValue)
                {
                    if (task.StartDate.Value.Date == date.Date)
                        result.Add(task);
                }
                else if (task.CreatedAt.Date == date.Date)
                {
                    result.Add(task);
                }
            }

            return result;
        }

        private void RecalculateGoalProgress(int goalId)
        {
            var tasks = _repo.GetTasksByGoalId(goalId);
            if (tasks.Count == 0) return;

            int completed = 0;
            foreach (var t in tasks)
            {
                if (t.IsCompleted) completed++;
            }

            double progress = (double)completed / tasks.Count * 100;
            _goalService.UpdateGoalProgress(goalId, progress);
        }
    }
}
