using System.Collections.ObjectModel;
using ME.Core;
using ME.Models;

namespace ME.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private double _completionRate;
        private int _totalGoals;
        private int _totalTasks;
        private int _completedTasks;
        private double _totalFocusHours;

        public double CompletionRate
        {
            get => _completionRate;
            set => SetProperty(ref _completionRate, value);
        }

        public int TotalGoals
        {
            get => _totalGoals;
            set => SetProperty(ref _totalGoals, value);
        }

        public int TotalTasks
        {
            get => _totalTasks;
            set => SetProperty(ref _totalTasks, value);
        }

        public int CompletedTasks
        {
            get => _completedTasks;
            set => SetProperty(ref _completedTasks, value);
        }

        public double TotalFocusHours
        {
            get => _totalFocusHours;
            set => SetProperty(ref _totalFocusHours, value);
        }

        private readonly Services.GoalService _goalService;
        private readonly Services.TaskService _taskService;
        private readonly Services.FocusTimerService _focusService;

        public DashboardViewModel()
        {
            _goalService = new Services.GoalService();
            _taskService = new Services.TaskService();
            _focusService = new Services.FocusTimerService();
            LoadData();
        }

        public void LoadData()
        {
            var goals = _goalService.GetAllGoals();
            var tasks = _taskService.GetAllTasks();

            TotalGoals = goals.Count;
            TotalTasks = tasks.Count;
            CompletedTasks = 0;

            double totalProgress = 0;
            foreach (var g in goals)
            {
                totalProgress += g.Progress;
            }

            foreach (var t in tasks)
            {
                if (t.IsCompleted)
                    CompletedTasks++;
            }

            CompletionRate = TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
            TotalFocusHours = 0;
        }
    }
}
