using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient.Transformers.ParsedOutput {
    internal class SimpleParser {
        private Dictionary<string, LineMetadataType> _simpleStrings = new() {
            { "They aren't here.", LineMetadataType.TheyArentHere },
            { "You do the best you can!", LineMetadataType.YouDoTheBestYouCan },
            { "Ok", LineMetadataType.Ok },
            { "You flee head over heels.", LineMetadataType.YouFlee },
            { "The fighting is too thick and heavy for you to enter the fray!", LineMetadataType.TheFightingIsTooHeavyForYoutToEnterTheFray },
            { "Alas, you cannot go that way...", LineMetadataType.YouCannotGoThatWay },
            { "No way!  You're fighting for your life!", LineMetadataType.YouAreFightingForYourLife },
            { "Your heartbeat calms down more as you feel less panicked.", LineMetadataType.YouFeelLessPanicked },
            { "You are standing.", LineMetadataType.YouAreStanding },
            { "Arglebargle, glop-glyf!?!", LineMetadataType.Arglebargle },
            { "Bash who?", LineMetadataType.BashWho },
            { "They already seem to be stunned.", LineMetadataType.TheyAlreadySeemToBeStunned },
            { "You start paying increased attention to your surroundings.", LineMetadataType.YouStartPayingIncreasedAttentionToYourSurroundings },
            { "PANIC!  You couldn't escape!", LineMetadataType.YouCouldntEscape },
        };

        internal SimpleParser() {
        }

        internal LineMetadataType Parse(string line) {
            if (_simpleStrings.ContainsKey(line)) {
                return _simpleStrings[line];
            }
            return LineMetadataType.None;
        }

    }
}
