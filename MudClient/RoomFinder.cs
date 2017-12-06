using MudClient.Extensions;
using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class RoomFinder {
        private readonly BufferBlock<List<FormattedOutput>> _outputBuffer;
        private readonly BufferBlock<string> _sentMessageBuffer;

        public class Room {
            public string Name;
            public string Description;
            public string ExitsLine;
        }


        private readonly MapWindow _mapWindow;

        public RoomFinder(BufferBlock<List<FormattedOutput>> outputBuffer, BufferBlock<string> sentMessageBuffer, MapWindow mapWindow) {
            _outputBuffer = outputBuffer;
            _sentMessageBuffer = sentMessageBuffer;
            _mapWindow = mapWindow;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken)
        {
            Task.Run(() => LoopFormattedOutput(cancellationToken));
            Task.Run(() => LoopSentMessage(cancellationToken));
        }

        private enum RoomSeenState {
            NotStarted,
            SeenTitle,
            SeenDescirption,
            SeenExits
        }

        private async Task LoopFormattedOutput(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                List<FormattedOutput> outputs = await _outputBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                RoomSeenState state = RoomSeenState.NotStarted;
                string roomName = null;
                foreach (var output in outputs) {
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
                        int skipLines = 0;
                        if (string.IsNullOrEmpty(splitIntoLines.First())) {
                            skipLines = 1;
                        }

                        // todo: there are probably some other checks I can add here to ignore descriptions that are known bad
                        // but for now I guess just include everything

                        var room = new Room {
                            Name = roomName,
                            Description = string.Join("\n", splitIntoLines.Skip(skipLines).Take(exitsLine - skipLines)) + "\n",
                            ExitsLine = splitIntoLines[exitsLine],
                        };
                        roomName = null;
                        state = RoomSeenState.NotStarted;
                        _mapWindow.RoomVisited(room);
                    }
                }
            }
        }

        // maybe useful at some point
        private async Task LoopSentMessage(CancellationToken cancellationToken) {
            while (!_mapWindow.DataLoaded) {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            while (!cancellationToken.IsCancellationRequested) {
                string output = await _sentMessageBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                // process the command the player entered
                output = output.Trim().ToLower();
                if (new[] { "qf", "n", "s", "e", "w", "u", "d" }.Contains(output)) {
                    _mapWindow.MoveVirtualRoom(output);
                }
            }
        }
    }
}
