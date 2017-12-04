using MudClient.Core.Common;
using MudClient.Extensions;
using MudClient.Management;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class DevOutputWriter {
        private readonly BufferBlock<string> _devOutputBuffer;
        private readonly BufferBlock<string> _sentMessageBuffer;
        private readonly DevViewForm _form;

        public DevOutputWriter(BufferBlock<string> devOutputBuffer, BufferBlock<string> sentMessageBuffer, DevViewForm form) {
            _devOutputBuffer = devOutputBuffer;
            _sentMessageBuffer = sentMessageBuffer;
            _form = form;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken)
        {
            Task.Run(() => LoopDevOutput(cancellationToken));
            Task.Run(() => LoopSentMessage(cancellationToken));
        }

        private async Task LoopDevOutput(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                string output = await _devOutputBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                _form.WriteToOutput(output, MudColors.ForegroundColor);
            }
        }

        private async Task LoopSentMessage(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                string output = await _sentMessageBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                output = output + "\n"; // seems to be the most like zMud
                _form.WriteToOutput(output, MudColors.CommandColor);
            }
        }
    }
}
