// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
#if !NET35
using Microsoft.AspNet.SignalR.Client.Transports.WebSockets;
#else
using WebSocketSharp;
#endif

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : ClientTransportBase
    {
#if !NET35
        private readonly ClientWebSocketHandler _webSocketHandler;
#endif
        private CancellationToken _disconnectToken;
        private IConnection _connection;
        private string _connectionData;
        private CancellationTokenSource _webSocketTokenSource;
#if !NET35
        private ClientWebSocket _webSocket;
#else
        private WebSocket _webSocket;
#endif
        private int _disposed;

        public WebSocketTransport()
            : this(new DefaultHttpClient())
        {
        }

        public WebSocketTransport(IHttpClient client)
            : base(client, "webSockets")
        {
            _disconnectToken = CancellationToken.None;
            ReconnectDelay = TimeSpan.FromSeconds(2);
#if !NET35
            _webSocketHandler = new ClientWebSocketHandler(this);
#endif
        }

#if !NET35
        // intended for testing
        internal WebSocketTransport(ClientWebSocketHandler webSocketHandler)
            : this()
        {
            _webSocketHandler = webSocketHandler;
        }
#endif

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        /// <summary>
        /// Indicates whether or not the transport supports keep alive
        /// </summary>
        public override bool SupportsKeepAlive
        {
            get { return true; }
        }

        protected override void OnStart(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            _disconnectToken = disconnectToken;
            _connection = connection;
            _connectionData = connectionData;

            // We don't need to await this task
            PerformConnect().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    TransportFailed(task.Exception);
                }
                else if (task.IsCanceled)
                {
                    TransportFailed(null);
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);
        }

        // For testing
        public virtual Task PerformConnect()
        {
            return PerformConnect(UrlBuilder.BuildConnect(_connection, Name, _connectionData));
        }

#if !NET35
        private async Task PerformConnect(string url)
#else
        private Task PerformConnect(string url)
#endif
        {
            var uri = UrlBuilder.ConvertToWebSocketUri(url);

            _connection.Trace(TraceLevels.Events, "WS Connecting to: {0}", uri);

            // TODO: Revisit thread safety of this assignment
            _webSocketTokenSource = new CancellationTokenSource();
#if !NET35
            _webSocket = new ClientWebSocket();

            _connection.PrepareRequest(new WebSocketWrapperRequest(_webSocket, _connection));

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_webSocketTokenSource.Token, _disconnectToken);
            CancellationToken token = linkedCts.Token;

            await _webSocket.ConnectAsync(uri, token);
            await _webSocketHandler.ProcessWebSocketRequestAsync(_webSocket, token);
#else
            _webSocket = new WebSocket(uri.AbsoluteUri);

            _connection.PrepareRequest(new WebSocketWrapperRequest(_webSocket, _connection));
            
            _webSocket.OnOpen += (sender, e) => this.OnOpen();
            _webSocket.OnClose += (sender, e) => this.OnClose();
            _webSocket.OnMessage += (sender, e) => this.OnMessage(e.Data);
            _webSocket.OnError += (sender, e) => this.OnError(e.Exception);
            
            _webSocket.Connect();
            return TaskAsyncHelper.Empty;
#endif
        }

        protected override void OnStartFailed()
        {
            // if the transport failed to start we want to stop it silently.
            Dispose();
        }

        public override Task Send(IConnection connection, string data, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

#if !NET35
            // If we don't throw here when the WebSocket isn't open, WebSocketHander.SendAsync will noop.
            if (_webSocketHandler.WebSocket.State != WebSocketState.Open)
#else
            if (_webSocket.ReadyState != WebSocketState.Open)
#endif
            {
                // Make this a faulted task and trigger the OnError even to maintain consistency with the HttpBasedTransports
                var ex = new InvalidOperationException(Resources.Error_DataCannotBeSentDuringWebSocketReconnect);
                connection.OnError(ex);
                return TaskAsyncHelper.FromError(ex);
            }

#if !NET35
            return _webSocketHandler.SendAsync(data);
#else
            _webSocket.Send(data);
            return TaskAsyncHelper.Empty;
#endif
        }

        // virtual for testing
        internal virtual void OnMessage(string message)
        {
            _connection.Trace(TraceLevels.Messages, "WS: OnMessage({0})", message);

            ProcessResponse(_connection, message);
        }

        // virtual for testing
        internal virtual void OnOpen()
        {
            // This will noop if we're not in the reconnecting state
            if (_connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
            {
                _connection.OnReconnected();
            }
        }

        // virtual for testing
        internal virtual void OnClose()
        {
            _connection.Trace(TraceLevels.Events, "WS: OnClose()");

            if (_disconnectToken.IsCancellationRequested)
            {
                return;
            }

            if (AbortHandler.TryCompleteAbort())
            {
                return;
            }

            DoReconnect();
        }

        // fire and forget
#if !NET35
        private async void DoReconnect()
#else
        private void DoReconnect()
#endif
        {
            var reconnectUrl = UrlBuilder.BuildReconnect(_connection, Name, _connectionData);

#if !NET35
            while (TransportHelper.VerifyLastActive(_connection) && _connection.EnsureReconnecting())
#else
            if (TransportHelper.VerifyLastActive(_connection) && _connection.EnsureReconnecting())
#endif
            {
                try
                {
#if !NET35
                    await PerformConnect(reconnectUrl);
                    break;
#else
                    PerformConnect(reconnectUrl);
                    return;
#endif
                }
                catch (OperationCanceledException)
                {
#if !NET35
                    break;
#else
                    return;
#endif
                }
                catch (Exception ex)
                {
                    if (ExceptionHelper.IsRequestAborted(ex))
                    {
#if !NET35
                        break;
#else
                        return;
#endif
                    }

                    _connection.OnError(ex);
                }

#if !NET35
                await Task.Delay(ReconnectDelay);
#else
                TaskAsyncHelper.Delay(ReconnectDelay)
                    .Then(() => DoReconnect());
#endif
            }
        }

        // virtual for testing
        internal virtual void OnError(Exception error)
        {
            _connection.OnError(error);
        }

        public override void LostConnection(IConnection connection)
        {
            _connection.Trace(TraceLevels.Events, "WS: LostConnection");

            if (_webSocketTokenSource != null)
            {
                _webSocketTokenSource.Cancel();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 1)
                {
                    base.Dispose(disposing);
                    return;
                }

                if (_webSocketTokenSource != null)
                {
                    // Gracefully close the websocket message loop
                    _webSocketTokenSource.Cancel();
                }

                if (_webSocket != null)
                {
#if !NET35
                    _webSocket.Dispose();
#else
                    using (_webSocket) { }
#endif
                }

                if (_webSocketTokenSource != null)
                {
                    _webSocketTokenSource.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
