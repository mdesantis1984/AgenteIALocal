using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgenteIALocal.Core.Interfaces.AI
{
    /// <summary>
    /// Abstraction for any AI provider.
    /// Read-only descriptor of provider capabilities and an execution method.
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Provider display name (e.g., "LocalMock", "OpenAI").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Available models exposed by the provider.
        /// </summary>
        IReadOnlyCollection<IAIModel> AvailableModels { get; }

        /// <summary>
        /// Execute an inference/request against the provider.
        /// Implementations should be read-only and side-effect free aside from telemetry/logging.
        /// </summary>
        Task<IAIResponse> ExecuteAsync(IAIRequest request, CancellationToken cancellationToken);
    }
}
