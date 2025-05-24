using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    internal static class  MapData {
        internal static Dictionary<int, ZmudDbOjectTblRow> RoomsById { get; private set; }
        internal static Dictionary<int, ZmudDbOjectTblRow[]> RoomsByZone { get; private set; }
        internal static Dictionary<string, ZmudDbOjectTblRow[]> RoomsByName { get; private set; }
        internal static Dictionary<string, ZmudDbOjectTblRow[]> RoomsByDescription { get; private set; }
        internal static Dictionary<string, ZmudDbOjectTblRow[]> RoomsByFirstLineOfDescription { get; private set; }

        internal static Dictionary<int, ZmudDbExitTblRow> ExitsById;
        internal static Dictionary<int, ZmudDbExitTblRow[]> ExitsByFromRoom;
        internal static Dictionary<int, ZmudDbExitTblRow[]> ExitsByToRoom;

        internal static bool DataLoaded { get; private set; } = false;

        private static ZmudDbZoneTbl[] _zones;


        // todo: these probably shouldn't be here, they're not really part of the map data, they're the player location.
        internal static int CurrentRoomId { get; set; } = 0;
        internal static int CurrentVirtualRoomId { get; set; } = -1;
        internal static int CurrentSmartRoomId { get; set; } = -1;



        internal static void Load() {
            var dataLoader = new MapDataLoader();
            dataLoader.LoadData();

            RoomsById = dataLoader.Rooms.ToDictionary(room => room.ObjID.Value, room => room);
            RoomsByZone = dataLoader.Rooms.GroupBy(room => room.ZoneID.Value).ToDictionary(group => group.Key, group => group.ToArray());
            RoomsByName = dataLoader.Rooms.GroupBy(room => room.Name).ToDictionary(group => group.Key, group => group.ToArray());
            RoomsByDescription = dataLoader.Rooms.GroupBy(room => room.Desc).ToDictionary(group => group.Key, group => group.ToArray());
            RoomsByFirstLineOfDescription = dataLoader.Rooms.GroupBy(room => room.Desc.Split('\r', '\n').FirstOrDefault().Trim()).ToDictionary(group => group.Key, group => group.ToArray());

            ExitsById = dataLoader.Exits.ToDictionary(exit => exit.ExitID.Value, exit => exit);
            ExitsByFromRoom = dataLoader.Exits.GroupBy(exit => exit.FromID.Value).ToDictionary(group => group.Key, group => group.ToArray());
            ExitsByToRoom = dataLoader.Exits.GroupBy(exit => exit.ToID.Value).ToDictionary(group => group.Key, group => group.ToArray());

            _zones = dataLoader.Zones;

            DataLoaded = true;
        }
    }
}
