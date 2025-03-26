namespace SecondDiary.Tests.Models
{
    // Test models - use these instead of the API models in test code
    public class TestSystemPrompt
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
    }

    public class TestLineUpdate
    {
        public int LineNumber { get; set; }
        public string Content { get; set; }
    }
}
