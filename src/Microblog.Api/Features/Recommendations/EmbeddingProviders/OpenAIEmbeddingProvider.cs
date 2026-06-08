using Microblog.Api.Interfaces.ProviderInterfaces;
using OpenAI.Embeddings;
using Microblog.Api.Infrastructure.Observability;

namespace Microblog.Api.Features.Recommendations.EmbeddingProviders;

/// <summary>
/// Generates text embeddings using OpenAI's <c>text-embedding-3-small</c> model (1 536 dimensions).
/// Requires <c>OpenAI:ApiKey</c> in configuration.
/// </summary>
internal sealed class OpenAIEmbeddingProvider : IEmbeddingProvider
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<OpenAIEmbeddingProvider> _logger;

    public OpenAIEmbeddingProvider(IConfiguration config, ILogger<OpenAIEmbeddingProvider> logger)
    {
        _logger = logger;
        string apiKey = config["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is required when Features:EnableEmbeddings is true");
        string model = config["OpenAI:Model"] ?? "text-embedding-3-small";
        _client = new EmbeddingClient(model, new System.ClientModel.ApiKeyCredential(apiKey));
    }

    public async Task<float[]> GetEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _client.GenerateEmbeddingAsync(input, cancellationToken: cancellationToken);
            AppMetrics.EmbeddingsGenerated.Inc();
            return result.Value.ToFloats().ToArray();
        }
        catch (Exception ex)
        {
            AppMetrics.EmbeddingErrors.Inc();
            _logger.LogError(ex, "OpenAI embedding generation failed for input of length {Len}", input.Length);
            throw;
        }
    }
}
