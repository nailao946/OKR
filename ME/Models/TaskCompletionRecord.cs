using System;

namespace ME.Models
{
    public class TaskCompletionRecord
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Date { get; set; } // "yyyy-MM-dd"
        public DateTime CompletedAt { get; set; }
    }
}
