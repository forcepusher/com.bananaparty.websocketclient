using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace BananaParty.WebSocketClient
{
    public class StandaloneSocket : ISocket
    {
        private const int MaxPayloadChunkSize = 1024;

        private readonly Uri _serverUri;
        private readonly bool _useTextMessages;

        private readonly ClientWebSocket _clientWebSocket = new();
        private readonly CancellationTokenSource _disconnectTokenSource = new();

        private readonly Queue<byte[]> _payloadQueue = new();

        public bool IsConnected => _clientWebSocket.State == WebSocketState.Open;

        public bool HasUnreadPayloadQueue => _payloadQueue.Count > 0;

        public byte[] ReadPayloadQueue() => _payloadQueue.Dequeue();

        public StandaloneSocket(string serverAddress, bool useTextMessages = false)
        {
            _serverUri = new Uri(serverAddress);
            _useTextMessages = useTextMessages;
        }

        public void Connect()
        {
            ConnectAndReceiveLoopAsync();
        }

        public void Disconnect()
        {
            _disconnectTokenSource.Cancel();
        }

        public void Send(byte[] payloadBytes)
        {
            SendAsync(payloadBytes);
        }

        private async void SendAsync(byte[] payloadBytes)
        {
            if (!IsConnected)
                throw new InvalidOperationException($"Connection is not open. State = {_clientWebSocket.State}");

            int payloadBytesSent = 0;
            while (payloadBytesSent < payloadBytes.Length)
            {
                var payloadBytesSegment = new ArraySegment<byte>(payloadBytes, payloadBytesSent, Math.Min(payloadBytes.Length - payloadBytesSent, MaxPayloadChunkSize));
                bool isFinalChunk = payloadBytesSegment.Offset + payloadBytesSegment.Count >= payloadBytes.Length;
                await _clientWebSocket.SendAsync(payloadBytesSegment, _useTextMessages ? WebSocketMessageType.Text : WebSocketMessageType.Binary, isFinalChunk, _disconnectTokenSource.Token);
                payloadBytesSent += payloadBytesSegment.Count;
            }
        }

        private async void ConnectAndReceiveLoopAsync()
        {
            Task connectTask = _clientWebSocket.ConnectAsync(_serverUri, _disconnectTokenSource.Token);

            // Workaround for "ObjectDisposedException: Cannot access a disposed object".
            while (!connectTask.IsCompleted)
            {
                await Task.Yield();

                if (_disconnectTokenSource.IsCancellationRequested)
                    goto ConnectionAborted;
            }

            if (!connectTask.IsCompletedSuccessfully)
                goto ConnectionAborted;

            byte[] payloadBytesBuffer = new byte[MaxPayloadChunkSize];
            WebSocketReceiveResult result;
            do
            {
                var payloadWriter = new ArrayBufferWriter<byte>();
                do
                {
                    Task<WebSocketReceiveResult> receiveTask = _clientWebSocket.ReceiveAsync(payloadBytesBuffer, _disconnectTokenSource.Token);
                    // Workaround for bug where it awaits forever if server connection is gone.
                    while (!receiveTask.IsCompleted)
                    {
                        await Task.Yield();

                        // Workaround for "The WebSocket is in an invalid state ('Aborted') for this operation".
                        if (_clientWebSocket.State == WebSocketState.Aborted)
                            goto ConnectionAborted;
                    }

                    // Workaround for "Operation was cancelled".
                    if (_disconnectTokenSource.IsCancellationRequested)
                        goto DisconnectRequested;

                    result = receiveTask.Result;

                    payloadWriter.Write(new ArraySegment<byte>(payloadBytesBuffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                _payloadQueue.Enqueue(payloadWriter.WrittenSpan.ToArray());
            }
            while (result.MessageType != WebSocketMessageType.Close);

        DisconnectRequested:
            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

        ConnectionAborted:
            _clientWebSocket.Dispose();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
