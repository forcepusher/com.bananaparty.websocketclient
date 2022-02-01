using System;

namespace BananaParty.WebSocketClient
{
    public class Socket : ISocket
    {
        private readonly string _serverAddress;

        private ISocket _webSocketClient;

        public Socket(string serverAddress)
        {
            _serverAddress = serverAddress;
        }

        public bool IsConnected => _webSocketClient != null && _webSocketClient.IsConnected;

        public bool HasUnreadPayloadQueue => _webSocketClient != null ? _webSocketClient.HasUnreadPayloadQueue : throw new InvalidOperationException($"Trying to use {nameof(HasUnreadPayloadQueue)} before calling {nameof(Connect)}.");

        public byte[] ReadPayloadQueue() => _webSocketClient != null ? _webSocketClient.ReadPayloadQueue() : throw new InvalidOperationException($"Trying to use {nameof(ReadPayloadQueue)} before calling {nameof(Connect)}.");

        public void Connect()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _webSocketClient = new BrowserSocket(_serverAddress);
#else
            _webSocketClient = new StandaloneSocket(_serverAddress);
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
