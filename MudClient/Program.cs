using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static MudClient.RawInputToRichTextConverter;

namespace MudClient {
	public class Program {

        // public const bool ReadFromLogFile = true;
        public const bool ReadFromLogFile = false;
        // public const string LogFilename = "./test_bash.csv";
        // public const string LogFilename = "./test_only_bash.csv";
        public const string LogFilename = "./Log_2017-11-27 17-34-35.csv";

        [STAThread]
		public static void Main(string[] args) {

            BufferBlock<string> tcpReceiveBuffer = new BufferBlock<string>();
            BufferBlock<List<FormattedOutput>> richTextBuffer = new BufferBlock<List<FormattedOutput>>();
            BufferBlock<string> devTextBuffer = new BufferBlock<string>();

            BufferBlock<string> sendMessageBuffer = new BufferBlock<string>();
            BufferBlock<string> clientInfoBuffer = new BufferBlock<string>();
            var tcpReceiveMultiplier = new BufferBlockMultiplier<string>(tcpReceiveBuffer);
            var sendMessageMultiplier = new BufferBlockMultiplier<string>(sendMessageBuffer);
            var clientInfoMultiplier = new BufferBlockMultiplier<string>(clientInfoBuffer);

            var cts = new CancellationTokenSource();

            var csvLogFileProducer = new CsvLogFileProducer(tcpReceiveBuffer, sendMessageBuffer, clientInfoBuffer);
            if (ReadFromLogFile) {
                csvLogFileProducer.LoopOnNewThread(LogFilename, cts.Token);
            }


            var connectionClientProducer = new ConnectionClientProducer(tcpReceiveBuffer, sendMessageMultiplier.GetBlock());
            var rawInputToRichTextConverter = new RawInputToRichTextConverter(tcpReceiveMultiplier.GetBlock(), richTextBuffer);
            var rawInputToDevTextConverter = new RawInputToDevTextConverter(tcpReceiveMultiplier.GetBlock(), devTextBuffer);
            var csvWriter = new CsvLogFileWriter(tcpReceiveMultiplier.GetBlock(), sendMessageMultiplier.GetBlock(), clientInfoMultiplier.GetBlock());
            rawInputToRichTextConverter.LoopOnNewThread(cts.Token);
            rawInputToDevTextConverter.LoopOnNewThread(cts.Token);
            tcpReceiveMultiplier.LoopOnNewThread(cts.Token);
            csvWriter.LoopOnNewThread(cts.Token);

			using (var form = new MudClientForm(cts.Token, connectionClientProducer, sendMessageBuffer, clientInfoBuffer)) {
                var outputWriter = new OutputWriter(richTextBuffer, sendMessageMultiplier.GetBlock(), clientInfoMultiplier.GetBlock(), form);
                var devOutputWriter = new DevOutputWriter(devTextBuffer, sendMessageMultiplier.GetBlock(), form.DevViewForm);
                outputWriter.LoopOnNewThread(cts.Token);
                devOutputWriter.LoopOnNewThread(cts.Token);
                sendMessageMultiplier.LoopOnNewThread(cts.Token);
                clientInfoMultiplier.LoopOnNewThread(cts.Token);

				form.ShowDialog();
			}
            cts.Cancel();
		}
	}
}
