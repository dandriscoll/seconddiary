namespace SecondDiary.Models
{
    public class CosmosDbSettings
    {
        public required string Endpoint { get; set; }
        public required string Key { get; set; }
        public required string DatabaseName { get; set; }
        public required string DiaryEntriesContainerName { get; set; }
        public required string SystemPromptsContainerName { get; set; }
    }
}