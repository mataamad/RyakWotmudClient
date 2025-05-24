using System.Collections.Generic;
using System.Drawing;

namespace MudClient {
    internal class MudColors {
        internal const string ANSI_COLOR_ESCAPE_CHARACTER = "\u001b";
        internal const string ANSI_ESCAPE_CHARACTER = "\\x1B";
        internal const string ANSI_RESET = "[0m";
        internal const string ANSI_UNKNOWN = "[01m";
        internal const string ANSI_BLACK = "[30m";
        internal const string ANSI_RED = "[31m";
        internal const string ANSI_GREEN = "[32m";
        internal const string ANSI_YELLOW = "[33m";
        internal const string ANSI_BLUE = "[34m";
        internal const string ANSI_PURPLE = "[35m";
        internal const string ANSI_CYAN = "[36m";
        internal const string ANSI_WHITE = "[37m";

        internal static Color BackgroundColor { get; set; } = Color.Black;
        internal static Color ForegroundColor { get; set; } = Color.White;

        internal static Color RoomTitle { get; set; } = Color.Teal;
        internal static Color RoomExits { get; set; } = Color.White;
        internal static Color Tracks { get; set; } = Color.White;
        internal static Color ItemsOnFloor { get; set; } = Color.Green;
        internal static Color CreaturesInRoom { get; set; } = Color.Yellow;
        internal static Color CommandColor { get; set; } = Color.Gold;
        internal static Color ClientInfoColor { get; set; } = Color.CornflowerBlue;

        internal static readonly Dictionary<string, Color> Dictionary = new Dictionary<string, Color> {
            { ANSI_UNKNOWN, Color.White },
            { ANSI_BLACK, Color.WhiteSmoke },
            { ANSI_RED, Color.Red },
            { ANSI_GREEN, Color.Green },
            { ANSI_YELLOW, Color.Yellow },
            { ANSI_BLUE, Color.Blue },
            { ANSI_PURPLE, Color.Purple },
            { ANSI_CYAN, Color.Teal },
            { ANSI_WHITE, Color.White },
        };
    }
}
