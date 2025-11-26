namespace Microblog.Api.Interfaces.ProviderInterfaces
{
    internal interface IEmbeddingProvider
    {
        Task<float[]> GetEmbeddingAsync(string input, CancellationToken cancellationToken = default);
    }
}
