using System;

namespace ME.Models
{
    public enum TimerMode
    {
        Stopwatch,
        Countdown
    }

    public class FocusSession
    {
        public int Id { get; set; }
        public int? GoalId { get; set; }
        public int? TaskId { get; set; }
        public TimerMode Mode { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public string Notes { get; set; }
    }
}
