using System;

namespace TinyNakama
{
    [Serializable]
    public sealed class NakamaSyncResult
    {
        public int status;
        public string message;
    }

    abstract public class NakamaHelper
    {
        protected NakamaClient client;
        public NakamaHelper(NakamaClient client) => this.client = client;
    }

    public sealed class NakamaException : Exception
    {
        public long StatusCode { get; }
        public NakamaStatus Status { get; }
        public string ResponseBody { get; }
        public bool IsNetworkError { get; }
        internal NakamaException(string message, long statusCode, string responseBody, bool isNetworkError, NakamaStatus status = NakamaStatus.Unknown) : base(message)
        {
            StatusCode = statusCode; Status = status; ResponseBody = responseBody; IsNetworkError = isNetworkError;
        }
    }

    public enum NakamaStatus
    {
        NotEnough = -1, AlreadyExists = -3, Success = 0, Forbidden = 403, NotFound = 404, Unavailable = 503, Unknown = 999
    }
}