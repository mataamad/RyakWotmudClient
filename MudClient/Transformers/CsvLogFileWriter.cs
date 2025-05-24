using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace MudClient {
    internal class CsvLogFileWriter {
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        internal const string LOG_TYPE_MUD_INPUT = "MudInput"; // message sent to the mud
        internal const string LOG_TYPE_MUD_OUTPUT = "MudOutput"; // message received from the mud
        internal const string LOG_TYPE_CLIENT_INFO = "ClientInfo"; // info printed to the screen by the client

        internal class LogLine {
            internal DateTime Time { get; set; }
            internal double MsSinceStart { get; set; }
            internal string MessageType { get; set; } = LOG_TYPE_MUD_INPUT;
            internal string EncodedText { get; set; }
        }

        private readonly SubscribableBuffer<LogLine> _logBuffer = new SubscribableBuffer<LogLine>();

        internal const string _dateString = "{date}";
        private readonly string _filename = $"./Log_{_dateString}.csv";

        private StreamWriter _file;

        internal CsvLogFileWriter() {
            Store.TcpReceive.SubscribeAsync(async (message) => {
                await _logBuffer.SendAsync(ToLogLine(message, LOG_TYPE_MUD_OUTPUT));
            });

            Store.TcpSend.SubscribeAsync(async (message) => {
                await _logBuffer.SendAsync(ToLogLine(message, LOG_TYPE_MUD_INPUT));
            });

            Store.ClientInfo.SubscribeAsync(async (message) => {
                await _logBuffer.SendAsync(ToLogLine(message, LOG_TYPE_CLIENT_INFO));
            });

            string filename = _filename.Replace(_dateString, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture));
            _file = new StreamWriter(filename, append: true);
            _logBuffer.Subscribe((output) => {
                string formattedOutput = $"{output.Time},{output.MsSinceStart},{output.MessageType},{output.EncodedText}";
                _file.WriteLine(formattedOutput);
                _file.Flush();
            });
        }

        internal void CloseFile() {
            _file.Close();
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
