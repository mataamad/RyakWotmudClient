using System.Drawing;

namespace MudClient {
    public class FormattedOutput {
        public string Text { get; set; }
        public Color TextColor { get; set; } = MudColors.ForegroundColor;
        public bool ReplaceCurrentLine { get; set; } = false;

        public override string ToString() {
            string replace = ReplaceCurrentLine ? "T" : "F";
            return $"R:{replace},{TextColor},\"{Text.Replace("\n", "\\n")}\"";
        }
    }
}
