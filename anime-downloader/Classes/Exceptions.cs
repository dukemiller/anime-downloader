using System;
using System.Net;

namespace anime_downloader.Classes
{
    [Serializable]
    public class ServerProblemException : Exception
    {
        public ServerProblemException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}