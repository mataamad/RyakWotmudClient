using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    public static class  MapData {
        public static Dictionary<int, ZmudDbOjectTblRow> RoomsById { get; private set; }
        public static Dictionary<int, ZmudDbOjectTblRow[]> RoomsByZone { get; private set; }
        public static Dictionary<string, ZmudDbOjectTblRow[]> RoomsByName { get; private set; }
        public static Dictionary<string, ZmudDbOjectTblRow[]> RoomsByDescription { get; private set; }
        public static Dictionary<string, ZmudDbOjectTblRow[]> RoomsByFirstLineOfDescription { get; private set; }

        public static Dictionary<int, ZmudDbExitTblRow> ExitsById;
        public static Dictionary<int, ZmudDbExitTblRow[]> ExitsByFromRoom;
        public static Dictionary<int, ZmudDbExitTblRow[]> ExitsByToRoom;

        public static bool DataLoaded { get; private set; } = false;

        private static ZmudDbZoneTbl[] _zones;


        // todo: these probably shouldn't be here, they're not really part of the map data, they're the player location.
        public static int CurrentRoomId { get; set; } = 0;
        public static int CurrentVirtualRoomId { get; set; } = -1;
        public static int CurrentSmartRoomId { get; set; } = -1;



        public static void Load() {
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
