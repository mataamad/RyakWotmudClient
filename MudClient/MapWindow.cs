using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MudClient.RoomFinder;

namespace MudClient {
    public partial class MapWindow : Form {

        private MapDataLoader _dataLoader;

        private Dictionary<int, ZmudDbOjectTblRow> _roomsById;
        private Dictionary<int, ZmudDbOjectTblRow[]> _roomsByZone;
        private Dictionary<string, ZmudDbOjectTblRow[]> _roomsByName;
        private Dictionary<string, ZmudDbOjectTblRow[]> _roomsByDescription;
        private Dictionary<string, ZmudDbOjectTblRow[]> _roomsByFirstLineOfDescription;

        private Dictionary<int, ZmudDbExitTblRow> _exitsById;
        private Dictionary<int, ZmudDbExitTblRow[]> _exitsByFromRoom;
        private Dictionary<int, ZmudDbExitTblRow[]> _exitsByToRoom;

        private ZmudDbZoneTbl[] _zones;


        private const int DefaultRoomSize = 6;
        private const double MaxScaling = 0.08;
        private const double MinScaling = 0.1;

        private int _currentRoomId = 0;
        private int _currentVirtualRoomId = -1;

        private int _prevOffsetX = 0;
        private int _prevOffsetY = 0;
        private int _prevZoneId = -1;

        public bool DataLoaded = false;

        public MapWindow() {
            InitializeComponent();
        }

        public void LoadData() {

            _dataLoader = new MapDataLoader();
            _dataLoader.LoadData();

            _roomsById = _dataLoader.Rooms.ToDictionary(room => room.ObjID.Value, room => room);
            _roomsByZone = _dataLoader.Rooms.GroupBy(room => room.ZoneID.Value).ToDictionary(group => group.Key, group => group.ToArray());
            _roomsByName = _dataLoader.Rooms.GroupBy(room => room.Name).ToDictionary(group => group.Key, group => group.ToArray());
            _roomsByDescription = _dataLoader.Rooms.GroupBy(room => room.Desc).ToDictionary(group => group.Key, group => group.ToArray());
            _roomsByFirstLineOfDescription = _dataLoader.Rooms.GroupBy(room => room.Desc.Split('\r','\n').FirstOrDefault().Trim()).ToDictionary(group => group.Key, group => group.ToArray());

            _exitsById = _dataLoader.Exits.ToDictionary(exit => exit.ExitID.Value, exit => exit);
            _exitsByFromRoom = _dataLoader.Exits.GroupBy(exit => exit.FromID.Value).ToDictionary(group => group.Key, group => group.ToArray());
            _exitsByToRoom = _dataLoader.Exits.GroupBy(exit => exit.ToID.Value).ToDictionary(group => group.Key, group => group.ToArray());

            _zones = _dataLoader.Zones;

            DataLoaded = true;
            this.Invalidate();
        }

