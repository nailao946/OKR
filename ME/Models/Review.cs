using System;

namespace ME.Models
{
    public enum ReviewType
    {
        Weekly,
        Monthly,
        GoalClosed
    }

    public class Review
    {
        public int Id { get; set; }
        public ReviewType Type { get; set; }
        public int? GoalId { get; set; }
        public DateTime ReviewDate { get; set; }
        public double CompletionRate { get; set; }
        public double DelayRatio { get; set; }
        public string TimeImbalance { get; set; }
        public string DecomposeIssues { get; set; }
        public string OptimizationSuggestions { get; set; }
        public string SuccessReasons { get; set; }
        public string FailureReasons { get; set; }
        public string Strengths { get; set; }
        public string Weaknesses { get; set; }
        public string PersonalNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
