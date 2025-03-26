using System.Text.Json.Serialization;

namespace SecondDiary.API.Models
{
    public class SystemPrompt
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("promptLines")]
        public List<string> PromptLines { get; set; } = new List<string> { 
            "You are a helpful AI assistant that provides thoughtful recommendations based on diary entries." 
        };
    }
}