        // todo: tidy up all this scaling and offset stuff
        private void OnPaint(object sender, PaintEventArgs e) {
            if (_dataLoader == null) {
                return;
            }

            Graphics g = e.Graphics;

            bool currentRoomExists = _roomsById.ContainsKey(_currentRoomId);
            bool currentVirtualRoomExists = _roomsById.ContainsKey(_currentVirtualRoomId);

            ZmudDbOjectTblRow currentRoom = null;
            if (currentVirtualRoomExists) {
                currentRoom = _roomsById[_currentVirtualRoomId];
            } else if (currentRoomExists) {
                _currentVirtualRoomId = _currentRoomId;
                currentRoom = _roomsById[_currentVirtualRoomId];
            } else {
                return;
            }

            var currentZoneId = currentRoom.ZoneID.Value;

            var roomsInZone = _roomsByZone[currentZoneId];

            // todo: could cache this
            HashSet<int> exitsInZone = new HashSet<int>();
            foreach (var room in roomsInZone) {
                if (_exitsByFromRoom.ContainsKey(room.ObjID.Value)) {
                    foreach (var exit in _exitsByFromRoom[room.ObjID.Value]) {
                        exitsInZone.Add(exit.ExitID.Value);
                    }
                }
                if (_exitsByToRoom.ContainsKey(room.ObjID.Value)) {
                    foreach (var exit in _exitsByToRoom[room.ObjID.Value]) {
                        exitsInZone.Add(exit.ExitID.Value);
                    }
                }
            }

            var minX = roomsInZone.Min(o => o.X.Value);
            var minY = roomsInZone.Min(o => o.Y.Value);
            // var minZ = roomsInZone.Min(o => o.Z.Value);
            var maxX = roomsInZone.Max(o => o.X.Value);
            var maxY = roomsInZone.Max(o => o.Y.Value);
            // var maxZ = roomsInZone.Max(o => o.Z.Value);

            var screenWidth = e.ClipRectangle.Width;
            var screenHeight = e.ClipRectangle.Height;

            var zoneWidth = maxX - minX;
            var zoneHeight = maxY - minY;

            double scale = Math.Min(screenWidth / ((double)zoneWidth), screenHeight / ((double)zoneHeight));
            // Debug.WriteLine($"scaleX: {scaleX}, scaleY: {scaleY}");
            if (scale < MaxScaling) {
                scale = MaxScaling;
            }
            if (scale > MinScaling) {
                scale = MinScaling;
            }
            int roomSize = (int)((scale / 0.05) * DefaultRoomSize); // the 0.05 is kind of arbitary - it's a size that gives reasonable room sizes

            var scaledZoneWidth = scale * zoneWidth;
            var scaledZoneHeight = scale * zoneHeight;

            int offsetX = 0;
            int offsetY = 0;

            if (scaledZoneWidth < screenWidth) {
                offsetX = (int)((screenWidth - scaledZoneWidth) / 2);
            } else {
                // try to use the previous offset unless we're within 1/8 of the screensize of the edge of the screen
                bool usePrevOffset = _prevZoneId == currentZoneId;
                if (usePrevOffset) {
                    int prevOffsetXCoord = (int)(scale * (currentRoom.X.Value - minX)) + _prevOffsetX;
                    if (prevOffsetXCoord < screenWidth / 8 || prevOffsetXCoord > 7 * screenWidth / 8) {
                        usePrevOffset = false;
                    }
                }

                if (usePrevOffset) {
                    offsetX = _prevOffsetX;
                } else {
                    offsetX = screenWidth / 2 - (int)(scale * (currentRoom.X.Value - minX));

                    // if this offset will push past the left or right side of the screen, we should jump back to it
                    int maxXCoord = (int)(scale * (maxX - minX)) + offsetX;
                    // 20 px border
                    if (maxXCoord + 20 < screenWidth) {
                        offsetX = screenWidth - (int)(scale * (maxX - minX)) - 20;
                    }
                    if (offsetX > 20) {
                        offsetX = 20;
                    }
                }
            }
            if (scaledZoneHeight < screenHeight) {
                offsetY = (int)((screenHeight - scaledZoneHeight) / 2);
            } else {
                // try to use the previous offset unless we're within 1/8 of the screensize of the edge of the screen
                bool usePrevOffset = _prevZoneId == currentZoneId;
                if (usePrevOffset) {
                    int prevOffsetYCoord = (int)(scale * (currentRoom.Y.Value - minY)) + _prevOffsetY;
                    if (prevOffsetYCoord < screenHeight / 8 || prevOffsetYCoord > 7 * screenHeight / 8) {
                        usePrevOffset = false;
                    }
                }

                if (usePrevOffset) {
                    offsetY = _prevOffsetY;
                } else {
                    offsetY = screenHeight / 2 - (int)(scale * (currentRoom.Y.Value - minY));

                    // if this offset will push past the top or bottom side of the screen, we should jump back to it
                    int maxYCoord = (int)(scale * (maxY - minY)) + offsetY;
                    // 20 px border
                    if (maxYCoord + 20 < screenHeight) {
                        offsetY = screenHeight - (int)(scale * (maxY - minY)) - 20;
                    }
                    if (offsetY > 20) {
                        offsetY = 20;
                    }
                }
            }
            _prevZoneId = currentZoneId;
            _prevOffsetX = offsetX;
            _prevOffsetY = offsetY;


            /*
             * ExitIDTo - the exitTable entry that pairs with this one (-1 if no pair)
            KindId - 0 normal / 1 door / 2 locked door
            DirType - the direction that it's in
            DirToType - I assume this always mirros the ExitIDTo DirType?
            Param - the door name

            Name, Label - at a glance always empty
            */
            HashSet<int> drawn = new HashSet<int>();
            foreach (var exitId in exitsInZone) {
                var exit = _exitsById[exitId];
                if (drawn.Contains(exit.ExitID.Value)) {
                    continue;
                }

                bool hasFromRoom = _roomsById.TryGetValue(exit.FromID.Value, out var fromRoom);
                bool hasToRoom = _roomsById.TryGetValue(exit.ToID.Value, out var toRoom);
                hasToRoom = hasToRoom && toRoom.ZoneID.Value == currentZoneId;
                hasFromRoom = hasFromRoom && fromRoom.ZoneID.Value == currentZoneId;

                // todo: handle rooms that go out of zone
                if (hasFromRoom) {
                    int x1 = (int)(scale * (fromRoom.X.Value - minX)) + offsetX;
                    int y1 = (int)(scale * (fromRoom.Y.Value - minY)) + offsetY;
                    int x2 = -1;
                    int y2 = -1;
                    if (hasToRoom) {
                        x2 = (int)(scale * (toRoom.X.Value - minX)) + offsetX;
                        y2 = (int)(scale * (toRoom.Y.Value - minY)) + offsetY;
                    }

                    if (exit.DirType.Value == (int)DirectionTypes.Up || exit.DirType.Value == (int)DirectionTypes.Down) {
                        // draw up/down exits

                        if (exit.DirType.Value == (int)DirectionTypes.Up) {
                            // draw upwards facing triangle
                            e.Graphics.DrawPolygon(Pens.Black, new[] { new Point(x1 - 4, y1 ), new Point(x1 - 2, y1 + 2), new Point(x1 - 6, y1 + 2) });
                        }
                        if (exit.DirType.Value == (int)DirectionTypes.Down) {
                            // draw downwards facing triangle
                            e.Graphics.DrawPolygon(Pens.Black, new[] { new Point(x1 - 4, y1 + roomSize), new Point(x1 - 2, y1 - 2 + roomSize), new Point(x1 - 6, y1 - 2 + roomSize) });
                        }
                        // e.Graphics.DrawRectangle(penColor, 1, RoomSize/2, RoomSize/2);

                    } else {
                        // display lines straight out from rooms in the door direction initially, and then join them together
                        int deltaX1 = 0;
                        int deltaY1 = 0;
                        if (exit.DirType.Value == (int)DirectionTypes.North)
                            deltaY1 = -4 - roomSize/2;
                        if (exit.DirType.Value == (int)DirectionTypes.South)
                            deltaY1 = 4 + roomSize / 2;
                        if (exit.DirType.Value == (int)DirectionTypes.West)
                            deltaX1 = -4 - roomSize / 2;
                        if (exit.DirType.Value == (int)DirectionTypes.East)
                            deltaX1 = 4 + roomSize / 2;

                        int deltaX2 = 0;
                        int deltaY2 = 0;

                        if (exit.DirType.Value == (int)DirectionTypes.North)
                            deltaY2 = 4 + roomSize / 2;
                        if (exit.DirType.Value == (int)DirectionTypes.South)
                            deltaY2 = -4 - roomSize / 2;
                        if (exit.DirType.Value == (int)DirectionTypes.West)
                            deltaX2 = 4 + roomSize / 2;
                        if (exit.DirType.Value == (int)DirectionTypes.East)
                            deltaX2 = -4 - roomSize / 2;

                        if (hasToRoom) {
                            e.Graphics.DrawLines(Pens.Black, new[] {
                                new Point(x1 + roomSize/2, y1 + roomSize/2),
                                new Point(x1 + roomSize/2 + deltaX1, y1 + roomSize/2 + deltaY1),
                                new Point(x2 + roomSize/2 + deltaX2, y2 + roomSize/2 + deltaY2),
                                new Point(x2 + roomSize/2, y2 + roomSize/2)
                            });
                        } else {
                            // to room doesn't exist or is in different zone
                            var pen = new Pen(Brushes.Brown, width: 4);
                            e.Graphics.DrawLines(pen, new[] {
                                new Point(x1 + roomSize/2, y1 + roomSize/2),
                                new Point(x1 + roomSize/2 + deltaX1, y1 + roomSize/2 + deltaY1)
                            });
                        }

                        if (exit.ExitIDTo == -1) {
                            // todo: do more than just colour 1 way links
                            var pen = new Pen(Brushes.Green, width: 4);
                            e.Graphics.DrawLines(pen, new[] {
                                new Point(x1 + roomSize/2, y1 + roomSize/2),
                                new Point(x1 + roomSize/2 + deltaX1, y1 + roomSize/2 + deltaY1)
                            });
                        }

                    }

                    drawn.Add(exit.ExitID.Value);
                }
            }


            foreach (var room in roomsInZone) {
                int x = (int)(scale * (room.X.Value - minX)) + offsetX;
                int y = (int)(scale * (room.Y.Value - minY)) + offsetY;

                var rgb = room.Color.Value;
                var color = Color.FromArgb((rgb >> 0) & 0xff, (rgb >> 8) & 0xff, (rgb >> 16) & 0xff);
                Brush fillBrush = new SolidBrush(color);
                // var fillBrush = Brushes.LightGray;
                if (room.ObjID.Value == _currentVirtualRoomId) {
                    e.Graphics.DrawRectangle(Pens.Red, x-2, y-2, roomSize+4, roomSize+4); // draw a red hilight around the current spammed-to room

                    if (_currentVirtualRoomId == _currentRoomId) {
                        fillBrush = Brushes.Purple;
                    } else {
                        fillBrush = Brushes.Green;
                    }
                } else if (room.ObjID.Value == _currentRoomId) {
                    fillBrush = Brushes.IndianRed;
                }
                e.Graphics.FillRectangle(fillBrush, x, y, roomSize, roomSize);
                e.Graphics.DrawRectangle(Pens.Black, x, y, roomSize, roomSize);

                if (!string.IsNullOrEmpty(room.IDName)) {
                    var font = new Font(SystemFonts.DefaultFont.FontFamily, 10, FontStyle.Regular);
                    e.Graphics.DrawString(room.IDName, font, Brushes.Black, x + 4, y - 7);
                }

                // var font = new Font(SystemFonts.DefaultFont.FontFamily, 5, FontStyle.Regular);
                // e.Graphics.DrawString(room.ObjID.Value.ToString(), font, Brushes.Black, (float)x + 4, (float)y - 7);
            }

        }

