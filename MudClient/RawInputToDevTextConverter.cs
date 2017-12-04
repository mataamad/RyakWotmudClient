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

        public void LoopOnNewThread(CancellationToken cancellationToken)
        {
            Task.Run(() => Loop(cancellationToken));
        }

        private async Task Loop(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {

                string input;
                try {
                    input = await _inputBuffer.ReceiveAsync(cancellationToken);
                } catch (OperationCanceledException) {
                    return;
                }
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await _devTextBuffer.SendAsync(EncodeNonAsciiCharacters(input).ToString());
            }
        }

        // encodes non-ascii stuff to something machine readable (e.g. hex)
        private static string EncodeNonAsciiCharacters(string value) {
            /*value = Regex.Replace(value,
              @"\p{Cc}",
              a => string.Format("[{0:X2}]", (byte)a.Value[0])
            );*/

            var sb = new StringBuilder();
            foreach (char c in value) {
                if (Char.IsControl(c) || (c > 127 && c < 256)) {
                    // encode control characters as e.g. [1A]
                    if (c == '\n') {
                        sb.Append(string.Format("[{0:X2}]", (byte)c));
                        sb.Append(c);
                    } else if (c == '\r') {
                        sb.Append(string.Format("[{0:X2}]", (byte)c));
                        // skip \r or we get two newlines
                    } else {
                        sb.Append(string.Format("[{0:X2}]", (byte)c));
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
