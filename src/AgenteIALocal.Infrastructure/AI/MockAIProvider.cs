using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Interfaces.AI;
using AgenteIALocal.Core.Models.AI;

namespace AgenteIALocal.Infrastructure.AI
{
    /// <summary>
    /// Deterministic mock implementation of IAIProvider for offline testing and development.
    /// No external dependencies, no randomness, returns predictable responses.
    /// </summary>
    public class MockAIProvider : IAIProvider
    {
        private readonly List<IAIModel> models;

        public MockAIProvider()
        {
            Name = "MockAI";
            models = new List<IAIModel>
            {
                new MockModel("mock-default", "Mock Default", Name)
            };
        }

        public string Name { get; }

        public IReadOnlyCollection<IAIModel> AvailableModels => models.AsReadOnly();

        public Task<IAIResponse> ExecuteAsync(IAIRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Deterministic transformation: echo prompt with metadata.
            var prompt = request.Prompt ?? string.Empty;
            var modelId = request.Model?.Id ?? "mock-default";
            var content = $"[MockAI:{modelId}] {prompt}";

            var response = new MockAIResponse
            {
                Content = content,
                IsSuccess = true,
                ErrorMessage = null,
                Duration = TimeSpan.FromMilliseconds(50)
            };

            return Task.FromResult((IAIResponse)response);
        }

        private class MockModel : IAIModel
        {
            public MockModel(string id, string displayName, string provider)
            {
                Id = id;
                DisplayName = displayName;
                Provider = provider;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Provider { get; }
        }

        private class MockAIResponse : IAIResponse
        {
            public string Content { get; set; }
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; }
            public TimeSpan Duration { get; set; }
        }
    }
}
