using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MudClient.RoomFinder;

namespace MudClient {
    class RoomDescriptionStripper {

        public static List<FormattedOutput> StripRoomDescriptions(List<FormattedOutput> outputs) {
            List<FormattedOutput> strippedOutputs = new List<FormattedOutput>();

            RoomSeenState state = RoomSeenState.NotStarted;
            string roomName = null;
            foreach (var output in outputs) {
                var strippedOutput = new FormattedOutput {
                    Beep = output.Beep,
                    ReplaceCurrentLine = output.ReplaceCurrentLine,
                    Text = output.Text,
                    TextColor = output.TextColor,
                };
                strippedOutputs.Add(strippedOutput);

                if (state == RoomSeenState.NotStarted) {
                    string text = output.Text;
                    if (output.TextColor == MudColors.Dictionary[MudColors.ANSI_CYAN]) {
                        if (text.Contains("speaks from the") || text.Contains("answers your prayer")) {
                            continue;
                        }
                        roomName = text;
                        state = RoomSeenState.SeenTitle;

                        // todo: check for newlines - should only be either at the start or end
                    }
                } else if (state == RoomSeenState.SeenTitle) {
                    if (output.TextColor != MudColors.ForegroundColor) {
                        roomName = null;
                        state = RoomSeenState.NotStarted;
                        continue;
                    }
                    string[] splitIntoLines = output.Text.Split('\n');
                    int exitsLine = -1;
                    for (int i = 0; i < splitIntoLines.Length; i++) {
                        if (splitIntoLines[i].StartsWith("[ obvious exits: ")) {
                            exitsLine = i;
                            break;
                        }
                    }
                    if (exitsLine == -1) {
                        roomName = null;
                        state = RoomSeenState.NotStarted;
                        continue;
                    }

                    // it's a room.  Need to strip the description here
                    strippedOutput.Text = " " + string.Join("\n", splitIntoLines.Skip(exitsLine));

                    roomName = null;
                    state = RoomSeenState.NotStarted;
                }
            }
            return strippedOutputs;
        }
    }
}
