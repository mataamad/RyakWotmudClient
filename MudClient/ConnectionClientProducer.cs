using MudClient.Common.Extensions;
using MudClient.Core.Common;
using MudClient.Extensions;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace MudClient {
    public class ConnectionClientProducer {
        public delegate void EventHandler(MessageEventArgs e);

        public event EventHandler OnClientDisconnected;
        public event EventHandler OnConnectionEstablished;

        private readonly BufferBlock<string> _receiveBuffer;
        private readonly BufferBlock<string> _sendBuffer;
        private readonly TcpClient _tcpClient;

        private byte[] _dataBuffer = new byte[1024 * 8 /*8KB*/];
        private NetworkStream _controlStream;
        private StreamWriter _controlWriter;

        public ConnectionClientProducer(BufferBlock<string> receiveBuffer, BufferBlock<string> sendBuffer) {
            _receiveBuffer = receiveBuffer;
            _sendBuffer = sendBuffer;

            _tcpClient = new TcpClient {
                NoDelay = true,
            };
            _tcpClient.Client.NoDelay = true; // is this necessary?
            _tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true); // is this necessary?
            // 'todo: fix encoding'
        }

        public void LoopOnNewThreads(string address, int port, CancellationToken cancellationToken) {
            Task.Run(() => ReceiveLoop(address, port, cancellationToken));
            Task.Run(() => SendLoop(cancellationToken));
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
                    await _receiveBuffer.SendAsync(message);
                } else {
                    _tcpClient.Client.Disconnect(true);
                    OnClientDisconnected?.Invoke(new MessageEventArgs("Disconnected."));
                }
            }
        }

        // Loops sending any messages
        private async Task SendLoop(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                string message = await _sendBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                if (_tcpClient.Connected) {
                    if (message.Trim().ToLower() != "qf") { // todo: ugly hack hah
                        await _controlWriter.WriteLineAsync(message);
                        await _controlWriter.FlushAsync();
                    }
                }
            }
        }

    }
}