        public void MoveVirtualRoom(string movement) {
            movement = movement.Trim().ToLower();
            if (movement == "qf") {
                _currentVirtualRoomId = _currentRoomId;
                Invalidate();
            } else {
                // nsewud follow directions
                bool inValidRoom = _roomsById.TryGetValue(_currentVirtualRoomId, out var currentRoom);
                if (!inValidRoom) {
                    return;
                }
                 
                bool roomHasExits = _exitsByFromRoom.TryGetValue(_currentVirtualRoomId, out var currentExits);
                if (!roomHasExits) {
                    return;
                }

                var stringToDirections = new Dictionary<string, DirectionTypes> {
                    { "n", DirectionTypes.North },
                    { "e", DirectionTypes.East },
                    { "s", DirectionTypes.South },
                    { "w", DirectionTypes.West },
                    { "u", DirectionTypes.Up },
                    { "d", DirectionTypes.Down },
                };

                bool directionFound = stringToDirections.TryGetValue(movement, out var direction);
                if (!directionFound) {
                    return;
                }

                var link = currentExits.FirstOrDefault(exit => exit.DirType.Value == (int)direction);
                if (link == null) {
                    return;
                }
                _currentVirtualRoomId = link.ToID.Value;

                Invalidate();
            }
        }

        public void RoomVisited(Room room) {
            if (_dataLoader == null) {
                LoadData();
            }

            // for testing purposes I'm just jumping to the first room with the same room name
            ZmudDbOjectTblRow[] possibleRoomsByName = null;
            _roomsByName.TryGetValue(room.Name, out possibleRoomsByName);

            if (possibleRoomsByName == null) {
                // no rooms found with the same name.  For now just return
                return;
            }
            if (possibleRoomsByName.Length == 1) {
                _currentRoomId = possibleRoomsByName[0].ObjID.Value;
                Invalidate();
                return;
            }

            ZmudDbOjectTblRow[] possibleRooms = null;
            if (_roomsByDescription.TryGetValue(room.Description.Replace("\n", "\r\n"), out ZmudDbOjectTblRow[] roomsWithSameDescription)) {
                possibleRooms = roomsWithSameDescription.Intersect(possibleRoomsByName).ToArray();
            } else {
                possibleRooms = new ZmudDbOjectTblRow[0];
            }

            if (possibleRooms.Any()) {
                // todo: currently takes first if there are multiple
                _currentRoomId = possibleRooms[0].ObjID.Value;
                Invalidate();
                return;
            } else {
                // the zmud mapper only uses the first line of the description so it's more likely to be correct
                if (_roomsByFirstLineOfDescription.TryGetValue(room.Description.Split('\n').FirstOrDefault().Trim(), out ZmudDbOjectTblRow[] roomsWithSameFirstLineDescription)) {
                    possibleRooms = roomsWithSameFirstLineDescription.Intersect(possibleRoomsByName).ToArray();
                    if (possibleRooms.Any()) {
                        // todo: currently takes first if there are multiple
                        _currentRoomId = possibleRooms[0].ObjID.Value;
                        Invalidate();
                    }
                }
            }
        }

        private void OnResizeEnd(object sender, EventArgs e) {
            ((Control)sender).Invalidate();
        }

        private void MapWindow_SizeChanged(object sender, EventArgs e) {
            ((Control)sender).Invalidate();
        }
    }
}
