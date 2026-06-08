using Microblog.Api.Models;

namespace Microblog.Api.Features.Recommendations;

/// <summary>Returns similar posts for a given post using embedding-based cosine similarity.</summary>
public interface IRecommendationService
{
    /// <summary>Returns up to <paramref name="limit"/> posts similar to <paramref name="postId"/>.</summary>
    Task<List<Post>> GetRecommendationsAsync(long postId, int limit = 5, CancellationToken ct = default);

    /// <summary>Generates and persists the embedding for <paramref name="postId"/> asynchronously.</summary>
    Task ComputeAndStoreEmbeddingAsync(long postId, string content, CancellationToken ct = default);
}
