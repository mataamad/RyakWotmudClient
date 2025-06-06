﻿using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    internal class MapDataLoader {

        internal ZmudDbOjectTblRow[] Rooms { get; private set; }
        internal ZmudDbExitTblRow[] Exits { get; private set; }
        internal ZmudDbZoneTbl[] Zones { get; private set; }

        internal class DeserializeObject {
            internal ZmudDbOjectTblRow[] Rooms;
            internal ZmudDbExitTblRow[] Exits;
            internal ZmudDbZoneTbl[] Zones;
        }

        const string MapFilename = "./mapData.json.gzip";


        internal void LoadData() {
            LoadFromDb(); // loading from DB because it's a lot faster - use json if it's all you have access to

            /*using (var file = File.OpenRead(MapFilename))
            using (var gZipStream = new GZipStream(file, CompressionMode.Decompress, leaveOpen: true)) {
                var data = System.Text.Json.JsonSerializer.Deserialize<DeserializeObject>(gZipStream);
                Rooms = data.Rooms;
                Exits = data.Exits;
                Zones = data.Zones;
            }*/
        }

        // private const string ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=""Mud DB Dump"";Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        // private const string ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=""ZmudDump2021"";Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private const string ConnectionString = @"Data Source=(local);Initial Catalog=""ZmudDump2021"";Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private void LoadFromDb() {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            string roomsQuery = "SELECT * FROM  ObjectTbl";
            string exitsQuery = "SELECT * FROM  ExitTbl";
            string zonesQuery = "SELECT * FROM  ZoneTbl";
            Rooms = [.. connection.Query<ZmudDbOjectTblRow>(roomsQuery)];
            Exits = [.. connection.Query<ZmudDbExitTblRow>(exitsQuery)];
            Zones = [.. connection.Query<ZmudDbZoneTbl>(zonesQuery)];
        }

        // unused
        private void JsonGZipMap() {
            using var file = File.OpenWrite(MapFilename);
            using var gZipStream = new GZipStream(file, CompressionMode.Compress, leaveOpen: true);

            var json = System.Text.Json.JsonSerializer.Serialize(this);
            var bytes = Encoding.UTF8.GetBytes(json);
            gZipStream.Write(bytes, 0, bytes.Length);
        }


        /*internal ZmudDbOjectTblRow[] GetBlightRooms() {
            // var exits = _exits.Where(e => e. // exits is a lot harder without joins
            var blightZone = _zones.Where(z => z.Name == "Blight").FirstOrDefault();
            var blightRooms = _rooms.Where(r => r.ZoneID == blightZone.ZoneID).ToArray();
            return blightRooms;
        }*/

        /*internal ZmudDbExitTblRow[] GetExits(Dictionary<int, ZmudDbOjectTblRow> rooms) {

            var exits = _exits.Where(exit => rooms.ContainsKey(exit.FromID.Value) || rooms.ContainsKey(exit.ToID.Value));
            return exits.ToArray();
        }*/

        // debug method
        private void GenerateClassDeclaration() {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            // string roomsQuery = "SELECT * FROM ObjectTbl";
            string roomsQuery = "SELECT * FROM ExitTbl";
            // string roomsQuery = "SELECT * FROM  ZoneTbl";
            using var command = new SqlCommand(roomsQuery, connection);
            using SqlDataReader reader = command.ExecuteReader();
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
                    Debug.WriteLine($"internal {typesDict[type.Name]} {reader.GetName(i)} {{ get; set; }}");
                    i++;
                }
                return;
                // Debug.WriteLine(string.Join(",", values));
            }
        }
    }
}
