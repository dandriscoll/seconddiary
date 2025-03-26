using SecondDiary.API.Models;
using SecondDiary.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecondDiary.Tests.Mocks
{
    // Mock service implementation for testing with built-in repository behavior
    public class SystemPromptService : ISystemPromptService
    {
        private const string DefaultSystemPromptLine = "You are a helpful AI assistant that provides thoughtful recommendations based on diary entries.";
        
        // Internal storage for prompts
        private readonly Dictionary<string, SystemPrompt> _prompts = new Dictionary<string, SystemPrompt>();
        private readonly Dictionary<string, string> _userPrompts = new Dictionary<string, string>();

        public SystemPromptService()
        {
            // Initialize with some default prompts for testing
            SystemPrompt defaultPrompt = new SystemPrompt 
            { 
                Id = "system-default", 
                UserId = "system",
                PromptLines = new List<string> { DefaultSystemPromptLine }
            };
            
            _prompts["system-default"] = defaultPrompt;
        }

        public async Task<string> GetSystemPromptAsync(string userId)
        {
            string userPromptKey = $"{userId}-systemprompt";
            
            if (_userPrompts.TryGetValue(userId, out string prompt))
                return prompt;
                
            if (_prompts.TryGetValue(userPromptKey, out SystemPrompt systemPrompt))
                return string.Join(Environment.NewLine, systemPrompt.PromptLines);
            
            return DefaultSystemPromptLine;
        }

        public Task AddLineToPromptAsync(string userId, string line)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(line))
                return Task.CompletedTask;
                
            string userPromptKey = $"{userId}-systemprompt";
            
            // Check if we have a user prompt string
            if (_userPrompts.TryGetValue(userId, out string existingPrompt))
            {
                _userPrompts[userId] = String.Concat(existingPrompt, Environment.NewLine, line);
                return Task.CompletedTask;
            }
            
            // Check if we have a system prompt object
            if (_prompts.TryGetValue(userPromptKey, out SystemPrompt systemPrompt))
            {
                if (systemPrompt.PromptLines == null)
                    systemPrompt.PromptLines = new List<string>();
                
                systemPrompt.PromptLines.Add(line);
                return Task.CompletedTask;
            }
            
            // Create new prompt for this user
            _userPrompts[userId] = line;
            return Task.CompletedTask;
        }
        
        public Task RemoveLineAsync(string userId, string line)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(line))
                return Task.CompletedTask;
                
            string userPromptKey = $"{userId}-systemprompt";
            
            // Check if we have a user prompt string
            if (_userPrompts.TryGetValue(userId, out string existingPrompt))
            {
                List<string> lines = existingPrompt.Split(Environment.NewLine).ToList();
                if (lines.Remove(line))
                    _userPrompts[userId] = string.Join(Environment.NewLine, lines);
                    
                return Task.CompletedTask;
            }
            
            // Check if we have a system prompt object
            if (_prompts.TryGetValue(userPromptKey, out SystemPrompt systemPrompt))
            {
                if (systemPrompt.PromptLines != null)
                    systemPrompt.PromptLines.Remove(line);
                
                return Task.CompletedTask;
            }
            
            return Task.CompletedTask;
        }

        public Task<SystemPrompt> GetSystemPromptByIdAsync(string promptId)
        {
            if (_prompts.TryGetValue(promptId, out SystemPrompt prompt))
                return Task.FromResult(prompt);
                
            return Task.FromResult<SystemPrompt>(null);
        }

        public Task<bool> UpdateSystemPromptAsync(string promptId, SystemPrompt prompt)
        {
            if (string.IsNullOrEmpty(promptId) || !_prompts.ContainsKey(promptId))
                return Task.FromResult(false);
                
            _prompts[promptId] = prompt;
            return Task.FromResult(true);
        }

        public Task<List<SystemPrompt>> ListSystemPromptsAsync()
        {
            return Task.FromResult(_prompts.Values.ToList());
        }

        public Task<SystemPrompt> CreateSystemPromptAsync(SystemPrompt prompt)
        {
            if (prompt == null)
                throw new ArgumentNullException(nameof(prompt));
                
            // Generate ID if not provided
            if (string.IsNullOrEmpty(prompt.Id))
                prompt.Id = Guid.NewGuid().ToString();
                
            _prompts[prompt.Id] = prompt;
            return Task.FromResult(prompt);
        }

        public Task<bool> DeleteSystemPromptAsync(string promptId)
        {
            if (string.IsNullOrEmpty(promptId) || !_prompts.ContainsKey(promptId))
                return Task.FromResult(false);
                
            _prompts.Remove(promptId);
            return Task.FromResult(true);
        }
        
        // Helper methods for tests to interact with internal storage directly
        
        public void AddTestPrompt(SystemPrompt prompt)
        {
            if (prompt == null || string.IsNullOrEmpty(prompt.Id))
                throw new ArgumentException("Prompt must have an ID", nameof(prompt));
                
            _prompts[prompt.Id] = prompt;
        }
        
        public void AddTestUserPrompt(string userId, string promptContent)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
                
            _userPrompts[userId] = promptContent;
        }
        
        public void ClearTestData()
        {
            _prompts.Clear();
            _userPrompts.Clear();
            
            // Re-add the default prompt
            SystemPrompt defaultPrompt = new SystemPrompt 
            { 
                Id = "system-default",
                UserId = "system",
                PromptLines = new List<string> { DefaultSystemPromptLine }
            };
            
            _prompts["system-default"] = defaultPrompt;
        }
        
        public int GetPromptCount()
        {
            return _prompts.Count;
        }
        
        public int GetUserPromptCount()
        {
            return _userPrompts.Count;
        }
    }
}
