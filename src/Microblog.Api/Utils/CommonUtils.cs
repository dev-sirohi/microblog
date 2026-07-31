using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Microblog.Api.Utils;

public class AppException : Exception
{
    public AppException() => StatusCode = HttpStatusCode.BadRequest;

    public AppException(string message) : base(message) => StatusCode = HttpStatusCode.BadRequest;

    public AppException(string message, HttpStatusCode statusCode) : base(message) => StatusCode = statusCode;

    public HttpStatusCode StatusCode { get; set; }
}

public class CommonUtils
{
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new AppException("Cannot create hash for empty password");

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
    }

    public static T? TransformTo<T>(object? obj)
    {
        if (obj is null) throw new ArgumentNullException($"Could not convert null object {nameof(obj)}");

        string json = JsonConvert.SerializeObject(obj);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public class ControllerResponseParams
    {
        public bool Success { get; set; } = true;
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
