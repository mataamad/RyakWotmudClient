using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient.Tests {
    [TestClass]
    // todo: need to actually test something in here...
    internal class SmokeTests {
        [TestMethod]
        internal async Task TestBash() {
            string simpleBashLog = @"./test_only_bash.csv";
            var response = await RunTest(simpleBashLog);
        }

        [TestMethod]
        internal async Task TestLongerLog() {
            var response = await RunTest("./Log_2017-11-27 17-34-35.csv");
        }

        // todo: fix these tests after the new Store


        private async Task<List<FormattedOutput>> RunTest(string logFilename) {
            BufferBlock<string> tcpReceiveBuffer = new();
            BufferBlock<string> sendMessageBuffer = new();
            BufferBlock<string> clientInfoBuffer = new();
            BufferBlock<List<FormattedOutput>> richTextBuffer = new();

            var cts = new CancellationTokenSource();

            bool logFileParsed = false;
            void logParsedCallback() {
                logFileParsed = true;
            }

            var csvLogFileProducer = new CsvLogFileProducer();
            csvLogFileProducer.LoopOnNewThread(logFilename, cts.Token, TimeSpan.Zero, logParsedCallback);

            // var rawInputToRichTextConverter = new RawInputToRichTextConverter();

            // todo: not sure how to tell if all output has been processed... Currently I think it's impossible to be sure.



            var output = new List<FormattedOutput>();
            // while (await richTextBuffer.OutputAvailableAsync() || await tcpReceiveBuffer.OutputAvailableAsync() || await sendMessageBuffer.OutputAvailableAsync() || !logFileParsed) {
            var token = cts.Token;
            while (!token.IsCancellationRequested) {
                if (logFileParsed) {
                    cts.CancelAfter(1000);
                } else {
                    cts.CancelAfter(5000);
                }
                try {
                    var line = await richTextBuffer.ReceiveAsync(cts.Token);
                    output.AddRange(line);
                } catch (TaskCanceledException) {

                }
            }

            cts.Cancel();

            return output;
        }
    }
}
