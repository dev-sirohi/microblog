namespace Microblog.Api.Utils
{
    public class AppException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public AppException() : base()
        {
            StatusCode = HttpStatusCode.BadRequest;
        }
        public AppException(string message) : base(message)
        {
            StatusCode = HttpStatusCode.BadRequest;
        }
        public AppException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
        public AppException(string message, AppException innerException, HttpStatusCode statusCode = HttpStatusCode.BadRequest) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
