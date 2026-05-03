using System.Collections.ObjectModel;
using System.Windows.Input;
using ME.Core;
using ME.Models;

namespace ME.ViewModels
{
    public class RecycleBinViewModel : ViewModelBase
    {
        private ObservableCollection<Goal> _deletedGoals;
        private ObservableCollection<TaskItem> _deletedTasks;

        public ObservableCollection<Goal> DeletedGoals
        {
            get => _deletedGoals;
            set => SetProperty(ref _deletedGoals, value);
        }

        public ObservableCollection<TaskItem> DeletedTasks
        {
            get => _deletedTasks;
            set => SetProperty(ref _deletedTasks, value);
        }

        public ICommand RestoreGoalCommand { get; }
        public ICommand PermanentlyDeleteGoalCommand { get; }
        public ICommand RestoreTaskCommand { get; }
        public ICommand PermanentlyDeleteTaskCommand { get; }

        private readonly Services.GoalService _goalService;
        private readonly Services.TaskService _taskService;

        public RecycleBinViewModel()
        {
            _goalService = new Services.GoalService();
            _taskService = new Services.TaskService();
            DeletedGoals = new ObservableCollection<Goal>();
            DeletedTasks = new ObservableCollection<TaskItem>();
            LoadDeletedItems();

            RestoreGoalCommand = new RelayCommand(p => RestoreGoal(p as Goal));
            PermanentlyDeleteGoalCommand = new RelayCommand(p => PermanentlyDeleteGoal(p as Goal));
            RestoreTaskCommand = new RelayCommand(p => RestoreTask(p as TaskItem));
            PermanentlyDeleteTaskCommand = new RelayCommand(p => PermanentlyDeleteTask(p as TaskItem));
        }

        public void LoadDeletedItems()
        {
            DeletedGoals.Clear();
            DeletedTasks.Clear();

            var goalRepo = new Data.GoalRepository();
            var taskRepo = new Data.TaskRepository();

            var allGoals = goalRepo.GetAllGoals(true);
            foreach (var g in allGoals)
            {
                if (g.IsDeleted)
                    DeletedGoals.Add(g);
            }

            var allTasks = taskRepo.GetAllTasks(true);
            foreach (var t in allTasks)
            {
                if (t.IsDeleted)
                    DeletedTasks.Add(t);
            }
        }

        private bool CanRestoreGoal(object param) => param is Goal;
        private bool CanRestoreTask(object param) => param is TaskItem;

        private void RestoreGoal(Goal goal)
        {
            if (goal != null)
            {
                _goalService.RestoreGoal(goal.Id);
                DeletedGoals.Remove(goal);
            }
        }

        private void PermanentlyDeleteGoal(Goal goal)
        {
            if (goal != null)
            {
                _goalService.PermanentlyDeleteGoal(goal.Id);
                DeletedGoals.Remove(goal);
            }
        }

        private void RestoreTask(TaskItem task)
        {
            if (task != null)
            {
                _taskService.RestoreTask(task.Id);
                DeletedTasks.Remove(task);
            }
        }

        private void PermanentlyDeleteTask(TaskItem task)
        {
            if (task != null)
            {
                _taskService.PermanentlyDeleteTask(task.Id);
                DeletedTasks.Remove(task);
            }
        }
    }
}
