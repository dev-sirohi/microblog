using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Microblog.Api.Utils;

public class CommonUtils
{
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new AppException("Cannot create hash for empty password");

        string passwordHash = Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(password)
            )
        );

        return passwordHash;
    }

    public static T? TransformTo<T>(object? obj)
    {
        if (obj is null) throw new ArgumentNullException($"Could not convert null object {nameof(obj)}");

        string json = JsonConvert.SerializeObject(obj);
        var result = JsonConvert.DeserializeObject<T>(json);

        return result;
    }

    public static string BuildMediaUrl(string relativePath)
    {
        return $"{AppConstants.BASE_URL}{relativePath}";
    }

    public class ControllerResponseParams
    {
        public bool Success { get; set; } = true;
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}