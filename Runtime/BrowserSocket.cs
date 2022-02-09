using System.Runtime.InteropServices;

namespace BananaParty.WebSocketClient
{
    public class BrowserSocket : ISocket
    {
        private readonly string _serverAddress;
        private readonly bool _useTextMessages;

        private int _socketIndex = -1;

        public BrowserSocket(string serverAddress, bool useTextMessages = false)
        {
            _serverAddress = serverAddress;
            _useTextMessages = useTextMessages;
        }

        public bool IsConnected => GetBrowserSocketIsConnected(_socketIndex);

        [DllImport("__Internal")]
        private static extern bool GetBrowserSocketIsConnected(int socketIndex);

        public bool HasUnreadPayloadQueue => GetBrowserSocketHasUnreadPayloadQueue(_socketIndex);

        [DllImport("__Internal")]
        private static extern bool GetBrowserSocketHasUnreadPayloadQueue(int socketIndex);

        public byte[] ReadPayloadQueue()
        {
            int payloadBytesCount = BrowserSocketReadPayloadQueue(_socketIndex, null, 0);
            byte[] payloadBytesBuffer = new byte[payloadBytesCount];
            BrowserSocketReadPayloadQueue(_socketIndex, payloadBytesBuffer, payloadBytesBuffer.Length);
            return payloadBytesBuffer;
        }

        /// <summary>
        /// Does not remove item from the queue if it's not going to fit in <paramref name="payloadBytesBufferLength"/>.
        /// </summary>
        /// <returns>Received bytes count.</returns>
        [DllImport("__Internal")]
        private static extern int BrowserSocketReadPayloadQueue(int socketIndex, byte[] payloadBytesBuffer, int payloadBytesBufferLength);

        public void Connect()
        {
            _socketIndex = BrowserSocketConnect(_serverAddress, _useTextMessages);
        }

        [DllImport("__Internal")]
        private static extern int BrowserSocketConnect(string serverAddress, bool useTextMessages);

        public void Send(byte[] payloadBytes)
        {
            BrowserSocketSend(_socketIndex, payloadBytes, payloadBytes.Length);
        }

        [DllImport("__Internal")]
        private static extern void BrowserSocketSend(int socketIndex, byte[] payloadBytes, int payloadBytesCount);

        public void Disconnect()
        {
            BrowserSocketDisconnect(_socketIndex);
        }

        [DllImport("__Internal")]
        private static extern void BrowserSocketDisconnect(int socketIndex);

        public void Dispose()
        {
            Disconnect();
        }
    }
}
