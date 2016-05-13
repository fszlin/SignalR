using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNet.SignalR.Client.Http;
using WebSocketSharp;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    internal class WebSocketWrapperRequest : IRequest
    {
        private readonly WebSocket _clientWebSocket;
        private readonly IConnection _connection;

        public WebSocketWrapperRequest(WebSocket clientWebSocket, IConnection connection)
        {
            _clientWebSocket = clientWebSocket;
            _connection = connection;
            PrepareRequest();
        }

        public string Accept
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        public string UserAgent
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        public void Abort()
        {
        }

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            foreach (var headerEntry in headers)
            {
                _connection.Trace(TraceLevels.Messages, "Add header {0} to WebScoket.", headerEntry.Key);
                this._clientWebSocket.Headers.Add(headerEntry.Key, headerEntry.Value);
            }
        }
        private void PrepareRequest()
        {
            if (_connection.CookieContainer != null)
            {
                //TODO: Add CookieContainer support
            }

            if (_connection.Credentials != null)
            {
                //TODO: Add Credentials support
            }

            if (_connection.Proxy != null)
            {
                //TODO: Add Proxy support
            }
        }
    }

}
