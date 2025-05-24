using System.Drawing;

namespace MudClient {
    internal class FormattedOutput {
        internal string Text { get; set; } = "";
        internal Color TextColor { get; set; } = MudColors.ForegroundColor;
        internal bool ReplaceCurrentLine { get; set; } = false;
        internal bool Beep { get; set; } = false;

        public override string ToString() {
            string replace = ReplaceCurrentLine ? "T" : "F";
            if (Beep) {
                return "#BEEP";
            }
            return $"R:{replace},{TextColor},\"{Text.Replace("\n", "\\n")}\"";
        }
    }
}
