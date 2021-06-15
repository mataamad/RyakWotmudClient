using System.Drawing;

namespace MudClient {

    public enum ParsedOutputType {
        Raw,
        Room,
        Status
    }

    public enum LineMetadataType {
        None,
        Attack,
        Communication,
        TheyArentHere,
        YouDoTheBestYouCan,
        Ok,
        YouFlee,
        TheFightingIsTooHeavyForYoutToEnterTheFray,
        YouCannotGoThatWay,
        YouAreFightingForYourLife,
        YouFeelLessPanicked,
        YouAreStanding,
        Arglebargle,
        BashWho,
        TheyAlreadySeemToBeStunned,
        YouStartPayingIncreasedAttentionToYourSurroundings,
        YouCouldntEscape,
    }

    public class LineMetadata {
        public LineMetadataType Type { get; set; } = LineMetadataType.None;
    }

    public class ParsedOutput {
        public ParsedOutputType Type { get; set; } = ParsedOutputType.Raw;
        public string[] Lines { get; set; } = new string[0];
        public LineMetadata[] LineMetadata = new LineMetadata[0];

        // todo: only used by room
        public string Title = "";
        public string[] Description = new string[0];
        public string Exits = "";
        public string[] Tracks = new string[0];
        public string[] Items = new string[0];
        public string[] Creatures = new string[0];
    }
}
