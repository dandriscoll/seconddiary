using System.Text.Json.Serialization;

namespace SecondDiary.API.Models
{
    public class DiaryEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("thought")]
        public string? Thought { get; set; }

        [JsonPropertyName("encryptedThought")]
        public string? EncryptedThought { get; set; }

        [JsonPropertyName("context")]
        public string? Context { get; set; }

        [JsonPropertyName("encryptedContext")]
        public string? EncryptedContext { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }
}