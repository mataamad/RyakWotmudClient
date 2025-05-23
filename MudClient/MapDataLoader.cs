using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    public class MapDataLoader {

        public ZmudDbOjectTblRow[] Rooms { get; private set; }
        public ZmudDbExitTblRow[] Exits { get; private set; }
        public ZmudDbZoneTbl[] Zones { get; private set; }

        public class DeserializeObject {
            public ZmudDbOjectTblRow[] Rooms;
            public ZmudDbExitTblRow[] Exits;
            public ZmudDbZoneTbl[] Zones;
        }

        const string MapFilename = "./mapData.json.gzip";


        public void LoadData() {
            LoadFromDb(); // loading from DB because it's a lot faster - use json if it's all you have access to

            /*var serializer = new JsonSerializer();
            using (var file = File.OpenRead(MapFilename))
            using (var gZipStream = new GZipStream(file, CompressionMode.Decompress, leaveOpen: true))
            using (var streamReader = new StreamReader(gZipStream))
            using (var jsonTextReader = new JsonTextReader(streamReader)) {
               // var data2 = streamReader.ReadToEnd();
               // var data3 = serializer.Deserialize(jsonTextReader);
               var data = serializer.Deserialize<DeserializeObject>(jsonTextReader);
                Rooms = data.Rooms;
                Exits = data.Exits;
                Zones = data.Zones;
            }*/
        }

        // private const string ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=""Mud DB Dump"";Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        // private const string ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=""ZmudDump2021"";Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private const string ConnectionString = @"Data Source=(local);Initial Catalog=""ZmudDump2021"";Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private void LoadFromDb() {
            using (var connection = new SqlConnection(ConnectionString)) {
                connection.Open();

                string roomsQuery = "SELECT * FROM  ObjectTbl";
                string exitsQuery = "SELECT * FROM  ExitTbl";
                string zonesQuery = "SELECT * FROM  ZoneTbl";
                Rooms = connection.Query<ZmudDbOjectTblRow>(roomsQuery).ToArray();
                Exits = connection.Query<ZmudDbExitTblRow>(exitsQuery).ToArray();
                Zones = connection.Query<ZmudDbZoneTbl>(zonesQuery).ToArray();
            }
        }

        // unused
        private void JsonGZipMap() {
            using (var memoryStream = new MemoryStream())
            using (var file = File.OpenWrite(MapFilename))
            using (var gZipStream = new GZipStream(file, CompressionMode.Compress, leaveOpen: true)) {
                var json = JsonConvert.SerializeObject(this);
                var serializer = new JsonSerializer();

                var bytes = Encoding.ASCII.GetBytes(json);
                gZipStream.Write(bytes, 0, bytes.Length);
            }
        }


        /*public ZmudDbOjectTblRow[] GetBlightRooms() {
            // var exits = _exits.Where(e => e. // exits is a lot harder without joins
            var blightZone = _zones.Where(z => z.Name == "Blight").FirstOrDefault();
            var blightRooms = _rooms.Where(r => r.ZoneID == blightZone.ZoneID).ToArray();
            return blightRooms;
        }*/

        /*public ZmudDbExitTblRow[] GetExits(Dictionary<int, ZmudDbOjectTblRow> rooms) {

            var exits = _exits.Where(exit => rooms.ContainsKey(exit.FromID.Value) || rooms.ContainsKey(exit.ToID.Value));
            return exits.ToArray();
        }*/

        // debug method
        private void GenerateClassDeclaration() {
            using (var connection = new SqlConnection(ConnectionString)) {
                connection.Open();

                // string roomsQuery = "SELECT * FROM ObjectTbl";
                string roomsQuery = "SELECT * FROM ExitTbl";
                // string roomsQuery = "SELECT * FROM  ZoneTbl";
                using (var command = new SqlCommand(roomsQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        object[] values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        int i = 0;
                        foreach (var obj in values) {
                            var type = obj.GetType();
                            var typesDict = new Dictionary<string, string> {
                                { "Int32", "int?" },
                                { "Boolean", "bool?" },
                                { "String", "string" },
                                { "DateTime", "DateTime" },
                            };
                            Debug.WriteLine($"public {typesDict[type.Name]} {reader.GetName(i)} {{ get; set; }}");
                            i++;
                        }
                        return;
                        Debug.WriteLine(string.Join(",", values));
                    }
                }
            }
        }
    }
}
