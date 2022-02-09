using System;
using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BananaParty.WebSocketClient.Tests
{
    public class TextSocketTests
    {
        private const float ConnectTimeoutThreshold = 3;
        private const float ReceiveTimeoutThreshold = 5;
        private const float DisconnectTimeoutThreshold = 3;

        private Socket _socket;

        [UnitySetUp]
        public IEnumerator ConnectToServer()
        {
            // If this echo test site dies, use "ws://ws.ifelse.io"
            _socket = new("ws://echo.websocket.events", true);

            Assert.IsFalse(_socket.IsConnected, $"{nameof(_socket.IsConnected)} is {true} immediately after creation.");

            _socket.Connect();

            Assert.IsFalse(_socket.HasUnreadPayloadQueue, $"{nameof(_socket.HasUnreadPayloadQueue)} is {true} immediately after creation.");

            yield return new WaitWhile(() => !_socket.IsConnected, ConnectTimeoutThreshold);

            Assert.IsTrue(_socket.IsConnected, $"{nameof(_socket.Connect)} did not flip {nameof(_socket.IsConnected)} to {true} within {nameof(ConnectTimeoutThreshold)} of {ConnectTimeoutThreshold} seconds.");
        }

        [UnityTest]
        public IEnumerator ShouldEchoSmallMessage()
        {
            yield return SkipFirstMessage();

            byte[] bytesToSend = Encoding.UTF8.GetBytes("henlo");

            yield return TestEcho(bytesToSend);
        }

        [UnityTest]
        public IEnumerator ShouldEchoSequenceOfMessages()
        {
            yield return SkipFirstMessage();
            yield return TestEcho(GenerateRandomTextBytes(2048));
            yield return TestEcho(GenerateRandomTextBytes(600));
        }

        [UnityTest]
        public IEnumerator ShouldEchoHugeMessage()
        {
            yield return SkipFirstMessage();
            yield return TestEcho(GenerateRandomTextBytes(40000));
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

            Assert.IsTrue(_socket.HasUnreadPayloadQueue, $"Timeout waiting for message. {nameof(_socket.HasUnreadPayloadQueue)} did not flip to {true}.");

            byte[] receivedBytes = _socket.ReadPayloadQueue();

            Assert.IsTrue(bytesToSend.SequenceEqual(receivedBytes), $"Received corrupted data from echo. Expected {bytesToSend.Length} bytes, but received {receivedBytes.Length}.");
        }

        private byte[] GenerateRandomTextBytes(int length)
        {
            string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            var textToSend = new StringBuilder(length);
            var random = new Random();
            for (int characterIterator = 0; characterIterator < length; characterIterator += 1)
                textToSend.Append(characters[random.Next(characters.Length)]);

            return Encoding.UTF8.GetBytes(textToSend.ToString());
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
