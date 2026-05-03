using System.Collections.ObjectModel;
using ME.Core;
using ME.Models;

namespace ME.ViewModels
{
    public class MapViewModel : ViewModelBase
    {
        private ObservableCollection<Goal> _rootGoals;
        private Goal _selectedGoal;

        public ObservableCollection<Goal> RootGoals
        {
            get => _rootGoals;
            set => SetProperty(ref _rootGoals, value);
        }

        public Goal SelectedGoal
        {
            get => _selectedGoal;
            set => SetProperty(ref _selectedGoal, value);
        }

        private readonly Services.GoalService _goalService;

        public MapViewModel()
        {
            _goalService = new Services.GoalService();
            RootGoals = new ObservableCollection<Goal>();
            LoadRootGoals();
        }

        public void LoadRootGoals()
        {
            RootGoals.Clear();
            var allGoals = _goalService.GetAllGoals();

            foreach (var g in allGoals)
            {
                if (!g.ParentId.HasValue)
                    RootGoals.Add(g);
            }
        }
    }
}
