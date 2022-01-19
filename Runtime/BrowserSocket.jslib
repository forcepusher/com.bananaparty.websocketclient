const library = {

  // Class definition.

  $browserSocket: {
    sockets: [],

    getBrowserSocketIsConnected: function (socketIndex) {
      return browserSocket.sockets[socketIndex].webSocket.readyState === WebSocket.OPEN;
    },

    getBrowserSocketHasUnreadPayloadQueue: function (socketIndex) {
      return browserSocket.sockets[socketIndex].payloadQueue.length > 0;
    },

    browserSocketReadPayloadQueue: function (socketIndex, payloadBytesBufferPtr, payloadBytesBufferLength) {
      const payloadBytesCount = browserSocket.sockets[socketIndex].payloadQueue[0].length;
      if (payloadBytesBufferLength < payloadBytesCount)
        return payloadBytesCount;

      const payloadBytes = browserSocket.sockets[socketIndex].payloadQueue.shift();
      HEAPU8.set(payloadBytes, payloadBytesBufferPtr);
      return payloadBytesCount;
    },

    browserSocketConnect: function (serverAddress) {
      const webSocket = new WebSocket(serverAddress);
      webSocket.binaryType = 'arraybuffer';

      const payloadQueue = [];

      webSocket.onmessage = function (messageEvent) {
        if (messageEvent.data instanceof ArrayBuffer) {
          payloadQueue.push(new Uint8Array(messageEvent.data));
        } else if (typeof messageEvent.data === 'string') {
          payloadQueue.push(new TextEncoder().encode(messageEvent.data));
        } else if (messageEvent.data instanceof Blob) {
          console.error('Blob message type not supported. messageEvent.data=' + messageEvent.data);
        } else {
          console.error('Unknown message type not supported. messageEvent.data=' + messageEvent.data);
        }
      }

      const socket = { webSocket: webSocket, payloadQueue: payloadQueue };

      const socketIndex = browserSocket.sockets.push(socket) - 1;
      return socketIndex;
    },

    browserSocketSend: function (socketIndex, payloadBytes) {
      browserSocket.sockets[socketIndex].webSocket.send(payloadBytes);
    },

    browserSocketDisconnect: function (socketIndex) {
      browserSocket.sockets[socketIndex].webSocket.close();
    }
  },


  // External C# calls.

  GetBrowserSocketIsConnected: function (socketIndex) {
    return browserSocket.getBrowserSocketIsConnected(socketIndex);
  },

  GetBrowserSocketHasUnreadPayloadQueue: function (socketIndex) {
    return browserSocket.getBrowserSocketHasUnreadPayloadQueue(socketIndex);
  },

  BrowserSocketReadPayloadQueue: function (socketIndex, payloadBytesBufferPtr, payloadBytesBufferLength) {
    return browserSocket.browserSocketReadPayloadQueue(socketIndex, payloadBytesBufferPtr, payloadBytesBufferLength);
  },

  BrowserSocketConnect: function (serverAddressPtr) {
    const serverAddress = UTF8ToString(serverAddressPtr);
    return browserSocket.browserSocketConnect(serverAddress);
  },

  BrowserSocketSend: function (socketIndex, payloadBytesPtr, payloadBytesCount) {
    const bytesToSend = HEAPU8.buffer.slice(payloadBytesPtr, payloadBytesPtr + payloadBytesCount);
    browserSocket.browserSocketSend(socketIndex, bytesToSend);
  },

  BrowserSocketDisconnect: function (socketIndex) {
    browserSocket.browserSocketDisconnect(socketIndex);
  },
};

autoAddDeps(library, '$browserSocket');
mergeInto(LibraryManager.library, library);
