using MudClient.Extensions;
using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class NarrsWriter {
        private readonly BufferBlock<List<FormattedOutput>> _outputBuffer;

        private readonly MudClientForm _form;

        public NarrsWriter(BufferBlock<List<FormattedOutput>> outputBuffer, MudClientForm form) {
            _outputBuffer = outputBuffer;
            _form = form;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken)
        {
            Task.Run(() => LoopFormattedOutput(cancellationToken));
        }

        private enum RoomSeenState {
            NotStarted,
            SeenTitle,
            SeenDescirption,
            SeenExits
        }

        private async Task LoopFormattedOutput(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                List<FormattedOutput> outputs = await _outputBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                foreach (var output in outputs) {
                    foreach (var line in output.Text.Split('\n')) {
                        // todo: should match on colour and also with a regex instead of doing this
                        if (line.Contains(" narrates '")
                            || line.Contains(" tells you '")
                            || line.Contains(" says '")
                            || line.Contains(" speaks from the ")
                            || line.Contains(" bellows '")
                            || line.Contains(" hisses '")
                            || line.Contains(" chats '")
                            || line == "You are hungry."
                            || line == "You are thirsty.") {

                            if (line == "You are hungry." || line == "You are thirsty.") {
                                SystemSounds.Beep.Play();
                            }

                            _form.WriteToNarrs(line + "\n", output.TextColor);
                        }
                    }
                }
            }
        }
    }
}
