using System;
using System.Threading.Tasks;

namespace SecondDiary.API.Services
{
    public class SystemPromptService
    {
        private string _systemPrompt = "You are a helpful AI assistant that provides thoughtful recommendations based on diary entries.";

        public Task<string> GetSystemPromptAsync()
        {
            return Task.FromResult(_systemPrompt);
        }

        public Task SetSystemPromptAsync(string newPrompt)
        {
            _systemPrompt = newPrompt;
            return Task.CompletedTask;
        }

        public Task AppendToSystemPromptAsync(string additionalPrompt)
        {
            _systemPrompt += Environment.NewLine + additionalPrompt;
            return Task.CompletedTask;
        }
    }
} 