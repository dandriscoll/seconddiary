using System.Text.Json.Serialization;

namespace SecondDiary.Models
{
    public class SystemPrompt
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("lines")]
        public List<string> Lines { get; set; } = new List<string>();
    }
}
