using System.Drawing;

namespace MudClient.Transformers.ParsedOutput {

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
        internal string[] Lines { get; set; } = [];
        internal LineMetadata[] LineMetadata = [];

        // todo: only used by room
        internal string Title = "";
        internal string[] Description = [];
        internal string Exits = "";
        internal string[] Tracks = [];
        internal string[] Items = [];
        internal string[] Creatures = [];
    }
}
