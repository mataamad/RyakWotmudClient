using MudClient.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class RawInputToRichTextConverter {
        private readonly BufferBlock<string> _inputBuffer;
        private readonly BufferBlock<List<FormattedOutput>> _outputBuffer;

        public RawInputToRichTextConverter(BufferBlock<string> inputBuffer, BufferBlock<List<FormattedOutput>> outputBuffer) {
            _inputBuffer = inputBuffer;
            _outputBuffer = outputBuffer;
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

                await _outputBuffer.SendAsync(FormatOutput(input));
            }
        }

        // colours text based on the ansi escape sequences
        // minimises the number of elements in the returned List to make display faster
        private List<FormattedOutput> FormatOutput(string s) {
            var output = new List<FormattedOutput>();

            const char ESCAPE_CHAR = (char)0x1B;
            const char REPLACE_CURRENT_LINE_CHAR = (char)0x00; // is '\0'

            var foregroundColor = MudColors.ForegroundColor;
            var sb = new StringBuilder();
            bool replaceCurrentLine = false;
            var e = s.GetEnumerator();
            while (e.MoveNext())
            {
                char c = e.Current;

                if (c == ESCAPE_CHAR) {
                    StringBuilder escapeCharSb = new StringBuilder();

                    while (e.MoveNext()) {
                        char escapeChar = e.Current;
                        if (escapeChar == 'm') {
                            escapeCharSb.Append(escapeChar);
                            break;
                        }
                        if (escapeChar != '[' && !char.IsNumber(escapeChar)) {
                            sb.Append("\nUHOH! unrecognised escape sequence - unexpected character\n");
                            break;
                        }

                        escapeCharSb.Append(escapeChar);
                    }

                    if (MudColors.Dictionary.TryGetValue(escapeCharSb.ToString(), out Color escapeColor)) {
                        if (foregroundColor != escapeColor) {
                            var str = sb.ToString();
                            if (string.IsNullOrWhiteSpace(str) && !replaceCurrentLine) {
                                // string doesn't have printed characters so don't need to change the colour
                                // string is all whitespace so just concat it onto the previous one - color doesn't matter
                                var last = output.LastOrDefault();
                                if (last != null) {
                                    last.Text += sb.ToString();
                                } else {
                                    output.Add(new FormattedOutput { Text = sb.ToString(), TextColor = foregroundColor });
                                }
                            } else {
                                var last = output.LastOrDefault();
                                // if the last line didn't display any characters, then we can use it instead of making a new one
                                if (last != null && string.IsNullOrWhiteSpace(last.Text) && !replaceCurrentLine) {
                                    if (last.Text == "") {
                                        last.Text = sb.ToString();
                                    } else {
                                        last.Text += sb.ToString();
                                    }
                                    last.TextColor = foregroundColor;
                                } else {
                                    output.Add(new FormattedOutput { Text = sb.ToString(), TextColor = foregroundColor, ReplaceCurrentLine = replaceCurrentLine });
                                    replaceCurrentLine = false;
                                }
                            }
                            sb.Clear();
                            foregroundColor = escapeColor;
                        }
                    } else {
                        sb.Append($"\nUHOH! unrecognised escape sequence - unrecognised color {escapeCharSb.ToString()}\n");
                    }
                } else if (c == REPLACE_CURRENT_LINE_CHAR) { // '\0'
                    if (sb.Length > 0) {
                        output.Add(new FormattedOutput { Text = sb.ToString(), TextColor = foregroundColor, ReplaceCurrentLine = replaceCurrentLine });
                    }
                    sb.Clear();

                    replaceCurrentLine = true;
                } else if (c == '\r') {
                    // carrige returns always have \x00 or a \n next to them so just ignore them
                } else if (c == '\n') {
                    sb.Append('\n');
                } else if (c == '\x07') {
                    output.Add(new FormattedOutput { Beep = true });
                } else {
                    sb.Append(c);
                }
            }

            output.Add(new FormattedOutput { Text = sb.ToString(), TextColor = foregroundColor, ReplaceCurrentLine = replaceCurrentLine });
            return output;
        }
    }
}
