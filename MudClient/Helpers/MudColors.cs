using System.Collections.Generic;
using System.Drawing;

namespace MudClient {
    public class MudColors {
        public const string ANSI_COLOR_ESCAPE_CHARACTER = "\u001b";
        public const string ANSI_ESCAPE_CHARACTER = "\\x1B";
        public const string ANSI_RESET = "[0m";
        public const string ANSI_UNKNOWN = "[01m";
        public const string ANSI_BLACK = "[30m";
        public const string ANSI_RED = "[31m";
        public const string ANSI_GREEN = "[32m";
        public const string ANSI_YELLOW = "[33m";
        public const string ANSI_BLUE = "[34m";
        public const string ANSI_PURPLE = "[35m";
        public const string ANSI_CYAN = "[36m";
        public const string ANSI_WHITE = "[37m";

        public static Color BackgroundColor { get; set; } = Color.Black;
        public static Color ForegroundColor { get; set; } = Color.White;

        public static Color RoomTitle { get; set; } = Color.Teal;
        public static Color RoomExits { get; set; } = Color.White;
        public static Color Tracks { get; set; } = Color.White;
        public static Color ItemsOnFloor { get; set; } = Color.Green;
        public static Color CreaturesInRoom { get; set; } = Color.Yellow;
        public static Color CommandColor { get; set; } = Color.Gold;
        public static Color ClientInfoColor { get; set; } = Color.CornflowerBlue;

        public static readonly Dictionary<string, Color> Dictionary = new Dictionary<string, Color> {
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
