using MudClient.Management;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class CsvLogFileWriter {
        private readonly BufferBlock<string> _outputBuffer;
        private readonly BufferBlock<string> _sentMessageBuffer;
        private readonly BufferBlock<string> _clientInfoBuffer;

        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public const string LOG_TYPE_MUD_INPUT = "MudInput"; // message sent to the mud
        public const string LOG_TYPE_MUD_OUTPUT = "MudOutput"; // message received from the mud
        public const string LOG_TYPE_CLIENT_INFO = "ClientInfo"; // info printed to the screen by the client

        public class LogLine {
            public DateTime Time { get; set; }
            public double MsSinceStart { get; set; }
            public string MessageType { get; set; } = LOG_TYPE_MUD_INPUT;
            public string EncodedText { get; set; }
        }
        private readonly BufferBlock<LogLine> _logBuffer = new BufferBlock<LogLine>();

        public const string _dateString = "{date}";
        private readonly string _filename = $"./Log_{_dateString}.csv";

        // todo: need to pass in the timing somehow...
        //          measure it in here for now.
        // so will read the sent & output messages and append them to a new log file buffer & loop writing that to a single file on a 3rd thread
        // need to choose how to escape everything.
        public CsvLogFileWriter(BufferBlock<string> outputBuffer, BufferBlock<string> sentMessageBuffer, BufferBlock<string> clientInfoBuffer) {
            _outputBuffer = outputBuffer;
            _sentMessageBuffer = sentMessageBuffer;
            _clientInfoBuffer = clientInfoBuffer;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken) {
            Task.Run(() => LoopOutput(cancellationToken));
            Task.Run(() => LoopSentMessage(cancellationToken));
            Task.Run(() => LoopClientInfo(cancellationToken));
            Task.Run(() => LoopWriteFile(cancellationToken));
        }

        private async Task LoopOutput(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {

                string output;
                try {
                    output = await _outputBuffer.ReceiveAsync(cancellationToken);
                } catch (OperationCanceledException) {
                    return;
                }
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await _logBuffer.SendAsync(new LogLine {
                    Time = DateTime.Now,
                    MsSinceStart = _sw.Elapsed.TotalMilliseconds,
                    MessageType = LOG_TYPE_MUD_OUTPUT,
                    EncodedText = EncodeLogString(output),
                });
            }
        }

        private async Task LoopSentMessage(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {

                string output;
                try {
                    output = await _sentMessageBuffer.ReceiveAsync(cancellationToken);
                } catch (OperationCanceledException) {
                    return;
                }
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await _logBuffer.SendAsync(new LogLine {
                    Time = DateTime.Now,
                    MsSinceStart = _sw.Elapsed.TotalMilliseconds,
                    MessageType = LOG_TYPE_MUD_INPUT,
                    EncodedText = EncodeLogString(output),
                });
            }
        }

        private async Task LoopClientInfo(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {

                string output;
                try {
                    output = await _clientInfoBuffer.ReceiveAsync(cancellationToken);
                } catch (OperationCanceledException) {
                    return;
                }
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await _logBuffer.SendAsync(new LogLine {
                    Time = DateTime.Now,
                    MsSinceStart = _sw.Elapsed.TotalMilliseconds,
                    MessageType = LOG_TYPE_CLIENT_INFO,
                    EncodedText = EncodeLogString(output),
                });
            }
        }


        private async Task LoopWriteFile(CancellationToken cancellationToken) {
            string filename = _filename.Replace(_dateString, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture));
            using (var file = new StreamWriter(filename, append: true)) {
                while (!cancellationToken.IsCancellationRequested) {

                    LogLine output;
                    try {
                        output = await _logBuffer.ReceiveAsync(cancellationToken);
                    } catch (OperationCanceledException) {
                        return;
                    }
                    if (cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    string formattedOutput = $"{output.Time},{output.MsSinceStart},{output.MessageType},{output.EncodedText}";
                    file.WriteLine(formattedOutput);
                    file.Flush();
                }
            }
        }

        private string EncodeLogString(string s) {
            var sb = new StringBuilder();

            foreach (char c in s) {
                if (Char.IsControl(c) || (c > 127 && c < 256) || c == ',') {
                    if (c == '\r') {
                        sb.Append("\\r");
                    } else if (c == '\n') {
                        sb.Append("\\n");
                    } else {
                        // encode control characters as e.g. [1A]
                        sb.Append("\\x");
                        sb.Append(string.Format("{0:X2}", (byte)c));
                    }
                } else if (c > 255) {
                    // This character is too big for ASCII
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                } else {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
