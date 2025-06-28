namespace SecondDiary.Models
{
    public class CosmosDbSettings
    {
        public required string Endpoint { get; set; }
        public required string Key { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string DiaryEntriesContainerName { get; set; } = string.Empty;
        public string SystemPromptsContainerName { get; set; } = string.Empty;
        public string EmailSettingsContainerName { get; set; } = string.Empty;
        public string RecommendationsContainerName { get; set; } = string.Empty;
        public string PersonalAccessTokensContainerName { get; set; } = string.Empty;
    }
}