using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class UnityHttpClient : IHttpClient
    {
        private IConnection _connection;

        public void Initialize(IConnection connection)
        {
            _connection = connection;
        }

        public Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            var request = new UnityHttpRequest("GET", url)
            {
                synchronous = true
            };

            var requestWrapper = new UnityHttpRequestWrapper(request);
            prepareRequest(requestWrapper);

            request.Send();

            if (request.response != null)
            {
                return Task.Factory.StartNew(() => (IResponse)new UnityResponseWrapper(request.response));
            }

            if (request.exception != null)
            {
                throw request.exception;
            }
            
            throw new UnityHttpException("Fail to send request.");
        }

        public Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
        {
            var request = new UnityHttpRequest("POST", url, postData)
            {
                synchronous = true
            };

            var requestWrapper = new UnityHttpRequestWrapper(request);
            prepareRequest(requestWrapper);

            request.Send();

            if (request.response != null)
            {
                return Task.Factory.StartNew(() => (IResponse)new UnityResponseWrapper(request.response));
            }

            if (request.exception != null)
            {
                throw request.exception;
            }

            throw new UnityHttpException("Fail to send request.");
        }
        private void PrepareClientRequest(UnityHttpRequestWrapper req)
        {
            if (_connection.CookieContainer != null)
            {
                req.CookieContainer = _connection.CookieContainer;
            }

            // TODO: credentials and proxy
        }
    }
}
