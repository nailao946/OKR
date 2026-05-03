using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ME.Core;
using ME.Models;

namespace ME.ViewModels
{
    public class TasksViewModel : ViewModelBase
    {
        private ObservableCollection<TaskItem> _tasks;
        private TaskItem _selectedTask;
        private int _currentViewMode;

        public ObservableCollection<TaskItem> Tasks
        {
            get => _tasks;
            set => SetProperty(ref _tasks, value);
        }

        public TaskItem SelectedTask
        {
            get => _selectedTask;
            set => SetProperty(ref _selectedTask, value);
        }

        public int CurrentViewMode
        {
            get => _currentViewMode;
            set => SetProperty(ref _currentViewMode, value);
        }

        public ICommand AddTaskCommand { get; }
        public ICommand CompleteTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand UpdateProgressCommand { get; }

        private readonly Services.TaskService _taskService;
        private int? _filterGoalId;

        public TasksViewModel()
        {
            _taskService = new Services.TaskService();
            Tasks = new ObservableCollection<TaskItem>();
            LoadTasks();

            AddTaskCommand = new RelayCommand(_ => AddTask());
            CompleteTaskCommand = new RelayCommand(_ => CompleteTask(), _ => SelectedTask != null);
            DeleteTaskCommand = new RelayCommand(_ => DeleteTask(), _ => SelectedTask != null);
            EditTaskCommand = new RelayCommand(_ => EditTask(), _ => SelectedTask != null);
            UpdateProgressCommand = new RelayCommand(_ => UpdateProgress(), _ => SelectedTask != null && SelectedTask.QuantitativeMode.HasValue);
        }

        public void LoadTasks()
        {
            Tasks.Clear();
            var tasks = _filterGoalId.HasValue
                ? _taskService.GetTasksByGoalId(_filterGoalId.Value)
                : _taskService.GetAllTasks();

            foreach (var t in tasks)
                Tasks.Add(t);
        }

        public void ReloadTasks()
        {
            var currentId = SelectedTask?.Id;
            LoadTasks();
            if (currentId.HasValue)
                SelectedTask = Tasks.FirstOrDefault(t => t.Id == currentId.Value);
        }

        public void FilterByGoal(int goalId)
        {
            _filterGoalId = goalId;
            LoadTasks();
        }

        private void AddTask()
        {
            var newTask = new TaskItem
            {
                Title = "新任务",
                Type = TaskType.OneTime,
                GoalId = _filterGoalId,
                Priority = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var id = _taskService.CreateTask(newTask);
            newTask.Id = id;
            Tasks.Insert(0, newTask);
            SelectedTask = newTask;
        }

        private void CompleteTask()
        {
            if (SelectedTask != null)
            {
                _taskService.CompleteTask(SelectedTask.Id);
                SelectedTask.IsCompleted = true;
                SelectedTask.CompletedAt = DateTime.Now;
            }
        }

        private void DeleteTask()
        {
            if (SelectedTask != null)
            {
                _taskService.DeleteTask(SelectedTask.Id);
                Tasks.Remove(SelectedTask);
                SelectedTask = null;
            }
        }

        private void EditTask()
        {
            if (SelectedTask != null)
            {
                SelectedTask.UpdatedAt = DateTime.Now;
                _taskService.UpdateTask(SelectedTask);
            }
        }

        private void UpdateProgress()
        {
            if (SelectedTask != null && SelectedTask.QuantitativeMode.HasValue)
            {
                _taskService.UpdateQuantitativeProgress(SelectedTask.Id, 1);
            }
        }
    }
}
