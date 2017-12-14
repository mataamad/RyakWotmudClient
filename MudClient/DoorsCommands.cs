using MudClient.Extensions;
using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class DoorsCommands {
        private readonly BufferBlock<List<FormattedOutput>> _outputBuffer;
        private readonly BufferBlock<string> _sendMessageBuffer;
        private readonly BufferBlock<string> _sendSpecialMessageBuffer;
        private readonly BufferBlock<string> _clientInfoBuffer;

        private readonly MapWindow _map;

        public DoorsCommands(BufferBlock<List<FormattedOutput>> outputBuffer,
            BufferBlock<string> sendMessageBuffer,
            BufferBlock<string> sendSpecialMessageBuffer,
            BufferBlock<string> clientInfoBuffer,
            MapWindow mapWindow) {

            _outputBuffer = outputBuffer;
            _sendMessageBuffer = sendMessageBuffer;
            _sendSpecialMessageBuffer = sendSpecialMessageBuffer;
            _clientInfoBuffer = clientInfoBuffer;
            _map = mapWindow;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken)
        {
            // Task.Run(() => LoopFormattedOutput(cancellationToken));
            Task.Run(() => LoopSpecialMessage(cancellationToken));
        }

        private enum RoomSeenState {
            NotStarted,
            SeenTitle,
            SeenDescirption,
            SeenExits
        }

        private async Task LoopSpecialMessage(CancellationToken cancellationToken) {
            while (!_map.DataLoaded) {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested) {
                string output = await _sendSpecialMessageBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                // process the command the player entered
                var splitOutput = output.Split(' ');
                if (splitOutput.Length != 1) {
                    if (splitOutput[0] == "o") {
                        if (new[] { "n", "e", "s", "w" }.Contains(splitOutput[1])) {
                            splitOutput = new[] { "o" + splitOutput[2] };
                        } else {
                            await _sendMessageBuffer.SendAsync($"open {string.Join(" ", splitOutput.Skip(1))}");
                            continue;
                        }
                    } else if (splitOutput[0] == "c") {
                        if (new[] { "n", "e", "s", "w" }.Contains(splitOutput[1])) {
                            splitOutput = new[] { "s" + splitOutput[1] };
                        } else {
                            await _sendMessageBuffer.SendAsync($"close {string.Join(" ", splitOutput.Skip(1))}");
                            continue;
                        }
                    }
                }
                string command = splitOutput.First().ToLower();
                // todo: parse the message

                var commands = new HashSet<string> {
                    "o", "c", "cll", "opp", "unl", "loc", "pic",
                    "on", "oe", "os", "ow", "ou", "od",
                    "sn", "se", "ss", "sw", "su", "sd",
                    "locn", "loce", "locs", "locw", "locu", "locd",
                    "unln", "unle", "unls", "unlw", "unlu", "unld",
                    "picn", "pice", "pics", "pic", "picu", "picd",
                };

                if (!commands.Contains(command)) {
                    continue;
                }


                // need a map window command to get all doors, and also to get doors in a direction

                // todo: this probably shouldn't be accessed from here
                var currentRoomExits = new ZmudDbExitTblRow[0];
                _map.ExitsByFromRoom.TryGetValue(_map.CurrentVirtualRoomId, out currentRoomExits);

                var exitsWithDoors = currentRoomExits.Where(exit => !string.IsNullOrEmpty(exit.Param)).ToArray();

                if (exitsWithDoors.Length == 0) {
                    await _clientInfoBuffer.SendAsync($"DOOR NOT FOUND FOR: {output}");
                    continue;
                }


                (string commandType, DirectionType direction, bool all) = ParseCommand(command);

                if (direction == DirectionType.Other) {
                    if (all) {
                        foreach (var exit in exitsWithDoors) {
                            await _sendMessageBuffer.SendAsync($"{commandType} {exit.Param}");
                        }
                    } else {
                        // open the one door in the room
                        if (exitsWithDoors.Length == 1) {
                            await _sendMessageBuffer.SendAsync($"{commandType} {exitsWithDoors[0].Param}");
                        } else {
                            await _clientInfoBuffer.SendAsync($"MULTIPLE DOORS: {string.Join("|", exitsWithDoors.Select(e => e.Param))}");
                        }
                    }
                } else {
                    // open the door in the direction specified
                    var exitsWithDoor = exitsWithDoors.Where(exit => exit.DirType == (int)direction).ToArray();
                    if (exitsWithDoor.Length == 1) {
                        await _sendMessageBuffer.SendAsync($"{commandType} {exitsWithDoor[0].Param}");
                    } else {
                        await _clientInfoBuffer.SendAsync($"DOOR NOT FOUND FOR: {output}");
                    }
                }
            }
        }

        private (string commandType, DirectionType direction, bool all) ParseCommand(string command) {
            DirectionType direction = DirectionType.Other;
            string commandType = "open";
            bool all = false;

            switch (command) {
                case "o":
                    commandType = "open";
                    break;
                case "c":
                    commandType = "close";
                    break;
                case "opp":
                    commandType = "open";
                    all = true;
                    break;
                case "cll":
                    commandType = "close";
                    all = true;
                    break;
                case "unl":
                    commandType = "unlock";
                    break;
                case "loc":
                    commandType = "lock";
                    break;
                case "pic":
                    commandType = "pick";
                    break;
                case "on":
                    commandType = "open";
                    direction = DirectionType.North;
                    break;
                case "oe":
                    commandType = "open";
                    direction = DirectionType.East;
                    break;
                case "os":
                    commandType = "open";
                    direction = DirectionType.South;
                    break;
                case "ow":
                    commandType = "open";
                    direction = DirectionType.West;
                    break;
                case "ou":
                    commandType = "open";
                    direction = DirectionType.Up;
                    break;
                case "od":
                    commandType = "open";
                    direction = DirectionType.Down;
                    break;
                case "sn":
                    commandType = "close";
                    direction = DirectionType.North;
                    break;
                case "se":
                    commandType = "close";
                    direction = DirectionType.East;
                    break;
                case "ss":
                    commandType = "close";
                    direction = DirectionType.South;
                    break;
                case "sw":
                    commandType = "close";
                    direction = DirectionType.West;
                    break;
                case "su":
                    commandType = "close";
                    direction = DirectionType.Up;
                    break;
                case "sd":
                    commandType = "close";
                    direction = DirectionType.Down;
                    break;
                case "locn":
                    commandType = "lock";
                    direction = DirectionType.North;
                    break;
                case "loce":
                    commandType = "lock";
                    direction = DirectionType.East;
                    break;
                case "locs":
                    commandType = "lock";
                    direction = DirectionType.South;
                    break;
                case "locw":
                    commandType = "lock";
                    direction = DirectionType.West;
                    break;
                case "locu":
                    commandType = "lock";
                    direction = DirectionType.Up;
                    break;
                case "locd":
                    commandType = "lock";
                    direction = DirectionType.Down;
                    break;
                case "unln":
                    commandType = "unlock";
                    direction = DirectionType.North;
                    break;
                case "unle":
                    commandType = "unlock";
                    direction = DirectionType.East;
                    break;
                case "unls":
                    commandType = "unlock";
                    direction = DirectionType.South;
                    break;
                case "unlw":
                    commandType = "unlock";
                    direction = DirectionType.West;
                    break;
                case "unlu":
                    commandType = "unlock";
                    direction = DirectionType.Up;
                    break;
                case "unld":
                    commandType = "unlock";
                    direction = DirectionType.Down;
                    break;
                case "picn":
                    commandType = "pick";
                    direction = DirectionType.North;
                    break;
                case "pice":
                    commandType = "pick";
                    direction = DirectionType.East;
                    break;
                case "pics":
                    commandType = "pick";
                    direction = DirectionType.South;
                    break;
                case "picw":
                    commandType = "pick";
                    direction = DirectionType.West;
                    break;
                case "picu":
                    commandType = "pick";
                    direction = DirectionType.Up;
                    break;
                case "picd":
                    commandType = "pick";
                    direction = DirectionType.Down;
                    break;
            }
            return (commandType, direction, all);
        }
    }
}
