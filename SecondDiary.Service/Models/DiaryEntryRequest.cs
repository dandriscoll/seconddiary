namespace SecondDiary.Models
{
    public class DiaryEntryRequest(string? context = null, string thought = "")
    {
        public string? Context { get; set; } = context;
        public string Thought { get; set; } = thought;
    }
}