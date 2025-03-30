using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SecondDiary.Models
{
    public class SystemPrompt
    {
        [JsonPropertyName("id")]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; } = null!;

        [JsonPropertyName("lines")]
        [JsonProperty(PropertyName = "lines")]
        public List<string> Lines { get; set; } = new List<string>();
    }
}