using System;
using System.Collections.Generic;
using System.Linq;
using ME.Models;

namespace ME.Data
{
    public class GoalRepository
    {
        private const string FileName = "goals";

        public List<Goal> GetAllGoals(bool includeDeleted = false)
        {
            var goals = JsonStore.Load<Goal>(FileName);
            return includeDeleted ? goals : goals.Where(g => !g.IsDeleted).OrderBy(g => g.SortOrder).ThenByDescending(g => g.CreatedAt).ToList();
        }

        public List<Goal> GetGoalsByTimeFrame(GoalTimeFrame timeFrame)
        {
            return JsonStore.Load<Goal>(FileName)
                .Where(g => !g.IsDeleted && g.TimeFrame == timeFrame)
                .OrderByDescending(g => g.CreatedAt).ToList();
        }

        public Goal GetGoalById(int id)
        {
            return JsonStore.Load<Goal>(FileName).FirstOrDefault(g => g.Id == id);
        }

        public List<Goal> GetChildGoals(int parentId)
        {
            return JsonStore.Load<Goal>(FileName)
                .Where(g => !g.IsDeleted && g.ParentId == parentId)
                .OrderBy(g => g.CreatedAt).ToList();
        }

        public int InsertGoal(Goal goal)
        {
            var goals = JsonStore.Load<Goal>(FileName);
            var maxId = goals.Count > 0 ? goals.Max(g => g.Id) : 0;
            goal.Id = maxId + 1;
            goal.CreatedAt = goal.CreatedAt == default ? DateTime.Now : goal.CreatedAt;
            goal.UpdatedAt = DateTime.Now;
            goals.Add(goal);
            JsonStore.Save(FileName, goals);
            return goal.Id;
        }

        public void UpdateGoal(Goal goal)
        {
            var goals = JsonStore.Load<Goal>(FileName);
            var index = goals.FindIndex(g => g.Id == goal.Id);
            if (index >= 0)
            {
                goal.UpdatedAt = DateTime.Now;
                goals[index] = goal;
                JsonStore.Save(FileName, goals);
            }
        }

        public void SoftDeleteGoal(int id)
        {
            var goals = JsonStore.Load<Goal>(FileName);
            var goal = goals.FirstOrDefault(g => g.Id == id);
            if (goal != null)
            {
                goal.IsDeleted = true;
                goal.DeletedAt = DateTime.Now;
                JsonStore.Save(FileName, goals);
            }
        }

        public void RestoreGoal(int id)
        {
            var goals = JsonStore.Load<Goal>(FileName);
            var goal = goals.FirstOrDefault(g => g.Id == id);
            if (goal != null)
            {
                goal.IsDeleted = false;
                goal.DeletedAt = default;
                JsonStore.Save(FileName, goals);
            }
        }

        public void PermanentlyDeleteGoal(int id)
        {
            var goals = JsonStore.Load<Goal>(FileName);
            goals.RemoveAll(g => g.Id == id);
            JsonStore.Save(FileName, goals);
        }
    }
}
