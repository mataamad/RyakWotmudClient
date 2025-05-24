using System.Drawing;

namespace MudClient {

    internal enum ParsedOutputType {
        Raw,
        Room,
        Status
    }

    internal enum LineMetadataType {
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

    internal class LineMetadata {
        internal LineMetadataType Type { get; set; } = LineMetadataType.None;
    }

    internal class ParsedOutput {
        internal ParsedOutputType Type { get; set; } = ParsedOutputType.Raw;
        internal string[] Lines { get; set; } = new string[0];
        internal LineMetadata[] LineMetadata = new LineMetadata[0];

        // todo: only used by room
        internal string Title = "";
        internal string[] Description = new string[0];
        internal string Exits = "";
        internal string[] Tracks = new string[0];
        internal string[] Items = new string[0];
        internal string[] Creatures = new string[0];
    }
}
