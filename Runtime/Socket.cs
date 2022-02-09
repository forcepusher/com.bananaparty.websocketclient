using System;

namespace BananaParty.WebSocketClient
{
    /// <summary>
    /// Universal default implementation of <see cref="ISocket"/>.
    /// </summary>
    /// <remarks>
    /// Internally is an <see cref="ISocket"/> Proxy for <see cref="StandaloneSocket"/> and <see cref="BrowserSocket"/>.
    /// </remarks>
    public class Socket : ISocket
    {
        private readonly string _serverAddress;
        private readonly bool _useTextMessages;

        private ISocket _webSocketClient;

        public Socket(string serverAddress, bool useTextMessages = false)
        {
            _serverAddress = serverAddress;
            _useTextMessages = useTextMessages;
        }

        public bool IsConnected => _webSocketClient != null && _webSocketClient.IsConnected;

        public bool HasUnreadPayloadQueue => _webSocketClient?.HasUnreadPayloadQueue ?? false;

        public byte[] ReadPayloadQueue() => _webSocketClient?.ReadPayloadQueue() ?? throw new InvalidOperationException($"Trying to use {nameof(ReadPayloadQueue)} before calling {nameof(Connect)}.");

        /// <remarks>
        /// This is not a Factory Method anti-pattern, since it doesn't return anything.
        /// </remarks>
        public void Connect()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _webSocketClient = new BrowserSocket(_serverAddress, _useTextMessages);
#else
            _webSocketClient = new StandaloneSocket(_serverAddress, _useTextMessages);
#endif

            _webSocketClient.Connect();
        }

        public void Send(byte[] payloadBytes)
        {
            if (!IsConnected)
                throw new InvalidOperationException($"Trying to use {nameof(Send)} while not {nameof(IsConnected)}.");

            _webSocketClient.Send(payloadBytes);
        }

        public void Disconnect()
        {
            if (_webSocketClient == null)
                throw new InvalidOperationException($"Trying to use {nameof(Disconnect)} before calling {nameof(Connect)}.");

            _webSocketClient.Disconnect();
        }

        public void Dispose()
        {
            if (_webSocketClient != null)
                _webSocketClient.Dispose();
        }
    }
}
