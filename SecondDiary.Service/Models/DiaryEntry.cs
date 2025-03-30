using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SecondDiary.Models
{
    public class DiaryEntry
    {
        [JsonPropertyName("id")]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("userId")]
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("thought")]
        [JsonProperty(PropertyName = "thought")]
        public string? Thought { get; set; }

        [JsonPropertyName("encryptedThought")]
        [JsonProperty(PropertyName = "encryptedThought")]
        public string? EncryptedThought { get; set; }

        [JsonPropertyName("context")]
        [JsonProperty(PropertyName = "context")]
        public string? Context { get; set; }

        [JsonPropertyName("encryptedContext")]
        [JsonProperty(PropertyName = "encryptedContext")]
        public string? EncryptedContext { get; set; }
    }
}