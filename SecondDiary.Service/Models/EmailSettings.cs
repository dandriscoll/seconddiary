using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SecondDiary.Models
{
    public class EmailSettings
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("userId")]
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("email")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("preferredTime")]
        [JsonPropertyName("preferredTime")]
        public TimeSpan PreferredTime { get; set; } = new TimeSpan(9, 0, 0); // Default to 9:00 AM

        [JsonProperty("isEnabled")]
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        [JsonProperty("lastEmailSent")]
        [JsonPropertyName("lastEmailSent")]
        public DateTime? LastEmailSent { get; set; }

        [JsonProperty("timeZone")]
        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; } = "UTC"; // Default to UTC timezone
    }
}
