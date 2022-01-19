using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BananaParty.WebSocketClient.Tests
{
    public class SocketTests
    {
        private const float ConnectTimeoutThreshold = 3;
        private const float ReceiveTimeoutThreshold = 5;
        private const float DisconnectTimeoutThreshold = 3;

        private Socket _socket;

        [UnitySetUp]
        public IEnumerator ConnectToServer()
        {
            // If this echo test site dies, use "ws://ws.ifelse.io"
            _socket = new("ws://echo.websocket.events");
            Assert.IsFalse(_socket.IsConnected, $"{nameof(_socket.IsConnected)} is {true} immediately after creation.");
            _socket.Connect();
            Assert.IsFalse(_socket.HasUnreadPayloadQueue, $"{nameof(_socket.HasUnreadPayloadQueue)} is {true} immediately after creation.");
            yield return new WaitWhile(() => !_socket.IsConnected, ConnectTimeoutThreshold);
            Assert.IsTrue(_socket.IsConnected, $"{nameof(_socket.Connect)} did not flip {nameof(_socket.IsConnected)} to {true} within {nameof(ConnectTimeoutThreshold)} of {ConnectTimeoutThreshold} seconds.");
        }

        [UnityTest]
        public IEnumerator ShouldEchoSmallPackets()
        {
            yield return SkipFirstMessage();

            byte[] bytesToSend = new byte[] { 1, 0, 42, 228, 255, 0 };
            yield return TestEcho(bytesToSend);
        }

        [UnityTest]
        public IEnumerator ShouldEchoSequenceOfPackets()
        {
            yield return SkipFirstMessage();

            byte[] bytesToSend = new byte[4096];
            var random = new Random();
            for (int byteIterator = 0; byteIterator < bytesToSend.Length; byteIterator += 1)
                bytesToSend[byteIterator] = (byte)random.Next(255);

            yield return TestEcho(bytesToSend);

            bytesToSend = new byte[1234];
            for (int byteIterator = 0; byteIterator < bytesToSend.Length; byteIterator += 1)
                bytesToSend[byteIterator] = (byte)random.Next(255);

            yield return TestEcho(bytesToSend);
        }

        [UnityTest]
        public IEnumerator ShouldEchoHugePackets()
        {
            yield return SkipFirstMessage();

            byte[] bytesToSend = new byte[80000];
            var random = new Random();
            for (int byteIterator = 0; byteIterator < bytesToSend.Length; byteIterator += 1)
                bytesToSend[byteIterator] = (byte)random.Next(255);

            yield return TestEcho(bytesToSend);
        }

        /// <summary>
        /// Those services always send some trash message immediately as you connect.
        /// </summary>
        private IEnumerator SkipFirstMessage()
        {
            yield return new WaitWhile(() => !_socket.HasUnreadPayloadQueue, ReceiveTimeoutThreshold);
            Assert.IsTrue(_socket.HasUnreadPayloadQueue, "Service advertising trash message did not arrive.");
            _socket.ReadPayloadQueue();
        }

        private IEnumerator TestEcho(byte[] bytesToSend)
        {
            _socket.Send(bytesToSend);
            yield return new WaitWhile(() => !_socket.HasUnreadPayloadQueue, ReceiveTimeoutThreshold);
            Assert.IsTrue(_socket.HasUnreadPayloadQueue, $"Timeout waiting for message. {_socket.HasUnreadPayloadQueue} did not flip to {true}.");
            byte[] receivedBytes = _socket.ReadPayloadQueue();
            Assert.IsTrue(bytesToSend.SequenceEqual(receivedBytes), $"Received corrupted data from echo. Expected {bytesToSend.Length} bytes, but received {receivedBytes.Length}.");
        }

        [UnityTearDown]
        public IEnumerator Disconnect()
        {
            _socket.Disconnect();
            yield return new WaitWhile(() => _socket.IsConnected, DisconnectTimeoutThreshold);
            Assert.IsFalse(_socket.IsConnected, $"{nameof(_socket.Disconnect)} did not flip {nameof(_socket.IsConnected)} to {false} within {nameof(DisconnectTimeoutThreshold)} of {DisconnectTimeoutThreshold} seconds.");
        }
    }
}
