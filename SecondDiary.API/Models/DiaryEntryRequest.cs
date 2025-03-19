namespace SecondDiary.API.Models
{
    public class DiaryEntryRequest
    {
        public string? Context { get; set; }
        public string Thought { get; set; } = string.Empty;
    }
} 