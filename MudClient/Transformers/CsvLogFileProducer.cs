using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static MudClient.CsvLogFileWriter;

namespace MudClient {
    internal class CsvLogFileProducer {
        // todo: allow choosing between constant time between messages & replaying back at a % of orignal input speed
        // todo: allow configuring time between messages
        internal CsvLogFileProducer() {
        }

        private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(0.5);

        // private readonly TimeSpan _delay = TimeSpan.FromSeconds(0.25);
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(0.00);

        internal void LoopOnNewThread(string filename, CancellationToken cancellationToken, Action onLogParsed = null) {
            LoopOnNewThread(filename, cancellationToken, _delay, onLogParsed);
        }

        internal void LoopOnNewThread(string filename, CancellationToken cancellationToken, TimeSpan timeBetweenMessages, Action onLogParsed = null) {
            Task.Run(() => ProcessLogLoop(filename, timeBetweenMessages, onLogParsed, cancellationToken));
        }

        private async Task ProcessLogLoop(string filename, TimeSpan timeBetweenMessages,  Action onLogParsed, CancellationToken cancellationToken) {
            var lines = File.ReadAllLines(filename);

            await Task.Delay(_initialDelay); // todo: dirty hack because the windows need to be load before the log starts flying

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
                    await Store.TcpSend.SendAsync(decodedText);
                } else if (logLine.MessageType == LOG_TYPE_MUD_OUTPUT) {
                    await Store.TcpReceive.SendAsync(decodedText);
                } else if (logLine.MessageType == LOG_TYPE_CLIENT_INFO) {
                    await Store.ClientInfo.SendAsync(decodedText);
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
