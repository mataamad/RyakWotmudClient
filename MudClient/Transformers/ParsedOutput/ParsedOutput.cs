using System.Drawing;

namespace MudClient {

    public enum ParsedOutputType {
        Raw,
        Room,
        Status
    }

    public class ParsedOutput {
        public ParsedOutputType Type { get; set; } = ParsedOutputType.Raw;
        public string[] Lines { get; set; } = new string[0];

        // todo: only used by room
        public string Title = "";
        public string[] Description = new string[0];
        public string Exits = "";
        public string[] Tracks = new string[0];
        public string[] Items = new string[0];
        public string[] Creatures = new string[0];
    }
}
