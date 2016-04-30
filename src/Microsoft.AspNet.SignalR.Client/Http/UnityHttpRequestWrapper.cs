using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class UnityHttpRequestWrapper : IRequest
    {
        private readonly UnityHttpRequest httpRequest;

        public UnityHttpRequestWrapper(UnityHttpRequest httpRequest)
        {
            this.httpRequest = httpRequest;
        }

        public string Accept
        {
            get
            {
                return httpRequest.GetHeader("Accept");
            }

            set
            {
                httpRequest.SetHeader("Accept", value);
            }
        }

        public string UserAgent
        {
            get
            {
                return httpRequest.GetHeader("User-Agent");
            }

            set
            {
                httpRequest.SetHeader("User-Agent", value);
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                return httpRequest.cookieJar;
            }
            set
            {
                httpRequest.cookieJar = value;
            }
        }

        public void Abort()
        {
            this.httpRequest.Abort();
        }

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            foreach (KeyValuePair<string, string> headerEntry in headers)
            {
                httpRequest.SetHeader(headerEntry.Key, headerEntry.Value);
            }
        }
    }
}
