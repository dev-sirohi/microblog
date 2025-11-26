using System.Net.Http.Headers;

namespace Microblog.Api.ExternalProviders
{
    internal sealed class OpenAIEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private readonly string _model = "text-embedding-3-small";
        private readonly string _endpoint = "https://api.openai.com/v1/embeddings";

        internal OpenAIEmbeddingProvider(HttpClient http, string openAiApiKey)
        {
            _httpClient = http;
            _openAiApiKey = openAiApiKey;
        }

        public async Task<float[]> GetEmbeddingAsync(string input, CancellationToken cancellationToken = default)
        {
            dynamic request = new
            {
                model = _model,
                input,
            };

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            httpRequest.Content = new StringContent(CommonUtils.TransformTo<string>(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode(); // throws exception if not successful

            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                dynamic? result = await System.Text.Json.JsonSerializer.DeserializeAsync<dynamic>(stream, cancellationToken: cancellationToken);
                if (result == null || result!.data == null || result!.data.Count == 0)
                {
                    throw new Exception("Failed to get embedding from OpenAI.");
                }

                return CommonUtils.TransformTo<float[]>(result!.data[0].embedding);
            }
        }
    }
}
