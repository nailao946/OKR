using System.Collections.Generic;
using ME.Data;
using ME.Models;

namespace ME.Services
{
    public class GoalService
    {
        private readonly GoalRepository _repo;

        public GoalService()
        {
            _repo = new GoalRepository();
        }

        public List<Goal> GetAllGoals() => _repo.GetAllGoals();

        public List<Goal> GetGoalsByTimeFrame(GoalTimeFrame tf) => _repo.GetGoalsByTimeFrame(tf);

        public Goal GetGoalById(int id) => _repo.GetGoalById(id);

        public List<Goal> GetChildGoals(int parentId) => _repo.GetChildGoals(parentId);

        public int CreateGoal(Goal goal) => _repo.InsertGoal(goal);

        public void UpdateGoal(Goal goal) => _repo.UpdateGoal(goal);

        public void DeleteGoal(int id) => _repo.SoftDeleteGoal(id);

        public void RestoreGoal(int id) => _repo.RestoreGoal(id);

        public void PermanentlyDeleteGoal(int id) => _repo.PermanentlyDeleteGoal(id);

        public void UpdateGoalProgress(int goalId, double progress)
        {
            var goal = _repo.GetGoalById(goalId);
            if (goal != null)
            {
                goal.Progress = progress;
                _repo.UpdateGoal(goal);
            }
        }
    }
}
