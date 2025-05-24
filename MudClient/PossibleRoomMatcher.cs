using System;
using System.Collections.Generic;
using System.Linq;
using static MudClient.RoomFinder;

namespace MudClient {
    internal class PossibleRoom {
        internal ZmudDbOjectTblRow RoomData;
    }

    class PossibleRoomMatcher {
        // Return a list of matching rooms in the mud for a given Room name, description, and exits
        // If no rooms are returned, then no match was found
        // If one room is returned then it is likely to be the correct room
        // If multiple rooms are returned then it could be any of these rooms and other methods should be used to determine the exact room (e.g. pick the only one adjacent to the last room)
        internal static List<PossibleRoom> FindPossibleRooms(Room room) {
            // for testing purposes I'm just jumping to the first room with the same room name
            ZmudDbOjectTblRow[] possibleRoomsByName = null;
            MapData.RoomsByName.TryGetValue(room.Name, out possibleRoomsByName);

            if (possibleRoomsByName == null) {
                // no rooms found with the same name.  For now just return.  Could do fuzzy matching of some kind.
                return new List<PossibleRoom>();
            }
            if (possibleRoomsByName.Length == 1) {
                return possibleRoomsByName.Select(r => new PossibleRoom { RoomData = r }).ToList();
            }

            ZmudDbOjectTblRow[] possibleRooms = null;

        
            // todo: should probably completely strip newlines from RoomsByDescription rather than doing this
            if (MapData.RoomsByDescription.TryGetValue(room.Description.Replace("\n", "\r\n"), out ZmudDbOjectTblRow[] roomsWithSameDescription)) {
                possibleRooms = roomsWithSameDescription.Intersect(possibleRoomsByName).ToArray();
            } else {
                possibleRooms = new ZmudDbOjectTblRow[0];
            }

            if (!possibleRooms.Any()) {
                // the zmud mapper only uses the first line of the description so it's more likely to be correct
                if (MapData.RoomsByFirstLineOfDescription.TryGetValue(room.Description.Split('\n').FirstOrDefault().Trim(), out ZmudDbOjectTblRow[] roomsWithSameFirstLineDescription)) {
                    possibleRooms = roomsWithSameFirstLineDescription.Intersect(possibleRoomsByName).ToArray();
                }
            }

            if (!possibleRooms.Any()) {
                // no first line match either, go back to using the room name only.  Match by room name has multiple results
                possibleRooms = possibleRoomsByName;
            }

            if (possibleRooms.Length <= 1) {
                return possibleRooms.Select(r => new PossibleRoom { RoomData = r }).ToList();
            }

            // [ obvious exits: N S W D ]
            var seenExits = new HashSet<string>(room.ExitsLine.Trim().Replace("[ obvious exits:", "").Replace(" ]", "").Split(' ').Where(s => !string.IsNullOrEmpty(s)));
            // try matching on exits.
            var possibleRoomsWithExits = new List<ZmudDbOjectTblRow>();
            foreach (var possibleRoom in possibleRooms) {
                ZmudDbExitTblRow[] exits = new ZmudDbExitTblRow[0];
                MapData.ExitsByFromRoom.TryGetValue(possibleRoom.ObjID.Value, out exits);

                int seen = 0;
                int unseen = 0;
                foreach (var exit in exits) {
                    if (seenExits.Contains(DirectionTypeToExitString((DirectionType)exit.DirType.Value))) {
                        seen++;
                    } else if (string.IsNullOrEmpty(exit.Param)) {
                        // we aren't marking rooms with doors as unseen as some of them can be hidden. It'd be nice to only apply this for doors that can be hidden.
                        unseen++;
                    }
                }
                // todo: this doesn't take into account possibly unseen doors
                if (seen == seenExits.Count && unseen == 0) {
                    possibleRoomsWithExits.Add(possibleRoom);
                }
            }

            if (possibleRoomsWithExits.Any()) {
                return possibleRoomsWithExits.Select(r => new PossibleRoom { RoomData = r }).ToList();
            } else {
                // exits are probably wrong
                return possibleRooms.Select(r => new PossibleRoom { RoomData = r }).ToList();
            }
        }

        private static string DirectionTypeToExitString(DirectionType direction) {
            switch (direction) {
                case DirectionType.Up:
                    return "U";
                case DirectionType.Down:
                    return "D";
                case DirectionType.North:
                    return "N";
                case DirectionType.South:
                    return "S";
                case DirectionType.East:
                    return "E";
                case DirectionType.West:
                    return "W";
                default:
                    throw new Exception();
            }
        }
    }
}
