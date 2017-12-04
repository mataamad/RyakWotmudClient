using MudClient.Common.Extensions;
using MudClient.Core.Common;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static MudClient.CsvLogFileWriter;

namespace MudClient {
    public class CsvLogFileProducer {
        private readonly BufferBlock<string> _receiveBuffer;
        private readonly BufferBlock<string> _sendBuffer;
        private readonly BufferBlock<string> _clientInfoBuffer;

        // todo: allow choosing between constant time between messages & replaying back at a % of orignal input speed
        // todo: allow configuring time between messages
        public CsvLogFileProducer(BufferBlock<string> receiveBuffer, BufferBlock<string> sendBuffer, BufferBlock<string> clientInfoBuffer) {
            _receiveBuffer = receiveBuffer;
            _sendBuffer = sendBuffer;
            _clientInfoBuffer = clientInfoBuffer;
        }

        public void LoopOnNewThread(string filename, CancellationToken cancellationToken, Action onLogParsed = null) {
            LoopOnNewThread(filename, cancellationToken, TimeSpan.FromSeconds(0.25), onLogParsed);
        }

        public void LoopOnNewThread(string filename, CancellationToken cancellationToken, TimeSpan timeBetweenMessages, Action onLogParsed = null) {
            Task.Run(() => ProcessLogLoop(filename, timeBetweenMessages, onLogParsed, cancellationToken));
        }

        private async Task ProcessLogLoop(string filename, TimeSpan timeBetweenMessages,  Action onLogParsed, CancellationToken cancellationToken) {
            var lines = File.ReadAllLines(filename);

            foreach (var line in lines) {
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                // convert the line from csv into data
                var splitLine = line.Split(',');

                var logLine = new LogLine {
                    Time = DateTime.Parse(splitLine[0]),
                    MsSinceStart = double.Parse(splitLine[1]),
                    MessageType = splitLine[2],
                    EncodedText = splitLine[3]
                };

                await Task.Delay(timeBetweenMessages);

                string decodedText = ControlCharacterEncoder.Decode(logLine.EncodedText);

                if (logLine.MessageType == LOG_TYPE_MUD_INPUT) {
                    await _sendBuffer.SendAsync(decodedText);
                } else if (logLine.MessageType == LOG_TYPE_MUD_OUTPUT) {
                    await _receiveBuffer.SendAsync(decodedText);
                } else if (logLine.MessageType == LOG_TYPE_CLIENT_INFO) {
                    await _clientInfoBuffer.SendAsync(decodedText);
                } else {
                    throw new Exception("unknown log message type");
                }

            }
            if (onLogParsed != null) {
                onLogParsed();
            }
        }


    }
}
