using MudClient.Common.Extensions;
using MudClient.Core.Common;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace MudClient {
    internal class ConnectionClientProducer {
        internal delegate void EventHandler(MessageEventArgs e);

        internal event EventHandler OnClientDisconnected;
        internal event EventHandler OnConnectionEstablished;

        private readonly TcpClient _tcpClient;

        private readonly byte[] _dataBuffer = new byte[1024 * 8 /*8KB*/];
        private NetworkStream _controlStream;
        private StreamWriter _controlWriter;

        internal ConnectionClientProducer() {

            _tcpClient = new TcpClient {
                NoDelay = true,
            };
            _tcpClient.Client.NoDelay = true; // is this necessary?
            // 'todo: fix encoding'
            _tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true); // is this necessary?

            Store.TcpSend.SubscribeAsync(async (message) => {
                if (_tcpClient.Connected) {
                    await _controlWriter.WriteLineAsync(message);
                    await _controlWriter.FlushAsync();
                }
            });
        }

        internal void LoopOnNewThreads(string address, int port, CancellationToken cancellationToken) {
            Task.Run(() => ReceiveLoop(address, port, cancellationToken));
        }

        // loops receiving any messages
        private async Task ReceiveLoop(string address, int port, CancellationToken cancellationToken) {
            // connect
            await _tcpClient.ConnectAsync(address, port);

            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            if (!_tcpClient.Connected) {
                OnClientDisconnected?.Invoke(new MessageEventArgs("Failed to connect"));
                return;
            }
            _controlStream = _tcpClient.GetStream();
            _controlWriter = new StreamWriter(_controlStream, Encoding.Default);
            OnConnectionEstablished?.Invoke(new MessageEventArgs("Connected"));

            // loop receiving data
            while (!cancellationToken.IsCancellationRequested) {
                if (!_tcpClient.Connected) {
                    OnClientDisconnected?.Invoke(new MessageEventArgs("Failed to connect"));
                    return;
                }

                int length;
                try {
                    length = await _controlStream.ReadAsync(_dataBuffer, 0, _dataBuffer.Length, cancellationToken);
                } catch (OperationCanceledException) {
                    return;
                }

                string message = Encoding.Default.GetString(_dataBuffer, 0, length);
                if (!string.IsNullOrEmpty(message) || _tcpClient.IsSocketConnected()) {
                    await Store.TcpReceive.SendAsync(message);
                } else {
                    _tcpClient.Client.Disconnect(true);
                    OnClientDisconnected?.Invoke(new MessageEventArgs("Disconnected."));
                }
            }
        }
    }
}
