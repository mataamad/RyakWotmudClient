using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MudClient {
    internal class DoorsCommands {
        private readonly MapWindow _map;
        internal static readonly HashSet<string> directions = new() { "n", "e", "s", "w", "u", "d" };

        internal DoorsCommands(MapWindow mapWindow) {
            _map = mapWindow;

            Store.ComplexAlias.SubscribeAsync(async (message) => {
                await ProcessDoorCommands(message);
            });
        }

        private enum RoomSeenState {
            NotStarted,
            SeenTitle,
            SeenDescirption,
            SeenExits
        }

        private async Task ProcessDoorCommands(string output) {
            // process the command the player entered
            var splitOutput = output.Split(' ');
            if (splitOutput.Length != 1) {
                if (splitOutput[0] == "o") {
                    if (directions.Contains(splitOutput[1])) {
                        splitOutput = ["o" + splitOutput[2]];
                    } else {
                        await Store.TcpSend.SendAsync($"open {string.Join(" ", splitOutput.Skip(1))}");
                        return;
                    }
                } else if (splitOutput[0] == "c") {
                    if (directions.Contains(splitOutput[1])) {
                        splitOutput = ["s" + splitOutput[1]];
                    } else {
                        await Store.TcpSend.SendAsync($"close {string.Join(" ", splitOutput.Skip(1))}");
                        return;
                    }
                }
            }
            string command = splitOutput.First().ToLower();

            var commands = new HashSet<string> {
                    "o", "c", "cll", "opp", "unl", "loc", "pic",
                    "on", "oe", "os", "ow", "ou", "od",
                    "sn", "se", "ss", "sw", "su", "sd",
                    "locn", "loce", "locs", "locw", "locu", "locd",
                    "unln", "unle", "unls", "unlw", "unlu", "unld",
                    "picn", "pice", "pics", "pic", "picu", "picd",
                };

            if (!commands.Contains(command)) {
                return;
            }


            // need a map window command to get all doors, and also to get doors in a direction

            // todo: this probably shouldn't be accessed from here
            var currentRoomExits = Array.Empty<ZmudDbExitTblRow>();
            MapData.ExitsByFromRoom.TryGetValue(MapData.CurrentVirtualRoomId, out currentRoomExits);

            var exitsWithDoors = currentRoomExits.Where(exit => !string.IsNullOrEmpty(exit.Param)).ToArray();

            if (exitsWithDoors.Length == 0) {
                await Store.ClientInfo.SendAsync($"DOOR NOT FOUND FOR: {output}");
            }


            (string commandType, DirectionType direction, bool all) = ParseCommand(command);

            if (direction == DirectionType.Other) {
                if (all) {
                    foreach (var exit in exitsWithDoors) {
                        await Store.TcpSend.SendAsync($"{commandType} {exit.Param}");
                    }
                } else {
                    // open the one door in the room
                    if (exitsWithDoors.Length == 1) {
                        await Store.TcpSend.SendAsync($"{commandType} {exitsWithDoors[0].Param}");
                    } else {
                        await Store.ClientInfo.SendAsync($"MULTIPLE DOORS: {string.Join("|", exitsWithDoors.Select(e => e.Param))}");
                    }
                }
            } else {
                // open the door in the direction specified
                var exitsWithDoor = exitsWithDoors.Where(exit => exit.DirType == (int)direction).ToArray();
                if (exitsWithDoor.Length == 1) {
                    await Store.TcpSend.SendAsync($"{commandType} {exitsWithDoor[0].Param}");
                } else {
                    await Store.ClientInfo.SendAsync($"DOOR NOT FOUND FOR: {output}");
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
