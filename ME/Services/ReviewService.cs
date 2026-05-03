using System;
using System.Collections.Generic;
using ME.Data;
using ME.Models;

namespace ME.Services
{
    public class ReviewService
    {
        private readonly ReviewRepository _repo;
        private readonly GoalService _goalService;
        private readonly TaskService _taskService;

        public ReviewService()
        {
            _repo = new ReviewRepository();
            _goalService = new GoalService();
            _taskService = new TaskService();
        }

        public List<Review> GetAllReviews() => _repo.GetAllReviews();

        public Review GenerateWeeklyReview()
        {
            var allGoals = _goalService.GetAllGoals();
            double totalProgress = 0;
            int count = 0;

            foreach (var g in allGoals)
            {
                totalProgress += g.Progress;
                count++;
            }

            var completionRate = count > 0 ? totalProgress / count : 0;
            var allTasks = _taskService.GetAllTasks();
            int delayedTasks = 0;
            int totalActiveTasks = 0;

            foreach (var t in allTasks)
            {
                if (!t.IsCompleted && t.EndDate.HasValue && t.EndDate.Value < DateTime.Now)
                    delayedTasks++;
                if (!t.IsCompleted)
                    totalActiveTasks++;
            }

            var delayRatio = totalActiveTasks > 0 ? (double)delayedTasks / totalActiveTasks : 0;

            return new Review
            {
                Type = ReviewType.Weekly,
                ReviewDate = DateTime.Now,
                CompletionRate = completionRate,
                DelayRatio = delayRatio,
                CreatedAt = DateTime.Now
            };
        }

        public Review GenerateGoalClosedReview(int goalId)
        {
            var goal = _goalService.GetGoalById(goalId);
            var tasks = _taskService.GetTasksByGoalId(goalId);

            int completed = 0;
            foreach (var t in tasks)
            {
                if (t.IsCompleted) completed++;
            }

            var completionRate = tasks.Count > 0 ? (double)completed / tasks.Count * 100 : 0;

            return new Review
            {
                Type = ReviewType.GoalClosed,
                GoalId = goalId,
                ReviewDate = DateTime.Now,
                CompletionRate = completionRate,
                CreatedAt = DateTime.Now
            };
        }

        public int SaveReview(Review review) => _repo.InsertReview(review);
    }
}
