namespace Microblog.Api.Interfaces.ProviderInterfaces;

public interface IEmbeddingProvider
{
    Task<float[]> GetEmbeddingAsync(string input, CancellationToken cancellationToken = default);
}