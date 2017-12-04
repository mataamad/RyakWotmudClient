using MudClient.Extensions;
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
                string output = await _outputBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await _logBuffer.SendAsync(ToLogLine(output, LOG_TYPE_MUD_OUTPUT));
            }
        }

        private async Task LoopSentMessage(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                string output = await _sentMessageBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await _logBuffer.SendAsync(ToLogLine(output, LOG_TYPE_MUD_INPUT));
            }
        }

        private async Task LoopClientInfo(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                string output = await _clientInfoBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await _logBuffer.SendAsync(ToLogLine(output, LOG_TYPE_CLIENT_INFO));
            }
        }

        private async Task LoopWriteFile(CancellationToken cancellationToken) {
            string filename = _filename.Replace(_dateString, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture));
            using (var file = new StreamWriter(filename, append: true)) {
                while (!cancellationToken.IsCancellationRequested) {
                    LogLine output = await _logBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                    if (cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    string formattedOutput = $"{output.Time},{output.MsSinceStart},{output.MessageType},{output.EncodedText}";
                    file.WriteLine(formattedOutput);
                    file.Flush();
                }
            }
        }

        private LogLine ToLogLine(string output, string messageType) {
            return new LogLine {
                Time = DateTime.Now,
                MsSinceStart = _sw.Elapsed.TotalMilliseconds,
                MessageType = messageType,
                EncodedText = ControlCharacterEncoder.Encode(output, forCsv: true),
            };
        }
    }
}
