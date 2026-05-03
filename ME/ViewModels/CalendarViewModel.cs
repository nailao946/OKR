using System;
using System.Collections.ObjectModel;
using ME.Core;
using ME.Models;

namespace ME.ViewModels
{
    public class CalendarViewModel : ViewModelBase
    {
        private DateTime _selectedDate;
        private ObservableCollection<TaskItem> _dayTasks;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                    LoadDayTasks();
            }
        }

        public ObservableCollection<TaskItem> DayTasks
        {
            get => _dayTasks;
            set => SetProperty(ref _dayTasks, value);
        }

        private readonly Services.TaskService _taskService;

        public CalendarViewModel()
        {
            _taskService = new Services.TaskService();
            _selectedDate = DateTime.Today;
            DayTasks = new ObservableCollection<TaskItem>();
            LoadDayTasks();
        }

        private void LoadDayTasks()
        {
            DayTasks.Clear();
            var allTasks = _taskService.GetAllTasks();

            foreach (var task in allTasks)
            {
                if (task.StartDate.HasValue && task.StartDate.Value.Date <= SelectedDate.Date &&
                    task.EndDate.HasValue && task.EndDate.Value.Date >= SelectedDate.Date)
                {
                    DayTasks.Add(task);
                }
                else if (!task.StartDate.HasValue && !task.EndDate.HasValue && task.Type == TaskType.OneTime)
                {
                    if (task.CreatedAt.Date == SelectedDate.Date)
                        DayTasks.Add(task);
                }
            }
        }
    }
}
