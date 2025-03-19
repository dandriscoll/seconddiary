using System;

namespace SecondDiary.API.Models
{
    public class DiaryEntry
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Context { get; set; }
        public string Thought { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
} 