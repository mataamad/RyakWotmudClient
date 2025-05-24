using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MudClient {
    internal static class FormatDecodedText {


        // colours text based on the ansi escape sequences
        // minimises the number of elements in the returned List to make display faster
        internal static List<FormattedOutput> Format(string s) {
            var output = new List<FormattedOutput>();

            const char ESCAPE_CHAR = (char)0x1B;
            const char REPLACE_CURRENT_LINE_CHAR = (char)0x00; // is '\0'

            var previousForegroundColor = MudColors.ForegroundColor;
            var foregroundColor = MudColors.ForegroundColor;
            var sb = new StringBuilder();
            bool replaceCurrentLine = false;

            bool justResetColor = false;

            var e = s.GetEnumerator();
            while (e.MoveNext())
            {

                char c = e.Current;

                if (c == ESCAPE_CHAR) {
                    StringBuilder escapeCharSb = new StringBuilder();

                    while (e.MoveNext()) {
                        char ch = e.Current;
                        if (ch == 'm') {
                            escapeCharSb.Append(ch);
                            break;
                        }
                        if (ch != '[' && !char.IsNumber(ch)) {
                            sb.Append("\nUHOH! unrecognised escape sequence - unexpected character\n");
                            break;
                        }

                        escapeCharSb.Append(ch);
                    }

                    var escapeChar = escapeCharSb.ToString();
                    Color escapeColor = Color.Empty;
                    if (escapeChar == MudColors.ANSI_RESET) {
                        // reset to the previous escape color
                        // todo: this only goes one color deep; should I have a stack here? does wotmud ever do that? is that a thing
                        escapeColor = previousForegroundColor;
                        justResetColor = true;
                    }

                    if (escapeColor != Color.Empty || MudColors.Dictionary.TryGetValue(escapeChar, out escapeColor)) {
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

                            previousForegroundColor = foregroundColor;
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
                    previousForegroundColor = MudColors.ForegroundColor; // it looks like newlines probably reset the ANSI_RESET color
                    // foregroundColor = MudColors.ForegroundColor; // todo: this seems to be necessary too, but it'll mean that if any colors take up multiple lines they wont work as expected

                    if (justResetColor) {
                        foregroundColor = MudColors.ForegroundColor;
                        justResetColor = false;
                    }

                    sb.Append('\n');
                } else if (c == '\x07') {
                    output.Add(new FormattedOutput { Beep = true });
                    justResetColor = false;
                } else {
                    sb.Append(c);
                    justResetColor = false;
                }
            }

            output.Add(new FormattedOutput { Text = sb.ToString(), TextColor = foregroundColor, ReplaceCurrentLine = replaceCurrentLine });
            return output;
        }
    }
}
