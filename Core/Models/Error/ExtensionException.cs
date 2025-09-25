using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.Error
{
    public class ExtensionException : Exception
    {
        public ExtensionException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }

    public class ExtensionNotRegisteredException : ExtensionException
    {
        public ExtensionNotRegisteredException(Exception innerException = null) : base("Extension is not registered. Please register the extension before using it.", innerException)
        {
        }
    }

    public class ExtensionHttpException : ExtensionException
    {
        public HttpMethod HttpMethod { get; set; }
        public string Endpoint { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ResponseContent { get; set; }
        public ExtensionHttpException(
            HttpMethod httpMethod,
            string endpoint,
            HttpStatusCode statusCode, 
            string responseContent,
            Exception innerException = null
        ) : base($"HTTP request failed with status code {statusCode}", innerException)
        {
            HttpMethod = httpMethod;
            Endpoint = endpoint;
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}
