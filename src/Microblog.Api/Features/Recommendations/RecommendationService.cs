using System.Runtime.InteropServices;
using Microblog.Api.Infrastructure.Caching;
using Microblog.Api.Interfaces.ProviderInterfaces;
using Microblog.Api.Models;

namespace Microblog.Api.Features.Recommendations;

/// <summary>
/// Computes embedding-based recommendations using cosine similarity.
/// Embeddings are stored as raw float bytes in <see cref="Post.EmbeddingData"/>.
/// Results are cached in Redis for 10 minutes.
/// For production scale, replace the in-memory similarity search with Azure AI Search or pgvector.
/// </summary>
public sealed class RecommendationService(
    AppDbContext db,
    IEmbeddingProvider embeddingProvider,
    ICacheService cache,
    ILogger<RecommendationService> logger) : IRecommendationService
{
    private static readonly TimeSpan RecommendationCacheTtl = TimeSpan.FromMinutes(10);

    public async Task<List<Post>> GetRecommendationsAsync(long postId, int limit = 5, CancellationToken ct = default)
    {
        string cacheKey = $"recommendations:{postId}:{limit}";

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var targetPost = await db.Posts.FindAsync(new object[] { postId }, ct);
            if (targetPost?.EmbeddingData is null || targetPost.EmbeddingData.Length == 0)
                return [];

            float[] targetEmbedding = DeserializeEmbedding(targetPost.EmbeddingData);

            // Fetch candidate posts that have embeddings
            var candidates = await db.Posts
                .Where(p => p.Id != postId && p.EmbeddingData != null && p.EmbeddingData.Length > 0)
                .Select(p => new { p.Id, p.EmbeddingData })
                .ToListAsync(ct);

            var scored = candidates
                .Select(p => (Post: p, Score: CosineSimilarity(targetEmbedding, DeserializeEmbedding(p.EmbeddingData!))))
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.Post.Id)
                .ToList();

            return await db.Posts.Where(p => scored.Contains(p.Id)).ToListAsync(ct);
        }, RecommendationCacheTtl, ct) ?? [];
    }

    public async Task ComputeAndStoreEmbeddingAsync(long postId, string content, CancellationToken ct = default)
    {
        try
        {
            float[] embedding = await embeddingProvider.GetEmbeddingAsync(content, ct);
            byte[] bytes = SerializeEmbedding(embedding);

            var post = await db.Posts.FindAsync(new object[] { postId }, ct);
            if (post is null) return;

            post.EmbeddingData = bytes;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Stored embedding for post {PostId}", postId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to compute embedding for post {PostId}", postId);
        }
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB) + 1e-10f);
    }

    private static byte[] SerializeEmbedding(float[] embedding)
    {
        byte[] bytes = new byte[embedding.Length * sizeof(float)];
        MemoryMarshal.Cast<float, byte>(embedding).CopyTo(bytes);
        return bytes;
    }

    private static float[] DeserializeEmbedding(byte[] bytes)
    {
        float[] embedding = new float[bytes.Length / sizeof(float)];
        MemoryMarshal.Cast<byte, float>(bytes).CopyTo(embedding);
        return embedding;
    }
}
