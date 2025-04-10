// filepath: c:\src\seconddiary\SecondDiary.Service\Models\Recommendation.cs
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SecondDiary.Models
{
    public class Recommendation
    {
        [JsonPropertyName("id")]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("userId")]
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        [JsonProperty(PropertyName = "date")]
        public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;

        [JsonPropertyName("text")]
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; } = string.Empty;
    }
}
