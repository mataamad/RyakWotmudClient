using MudClient.Extensions;
using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class RoomFinder {
        private readonly BufferBlock<List<FormattedOutput>> _outputBuffer;
        private readonly BufferBlock<string> _sentMessageBuffer;
        private readonly BufferBlock<string> _sendSpecialMessageBuffer;

        // The string always ends with one of these
        private HashSet<string> _movementFailedStrings = new HashSet<string> {
            /*The * */ "seems to be closed.",
            "Nah... You feel too relaxed to do that...",
            "In your dreams, or what?",
            "Maybe you should get on your feet first?",
            /*No way%s*/ "You're fighting for your life!",
            "You shudder at the concept of crossing water.",
            "You would need to swim there, you can't just walk it.",
            "You are too exhausted.",
            "You can't ride in there.",
            "You can't ride on water.",
            "You can't ride there on a horse!",
            "You need a bot to go there.",
            "Your mount is too exhausted.",
            "Your mount is engaged in combat!",
            "Your mount ought to be awake and standing first!",
        };
        Regex _movementFailedRegex;

        // todo: is this at the start or end of a line? Probably end?
        private string _blindMoveString = "You can't see a damned thing, you're blinded!";

        // todo: is this at the start or end of a line? Probably end?
        private string _darkMoveString = "It is pitch black...";

        private string _followString = /*^*/ "You follow" /* *.$ */;

        private string _fleeCompleteString = /*^*/ "You flee head over heels.";

        private string _cannotGoThatWay = "Alas, you cannot go that way...";

        public class Room {
            public string Name;
            public string Description;
            public string ExitsLine;
            public DateTime Time;
            public List<int> PossibleRoomIds = new List<int>();
        }

        public class Movement {
            public string Direction; // n/e/s/w/u/d
            public DirectionType DirectionType;
            public DateTime Time;
        }

        public class OtherMovement {
            public bool LeaderFollowed = false; // ^You follow *.$
            public bool MovementFailed = false;
            public bool MovementSucceeded = false; // for moving while blind or in the dark
            public bool CouldNotTravel = false;
            public bool FleeEntered = false;
            // public bool FleeFailed = false;
            public bool FleeSucceeded = false;
            public string Line;
            public DateTime Time;
        }

        public List<OtherMovement> OtherMovements = new List<OtherMovement>();
        public int CurrentOtherMovement = -1;
        public int ProcessedOtherMovement = -1;
        public List<Room> SeenRooms = new List<Room>();
        public int CurrentRoomIndex = -1;
        public int ProcessedRoomIndex = -1; 
        public List<Movement> Movements = new List<Movement>();
        public int CurrentMovement = -1;
        public int ProcessedCurrentMovement = -1; 



        private readonly MapWindow _map;

        public RoomFinder(BufferBlock<List<FormattedOutput>> outputBuffer, BufferBlock<string> sentMessageBuffer, BufferBlock<string> sendSpecialMessageBuffer, MapWindow mapWindow) {
            _outputBuffer = outputBuffer;
            _sentMessageBuffer = sentMessageBuffer;
            _sendSpecialMessageBuffer = sendSpecialMessageBuffer;
            _map = mapWindow;

            _movementFailedRegex = new Regex(string.Join("$|", _movementFailedStrings), RegexOptions.Compiled);
        }

        public void LoopOnNewThread(CancellationToken cancellationToken)
        {
            Task.Run(() => LoopFormattedOutput(cancellationToken));
            Task.Run(() => LoopSentMessage(cancellationToken));
            Task.Run(() => LoopSpecialMessage(cancellationToken));
        }

        private enum RoomSeenState {
            NotStarted,
            SeenTitle,
            SeenDescirption,
            SeenExits
        }

        private async Task LoopFormattedOutput(CancellationToken cancellationToken) {
            while (!_map.DataLoaded) {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

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
                            Time = DateTime.Now,
                        };
                        roomName = null;
                        state = RoomSeenState.NotStarted;
                        SeenRooms.Add(room);
                        FindCurrentRoomId(room);
                        FindSmartRoomId();
                    }
                }

                foreach (var output in outputs) {
                    var splitIntoLines = output.Text.Split('\n');
                    foreach (var line in splitIntoLines) {
                        if (_movementFailedRegex.IsMatch(line)) {
                            OtherMovements.Add(new OtherMovement {
                                MovementFailed = true,
                                Line = line,
                                Time = DateTime.Now,
                            });
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
                        }
                    }

                }
            }
        }

        private async Task LoopSentMessage(CancellationToken cancellationToken) {
            while (!_map.DataLoaded) {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested) {
                string output = await _sentMessageBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

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

                output = output.Trim().ToLower();
                if (output == "qf") {
                    QuickFind();
                }
            }
        }

        private void QuickFind() {
            _map.CurrentVirtualRoomId = _map.CurrentRoomId;

            _map.CurrentSmartRoomId = _map.CurrentRoomId;
            CurrentOtherMovement = OtherMovements.Count - 1;
            ProcessedOtherMovement = OtherMovements.Count - 1;
            CurrentRoomIndex = SeenRooms.Count - 1;
            ProcessedRoomIndex = SeenRooms.Count - 1;
            CurrentMovement = Movements.Count - 1;
            ProcessedCurrentMovement = Movements.Count - 1;


            _map.Invalidate();
        }

        private void MoveVirtualRoom(string movement) {
            movement = movement.Trim().ToLower();
            // nsewud follow directions
            bool inValidRoom = _map.RoomsById.TryGetValue(_map.CurrentVirtualRoomId, out var currentRoom);
            if (!inValidRoom) {
                return;
            }

            bool roomHasExits = _map.ExitsByFromRoom.TryGetValue(_map.CurrentVirtualRoomId, out var currentExits);
            if (!roomHasExits) {
                return;
            }

            var direction = DirectionToDirectionType(movement);

            var link = currentExits.FirstOrDefault(exit => exit.DirType.Value == (int)direction);
            if (link == null) {
                return;
            }
            _map.CurrentVirtualRoomId = link.ToID.Value;

            _map.Invalidate();
        }

        public void FindSmartRoomId() {
            /*
            public List<OtherMovement> OtherMovements = new List<OtherMovement>();
                 LeaderFollowed MovementFailed MovementSucceeded FleeSucceeded
            public int CurrentOtherMovement = -1;
            public int ProcessedOtherMovement = -1;
            public List<Room> SeenRooms = new List<Room>();
            public int CurrentRoomIndex = -1;
            public int ProcessedRoomIndex = -1; 
            public List<Movement> Movements = new List<Movement>();
            public int CurrentMovement = -1;
            public int ProcessedCurrentMovement = -1; ;
        */

            // so uh process any movements, other movements, and seen rooms, I guess?

            if (!SeenRooms.Any()) {
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
                _map.CurrentSmartRoomId = currentRoom.PossibleRoomIds.Single();
                foreach (var movement in unprocessedMovements) {
                    _map.ExitsByFromRoom.TryGetValue(_map.CurrentSmartRoomId, out var currentExits);

                    var link = currentExits?.FirstOrDefault(exit => exit.DirType.Value == (int)movement.DirectionType);
                    if (link == null) {
                        continue;
                    }
                    _map.CurrentSmartRoomId = link.ToID.Value;
                }
                _map.Invalidate();

                ProcessedCurrentMovement = Movements.Count - 1;
            } else {
                // can't be certain the now room on the map is correct, so go off last 'quick' position
                foreach (var movement in Movements.Skip(ProcessedCurrentMovement + 1)) {
                    _map.ExitsByFromRoom.TryGetValue(_map.CurrentSmartRoomId, out var currentExits);

                    var link = currentExits?.FirstOrDefault(exit => exit.DirType.Value == (int)movement.DirectionType);
                    if (link == null) {
                        continue;
                    }
                    _map.CurrentSmartRoomId = link.ToID.Value;

                }
                _map.Invalidate();

                ProcessedCurrentMovement = Movements.Count - 1;
            }
        }

        public void FindCurrentRoomId(Room room) {

            var possibleRooms = PossibleRoomMatcher.FindPossibleRooms(_map, room);

            if (possibleRooms.Any()) {
                _map.CurrentRoomId = possibleRooms.First().RoomData.ObjID.Value;
                _map.Invalidate();
            }
            room.PossibleRoomIds = possibleRooms.Select(r => r.RoomData.ObjID.Value).ToList();
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
