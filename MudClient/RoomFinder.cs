using MudClient.Transformers.ParsedOutput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MudClient {
    internal class RoomFinder {
        // The string always ends with one of these
        private readonly HashSet<string> _movementFailedStrings = new HashSet<string> {
            /*The * */ "seems to be closed.",
            "Nah... You feel too relaxed to do that..",
            "In your dreams, or what?",
            "Maybe you should get on your feet first?",
            /*No way%s*/ "You're fighting for your life!",
            "You shudder at the concept of crossing water.",
            "You would need to swim there, you can't just walk it.",
            "You are too exhausted.",
            "You can't ride in there.",
            "You can't ride on water.",
            "You can't ride there on a horse!",
            "You need a boat to go there.",
            "Your mount is too exhausted.",
            "Your mount is engaged in combat!",
            "Your mount ought to be awake and standing first!",
        };
        readonly Regex _movementFailedRegex;

        // todo: is this at the start or end of a line? Probably end?
        private readonly string _blindMoveString = "You can't see a damned thing, you're blinded!";

        // todo: is this at the start or end of a line? Probably end?
        private readonly string _darkMoveString = "It is pitch black...";

        private readonly string _followString = /*^*/ "You follow" /* *.$ */;

        private readonly string _fleeCompleteString = /*^*/ "You flee head over heels.";

        private readonly string _cannotGoThatWay = "Alas, you cannot go that way...";

        // todo: flee failed string

        internal class Room {
            internal string Name;
            internal string Description;
            internal string ExitsLine;
            internal DateTime Time;
            internal List<int> PossibleRoomIds = new List<int>();
        }

        internal class Movement {
            internal string Direction; // n/e/s/w/u/d
            internal DirectionType DirectionType;
            internal DateTime Time;
        }

        internal class OtherMovement {
            internal bool LeaderFollowed = false; // ^You follow *.$
            internal bool MovementFailed = false;
            internal bool MovementSucceeded = false; // for moving while blind or in the dark
            internal bool CouldNotTravel = false;
            internal bool FleeEntered = false;
            // internal bool FleeFailed = false;
            internal bool FleeSucceeded = false;
            internal string Line;
            internal DateTime Time;
        }

        internal List<OtherMovement> OtherMovements = new List<OtherMovement>();
        internal int CurrentOtherMovement = -1;
        internal int ProcessedOtherMovement = -1;
        internal List<Room> SeenRooms = new List<Room>();
        internal int CurrentRoomIndex = -1;
        internal int ProcessedRoomIndex = -1; 
        internal List<Movement> Movements = new List<Movement>();
        internal int VisibleMovement = -1;
        internal int CurrentMovement = -1;
        internal int ProcessedCurrentMovement = -1;


        private int _foundRoomId = -1;


        private readonly MapWindow _map;

        internal RoomFinder(
            MapWindow mapWindow) {
            _map = mapWindow;

            _movementFailedRegex = new Regex(string.Join("$|", _movementFailedStrings), RegexOptions.Compiled);

            Store.TcpSend.Subscribe((message) => {
                ProcessSentMessage(message);
            });

            Store.ParsedOutput.SubscribeAsync(async (parsedOutputs) => {
                await ProcessParsedOutput(parsedOutputs);
            });

            Store.ComplexAlias.Subscribe((output) => {
                var cleaned = output.Trim().ToLower();
                if (cleaned == "qf") {
                    QuickFind();
                }
                if (cleaned.StartsWith("mv ") && cleaned.Length > 3) {
                    var match = Regex.Match(cleaned, @"^mv ((?<count>\d*)?(?<direction>n|e|s|w|u|d))+$");
                    var counts = match.Groups["count"].Captures;
                    var directions = match.Groups["direction"].Captures;
                    for (int i = 0; i < counts.Count; i++) {

                        int count = 1;
                        var countString = counts[i].Value;
                        if (countString.Length > 0) {
                            count = int.Parse(countString);
                        }
                        var direction = directions[i].Value;

                        for (int j = 0; j < count; j++) {
                            MoveVirtualRoom(direction);
                            FindSmartRoomId();
                        }
                    }
                }
            });
        }

        internal void ProcessSentMessage(string output) {
            // process the command the player entered
            output = output.Trim().ToLower();
            if (new[] { "n", "s", "e", "w", "u", "d" }.Contains(output)) {
                MoveVirtualRoom(output);
                Movements.Add(new Movement {
                    Direction = output,
                    DirectionType = DirectionToDirectionType(output),
                    Time = DateTime.Now,
                });
                FindSmartRoomId();
            }
            if (output == "f") {
                OtherMovements.Add(new OtherMovement {
                    FleeEntered = true,
                    Time = DateTime.Now,
                });
            }
        }

        private async Task ProcessParsedOutput(List<ParsedOutput> outputs) {
            if (!MapData.DataLoaded) {
                return;
            }

            foreach (var output in outputs) {
                if (output.Type != ParsedOutputType.Room) {
                    continue;
                }
                var room = new Room {
                    Name = output.Title,
                    Description = string.Join("\n", output.Description) + "\n",
                    ExitsLine = output.Exits,
                    Time = DateTime.Now,
                };
                SeenRooms.Add(room);
                await FindFoundRoomId(room);
                await ProcessMovementReceived(room);
                FindSmartRoomId();
            }

            foreach (var output in outputs) {
                if (output.Type != ParsedOutputType.Raw) {
                    continue;
                }
                foreach (var line in output.Lines) {
                    if (_movementFailedRegex.IsMatch(line)) {
                        OtherMovements.Add(new OtherMovement {
                            MovementFailed = true,
                            Line = line,
                            Time = DateTime.Now,
                        });

                        await ProcessMovementReceived(null, movementFailed: true);
                        FindSmartRoomId();
                    }
                    if (line.StartsWith(_followString)) {
                        OtherMovements.Add(new OtherMovement {
                            LeaderFollowed = true,
                            Line = line,
                            Time = DateTime.Now,
                        });
                        FindSmartRoomId();
                    }
                    if (line.EndsWith(_blindMoveString) || line.EndsWith(_darkMoveString)) {
                        OtherMovements.Add(new OtherMovement {
                            MovementSucceeded = true,
                            Line = line,
                            Time = DateTime.Now,
                        });
                        await ProcessMovementReceived(null, movementFailed: false, darkMovement: true);
                        FindSmartRoomId();
                    }
                    if (line.StartsWith(_fleeCompleteString)) {
                        OtherMovements.Add(new OtherMovement {
                            FleeSucceeded = true,
                            Line = line,
                            Time = DateTime.Now,
                        });
                        FindSmartRoomId();
                    }
                    if (line.StartsWith(_cannotGoThatWay)) {
                        OtherMovements.Add(new OtherMovement {
                            CouldNotTravel = true,
                            Line = line,
                            Time = DateTime.Now,
                        });
                        await ProcessMovementReceived(null, couldNotTravel: true);
                    }
                }
            }
        }

        private void QuickFind() {
            MapData.CurrentRoomId = _foundRoomId;
            MapData.CurrentVirtualRoomId = MapData.CurrentRoomId;

            MapData.CurrentSmartRoomId = MapData.CurrentRoomId;
            CurrentOtherMovement = OtherMovements.Count - 1;
            ProcessedOtherMovement = OtherMovements.Count - 1;
            CurrentRoomIndex = SeenRooms.Count - 1;
            ProcessedRoomIndex = SeenRooms.Count - 1;
            CurrentMovement = Movements.Count - 1;
            ProcessedCurrentMovement = Movements.Count - 1;
            VisibleMovement = Movements.Count - 1;
            

            _map.Invalidate();
        }

        private void MoveVirtualRoom(string movement) {
            if (MapData.CurrentVirtualRoomId == -1) {
                return;
            }

            movement = movement.Trim().ToLower();
            // nsewud follow directions
            bool inValidRoom = MapData.RoomsById.TryGetValue(MapData.CurrentVirtualRoomId, out var currentRoom);
            if (!inValidRoom) {
                return;
            }

            bool roomHasExits = MapData.ExitsByFromRoom.TryGetValue(MapData.CurrentVirtualRoomId, out var currentExits);
            if (!roomHasExits) {
                return;
            }

            var direction = DirectionToDirectionType(movement);

            var link = currentExits.FirstOrDefault(exit => exit.DirType.Value == (int)direction);
            if (link == null) {
                return;
            }
            MapData.CurrentVirtualRoomId = link.ToID.Value;

            _map.Invalidate();
        }

        internal void FindSmartRoomId() {
            /*
            internal List<OtherMovement> OtherMovements = new List<OtherMovement>();
                 LeaderFollowed MovementFailed MovementSucceeded FleeSucceeded
            internal int CurrentOtherMovement = -1;
            internal int ProcessedOtherMovement = -1;
            internal List<Room> SeenRooms = new List<Room>();
            internal int CurrentRoomIndex = -1;
            internal int ProcessedRoomIndex = -1; 
            internal List<Movement> Movements = new List<Movement>();
            internal int CurrentMovement = -1;
            internal int ProcessedCurrentMovement = -1; ;
        */

            // so uh process any movements, other movements, and seen rooms, I guess?

            if (SeenRooms.Count == 0) {
                return;
            }

            List<OtherMovement> unprocessedOtherMovements = new List<OtherMovement>();
            if (CurrentOtherMovement == -1) {
                CurrentOtherMovement = 0;
                // ProcessedCurrentMovement = 0; // -1?
                unprocessedOtherMovements = OtherMovements;
            } else {
                unprocessedOtherMovements = OtherMovements.Skip(CurrentOtherMovement + 1).ToList();
            }

            if (unprocessedOtherMovements.Any(other => other.MovementFailed)) {
                // a movement has failed - for now lets just snap back to the room we're sure we're in
                CurrentOtherMovement = OtherMovements.Count - 1;
                CurrentRoomIndex = SeenRooms.Count - 1;
                CurrentMovement = Movements.Count - 1;
                unprocessedOtherMovements = new List<OtherMovement>();
            }

            List<Room> unprocessedRooms = new List<Room>();
            Room currentRoom;
            if (CurrentRoomIndex == -1) {
                currentRoom = SeenRooms[0];
                CurrentRoomIndex = 0;
                // ProcessedRoomIndex = 0; // -1?
                unprocessedRooms = SeenRooms;
            } else {
                currentRoom = SeenRooms[CurrentRoomIndex];
                unprocessedRooms = SeenRooms.Skip(CurrentRoomIndex + 1).ToList();
            }

            CurrentMovement += unprocessedRooms.Count();
            CurrentRoomIndex = SeenRooms.Count - 1;
            currentRoom = SeenRooms.Last();

            CurrentMovement += unprocessedOtherMovements.Where(other => other.CouldNotTravel).Count();
            CurrentOtherMovement = OtherMovements.Count - 1;

            List<Movement> unprocessedMovements = new List<Movement>();
            if (CurrentMovement == -1) {
                CurrentMovement = 0;
                // ProcessedCurrentMovement = 0; // -1?
                unprocessedMovements = Movements;
            } else {
                unprocessedMovements = Movements.Skip(CurrentMovement + 1).ToList(); // +1?
            }

            if (currentRoom.PossibleRoomIds.Count == 1) {
                MapData.CurrentSmartRoomId = currentRoom.PossibleRoomIds.Single();
                foreach (var movement in unprocessedMovements) {
                    MapData.ExitsByFromRoom.TryGetValue(MapData.CurrentSmartRoomId, out var currentExits);

                    var link = currentExits?.FirstOrDefault(exit => exit.DirType.Value == (int)movement.DirectionType);
                    if (link == null) {
                        continue;
                    }
                    MapData.CurrentSmartRoomId = link.ToID.Value;
                }
                _map.Invalidate();

                ProcessedCurrentMovement = Movements.Count - 1;
            } else {
                // can't be certain the now room on the map is correct, so go off last 'quick' position
                foreach (var movement in Movements.Skip(ProcessedCurrentMovement + 1)) {
                    MapData.ExitsByFromRoom.TryGetValue(MapData.CurrentSmartRoomId, out var currentExits);

                    var link = currentExits?.FirstOrDefault(exit => exit.DirType.Value == (int)movement.DirectionType);
                    if (link == null) {
                        continue;
                    }
                    MapData.CurrentSmartRoomId = link.ToID.Value;

                }
                _map.Invalidate();

                ProcessedCurrentMovement = Movements.Count - 1;
            }
        }

        // unused - using ProcessMovementReceived instead
        private async Task FindFoundRoomId(Room room) {
            var possibleRooms = PossibleRoomMatcher.FindPossibleRooms(room);
            room.PossibleRoomIds = possibleRooms.Select(r => r.RoomData.ObjID.Value).ToList();

            if (possibleRooms.Count != 0) {
                _foundRoomId = possibleRooms.First().RoomData.ObjID.Value;

                if (possibleRooms.Count > 1) {
                    await Store.ClientInfo.SendAsync("Map: Multiple matching rooms found.");

                    MapData.RoomsById.TryGetValue(MapData.CurrentRoomId, out var previousRoom);
                    if (previousRoom != null) {
                        // if there are multiple rooms try to pick a match in the same ZoneId
                        var matchesZone = possibleRooms.Where(r => r.RoomData.ZoneID.Value == previousRoom.ZoneID.Value).ToList();

                        // todo: if there is more than one match then prefer matching rooms close to the previous room
                        if (matchesZone.Count != 0) {
                            _foundRoomId = matchesZone.First().RoomData.ObjID.Value;
                        }
                    }
                }
            } else {
                await Store.ClientInfo.SendAsync("Map: No matching rooms found.");
            }
        }

        // todo: make qf clear all this stuff
        // process a movement of some kind (e.g saw a new room, or received a message saying a movement failed)
        private async Task ProcessMovementReceived(Room newRoom, bool movementFailed = false, bool darkMovement = false, bool couldNotTravel = false) {
            if (VisibleMovement < Movements.Count) {
                VisibleMovement++;
            }

            Movement movement = null;
            if (VisibleMovement < Movements.Count && VisibleMovement >= 0) {
                movement = Movements[VisibleMovement];
            }
            if (couldNotTravel) {
                // failed - don't need to do anything to the current room
            } else if (movementFailed) {
                // failed - don't need to do anything to the current room
                // but do reset the spammed to room
                await Store.ClientInfo.SendAsync("Map: Move failed, resetting virtual room");
                MapData.CurrentVirtualRoomId = MapData.CurrentRoomId;
            } else if (darkMovement) {
                await MoveCurrentRoom(null, movement);
            } else {
                // assume it was a normal movement
                // could be a move via fleeing, following, or entered direction but we ignore the differences for now
                await MoveCurrentRoom(newRoom, movement);
            }
        }

        private async Task MoveCurrentRoom(Room newRoom, Movement movement) {
            MapData.RoomsById.TryGetValue(MapData.CurrentRoomId, out var previousRoom);

            List<PossibleRoom> matchedRooms = new List<PossibleRoom>();
            if (newRoom != null) {
                matchedRooms = PossibleRoomMatcher.FindPossibleRooms(newRoom);
                newRoom.PossibleRoomIds = matchedRooms.Select(r => r.RoomData.ObjID.Value).ToList();
            }

            if (matchedRooms.Count == 0) {
                await Store.ClientInfo.SendAsync("Map: No matching rooms found.");
            }
            if (matchedRooms.Count > 1) {
                await Store.ClientInfo.SendAsync("Map: Multiple matching rooms found.");
            }

            if (previousRoom != null && movement != null) {
                MapData.ExitsByFromRoom.TryGetValue(MapData.CurrentRoomId, out var previousRoomExits);


                ZmudDbExitTblRow exit = null;
                if (previousRoomExits != null) {
                    exit = previousRoomExits.FirstOrDefault(e => e.DirType.Value == (int)movement.DirectionType);
                }

                if (exit != null) {
                    int newRoomId = exit.ToID.Value;

                    if (matchedRooms.Count != 0 && !matchedRooms.Any(mr => mr.RoomData.ObjID.Value == newRoomId)) {
                        await Store.ClientInfo.SendAsync("Map: Moved to new room but didn't match an expected found room");
                    }

                    // currently trust the map find more than the movement direction code
                    if (matchedRooms.Count == 1) {
                        MapData.CurrentRoomId = matchedRooms[0].RoomData.ObjID.Value;
                    } else {
                        MapData.CurrentRoomId = newRoomId;
                    }

                    _map.Invalidate();
                    return;
                }
            }

            // didn't receive a direction - rely purely on map find

            if (matchedRooms.Count != 0) {
                var previousRoomId = MapData.CurrentRoomId;
                MapData.CurrentRoomId = matchedRooms.First().RoomData.ObjID.Value;

                if (matchedRooms.Count > 1) {
                    await Store.ClientInfo.SendAsync("Map: Multiple matching rooms found.");

                    if (previousRoom != null) {
                        var matchedAdjacentRooms = new List<int>();
                        MapData.ExitsByFromRoom.TryGetValue(previousRoom.ObjID.Value, out var previousRoomExits);
                        if (previousRoomExits != null) {
                            var adjacentRoomIds = previousRoomExits.Select(previousExit => previousExit.ToID.Value);
                            matchedAdjacentRooms = matchedRooms.Select(r => r.RoomData.ObjID.Value).Intersect(adjacentRoomIds).ToList();
                        }

                        var matchesZone = matchedRooms.Where(r => r.RoomData.ZoneID.Value == previousRoom.ZoneID.Value).ToList();

                        if (matchedAdjacentRooms.Count != 0) {
                            MapData.CurrentRoomId = matchedAdjacentRooms.First();
                        } else if (matchesZone.Count != 0) {
                            MapData.CurrentRoomId = matchesZone.First().RoomData.ObjID.Value;
                        }
                    }
                }

                // didn't recieve a direction so we should make the virtual room following the current room if we can
                if (previousRoomId == MapData.CurrentVirtualRoomId) {
                    MapData.CurrentVirtualRoomId = MapData.CurrentRoomId;
                }
            } else {
                await Store.ClientInfo.SendAsync("Map: No matching rooms found.");
            }
        }


        private DirectionType DirectionToDirectionType(string direction) {
            switch (direction) {
                case "u":
                    return DirectionType.Up;
                case "d":
                    return DirectionType.Down;
                case "n":
                    return DirectionType.North;
                case "s":
                    return DirectionType.South;
                case "e":
                    return DirectionType.East;
                case "w":
                    return DirectionType.West;
                default:
                    throw new Exception();
            }
        }
    }
}
