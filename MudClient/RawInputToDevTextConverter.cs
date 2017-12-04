using MudClient.Extensions;
using MudClient.Management;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class RawInputToDevTextConverter {
        private readonly BufferBlock<string> _inputBuffer;
        private readonly BufferBlock<string> _devTextBuffer;

        public RawInputToDevTextConverter(BufferBlock<string> inputBuffer, BufferBlock<string> devTextBuffer) {
            _inputBuffer = inputBuffer;
            _devTextBuffer = devTextBuffer;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken) {
            Task.Run(() => Loop(cancellationToken));
        }

        private async Task Loop(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                string input = await _inputBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await _devTextBuffer.SendAsync(ControlCharacterEncoder.Encode(input).ToString());
            }
        }
    }
}
