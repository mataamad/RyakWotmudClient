using MudClient.Extensions;
using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class OutputWriter {
        private readonly BufferBlock<List<FormattedOutput>> _outputBuffer;
        private readonly BufferBlock<string> _sentMessageBuffer;
        private readonly BufferBlock<string> _clientInfoBuffer;
        private readonly MudClientForm _form;

        public OutputWriter(BufferBlock<List<FormattedOutput>> outputBuffer, BufferBlock<string> sentMessageBuffer, BufferBlock<string> clientInfoBuffer, MudClientForm form) {
            _outputBuffer = outputBuffer;
            _sentMessageBuffer = sentMessageBuffer;
            _clientInfoBuffer = clientInfoBuffer;
            _form = form;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken)
        {
            Task.Run(() => LoopFormattedOutput(cancellationToken));
            Task.Run(() => LoopSentMessage(cancellationToken));
            Task.Run(() => LoopClientInfo(cancellationToken));
        }

        private async Task LoopFormattedOutput(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                List<FormattedOutput> output = await _outputBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                _form.WriteToOutput(output);
            }
        }

        private async Task LoopSentMessage(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                string output = await _sentMessageBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                // output = "\n" + output + "\n";
                // output = "\n" + output;
                output = output + "\n"; // seems to be the most like zMud
                _form.WriteToOutput(output, MudColors.CommandColor);
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

                output = "\n" + output + "\n";
                _form.WriteToOutput(output, MudColors.ClientInfoColor);
            }
        }
    }
}
