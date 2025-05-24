using System;
using System.Collections.Generic;
using System.Text;

namespace MudClient.Helpers {
    internal static class ControlCharacterEncoder {
        internal static string Encode(string s, bool forCsv = false) {
            var sb = new StringBuilder();

            foreach (char c in s) {
                if (Char.IsControl(c) || c > 127 && c < 256 || forCsv && c == ',') {
                    if (c == '\r') {
                        sb.Append("\\r");
                    } else if (c == '\n') {
                        sb.Append("\\n");
                        if (!forCsv) {
                            sb.Append(c);
                        }
                    } else {
                        // encode control characters as e.g. \x1A
                        sb.Append($"\\x{(byte)c:X2}");
                    }
                } else if (c > 255) {
                    sb.Append($"\\u{(int)c:X4}");
                } else {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        // Encode characters and split into newlines.  ignore '\n's and splits on '\r's so that '\r\x00' comes out nice
        internal static List<string> EncodeAndSplit(string s) {
            var lines = new List<string>();

            var sb = new StringBuilder();
            int i = 0;
            while (i < s.Length) {
                char c = s[i];
                if (Char.IsControl(c) || c > 127 && c < 256) {
                    // need to split on \r\n, \n\r, \n, and \r, but still want to allow two empty lines of output in a row
                    if (c == '\r') {
                        lines.Add(sb.ToString());
                        sb.Clear();
                        if (i + 1 < s.Length && s[i + 1] == '\n') {
                            i++;
                        }
                    } else if (c == '\n') {
                        lines.Add(sb.ToString());
                        sb.Clear();
                        if (i + 1 < s.Length && s[i + 1] == '\r') {
                            i++;
                        }
                    } else {
                        // encode control characters as e.g. \x1A
                        sb.Append($"\\x{(byte)c:X2}");
                    }
                } else if (c > 255) {
                    sb.Append($"\\u{(int)c:X4}");
                } else {
                    sb.Append(c);
                }

                i++;
            }

            if (sb.Length > 0) {
                lines.Add(sb.ToString());
            }
            return lines;
        }



        internal static string Decode(string s) {
            var sb = new StringBuilder();

            var e = s.GetEnumerator();
            while (e.MoveNext()) {
                char c = e.Current;

                if (c == '\\') {
                    if (e.MoveNext()) {
                        char next = e.Current;

                        if (next == 'x') {
                            // todo: I should add a CharEnumerator extension method that can TryGet two characters or something
                            if (!e.MoveNext()) {
                                sb.Append(c);
                                sb.Append(next);
                                continue;
                            }
                            char numberChar1 = e.Current;

                            if (!e.MoveNext()) {
                                sb.Append(c);
                                sb.Append(next);
                                sb.Append(numberChar1);
                                continue;
                            }
                            char numberChar2 = e.Current;

                            string numberString = new string([numberChar1, numberChar2]);
                            int parsed;
                            if (int.TryParse(numberString, System.Globalization.NumberStyles.HexNumber, provider: null, out parsed)) {
                                char decodedChar = (char)parsed;
                                sb.Append(decodedChar);
                            } else {
                                sb.Append(c);
                                sb.Append(next);
                                sb.Append(numberChar1);
                                continue;
                            }

                        } else if (next == 'r') {
                            sb.Append('\r');
                        } else if (next == 'n') {
                            sb.Append('\n');
                        } else {
                            sb.Append(c);
                            sb.Append(next);
                        }
                    }
                } else {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
