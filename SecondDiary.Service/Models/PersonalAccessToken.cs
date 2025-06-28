using Newtonsoft.Json;

namespace SecondDiary.Models
{
    public class PersonalAccessToken
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("tokenPrefix")]
        public string TokenPrefix { get; set; } = string.Empty;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; } = true;

        public bool IsValid => IsActive;
    }

    public class CreatePersonalAccessTokenRequest
    {
        // No properties needed - just create a token
    }

    public class CreatePersonalAccessTokenResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string TokenPrefix { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Warning message to show only once
        public string Warning { get; set; } = "This token will only be shown once. Please copy it now.";
    }

    public class PersonalAccessTokenSummary
    {
        public string Id { get; set; } = string.Empty;
        public string TokenPrefix { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
