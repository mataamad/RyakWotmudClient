using System;
using System.Text;

namespace MudClient {
    public static class ControlCharacterEncoder {
        public static string Encode(string s, bool forCsv = false) {
            var sb = new StringBuilder();

            foreach (char c in s) {
                if (Char.IsControl(c) || (c > 127 && c < 256) || (forCsv && c == ',')) {
                    if (c == '\r') {
                        sb.Append("\\r");
                    } else if (c == '\n') {
                        sb.Append("\\n");
                        if (!forCsv) {
                            sb.Append(c);
                        }
                    } else {
                        // encode control characters as e.g. \x1A
                        sb.Append("\\x");
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


        public static string Decode(string s) {
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

                            string numberString = new string(new[] { numberChar1, numberChar2 });
                            char decodedChar = (char)int.Parse(numberString, System.Globalization.NumberStyles.HexNumber);

                            sb.Append(decodedChar);
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
