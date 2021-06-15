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
    public static class MapRenderer {

        private const int DefaultRoomSize = 6;
        private const double MaxScaling = 0.08;
        private const double MinScaling = 0.1;


        private static int _prevOffsetX = 0;
        private static int _prevOffsetY = 0;
        private static int _prevZoneId = -1;

        private static SolidBrush _backgroundBrush = new SolidBrush(Color.FromArgb(60,60,60));

        public static void Render(PaintEventArgs e) {
            if (!MapData.DataLoaded) {
                return;
            }

            Graphics g = e.Graphics;

            bool currentRoomExists = MapData.RoomsById.ContainsKey(MapData.CurrentRoomId);
            bool currentVirtualRoomExists = MapData.RoomsById.ContainsKey(MapData.CurrentVirtualRoomId);

            ZmudDbOjectTblRow currentRoom = null;
            if (currentVirtualRoomExists) {
                currentRoom = MapData.RoomsById[MapData.CurrentVirtualRoomId];
            } else if (currentRoomExists) {
                MapData.CurrentVirtualRoomId = MapData.CurrentRoomId;
                currentRoom = MapData.RoomsById[MapData.CurrentVirtualRoomId];
            } else {
                return;
            }

            var currentZoneId = currentRoom.ZoneID.Value;

            var roomsInZone = MapData.RoomsByZone[currentZoneId];

            // todo: could cache this
            HashSet<int> exitsInZone = new HashSet<int>();
            foreach (var room in roomsInZone) {
                if (MapData.ExitsByFromRoom.ContainsKey(room.ObjID.Value)) {
                    foreach (var exit in MapData.ExitsByFromRoom[room.ObjID.Value]) {
                        exitsInZone.Add(exit.ExitID.Value);
                    }
                }
                if (MapData.ExitsByToRoom.ContainsKey(room.ObjID.Value)) {
                    foreach (var exit in MapData.ExitsByToRoom[room.ObjID.Value]) {
                        exitsInZone.Add(exit.ExitID.Value);
                    }
                }
            }

            var minX = roomsInZone.Min(o => o.X.Value);
            var minY = roomsInZone.Min(o => o.Y.Value);
            var maxX = roomsInZone.Max(o => o.X.Value);
            var maxY = roomsInZone.Max(o => o.Y.Value);

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

            g.FillRectangle(_backgroundBrush, 0, 0, screenWidth, screenHeight);


            HashSet<int> drawn = new HashSet<int>();
            foreach (var exitId in exitsInZone) {
                var exit = MapData.ExitsById[exitId];
                // todo: I'm pretty sure lines are still drawn twice
                if (drawn.Contains(exit.ExitID.Value)) {
                    continue;
                }

                bool hasFromRoom = MapData.RoomsById.TryGetValue(exit.FromID.Value, out var fromRoom);
                bool hasToRoom = MapData.RoomsById.TryGetValue(exit.ToID.Value, out var toRoom);
                hasToRoom = hasToRoom && toRoom.ZoneID.Value == currentZoneId;
                hasFromRoom = hasFromRoom && fromRoom.ZoneID.Value == currentZoneId;

                if (hasFromRoom) {
                    int x1 = (int)(scale * (fromRoom.X.Value - minX)) + offsetX;
                    int y1 = (int)(scale * (fromRoom.Y.Value - minY)) + offsetY;
                    int x2 = -1;
                    int y2 = -1;
                    if (hasToRoom) {
                        x2 = (int)(scale * (toRoom.X.Value - minX)) + offsetX;
                        y2 = (int)(scale * (toRoom.Y.Value - minY)) + offsetY;
                    }

                    if (exit.DirType.Value == (int)DirectionType.Up || exit.DirType.Value == (int)DirectionType.Down) {
                        // draw up/down exits

                        if (exit.DirType.Value == (int)DirectionType.Up) {
                            // draw upwards facing triangle
                            e.Graphics.FillPolygon(Brushes.Black, new[] { new Point(x1 - 6, y1), new Point(x1 - 2, y1 + 4), new Point(x1 - 10, y1 + 4) });

                            // has a door
                            if (exit.ExitKindID.Value != 0) {
                                e.Graphics.DrawRectangle(Pens.Black, x1 - 9, y1, 6, 6);
                            }
                        }
                        if (exit.DirType.Value == (int)DirectionType.Down) {
                            // draw downwards facing triangle
                            e.Graphics.FillPolygon(Brushes.Black, new[] { new Point(x1 - 5, y1 + roomSize), new Point(x1 - 2, y1 - 3 + roomSize), new Point(x1 - 8, y1 - 3 + roomSize) });

                            // has a door
                            if (exit.ExitKindID.Value != 0) {
                                e.Graphics.DrawRectangle(Pens.Black, x1 - 9, y1 - 5 + roomSize, 6, 6);
                            }
                        }

                    } else {
                        // display lines straight out from rooms in the door direction initially, and then join them together
                        int deltaX1 = 0;
                        int deltaY1 = 0;
                        if (exit.DirType.Value == (int)DirectionType.North)
                            deltaY1 = -4 - roomSize / 2;
                        if (exit.DirType.Value == (int)DirectionType.South)
                            deltaY1 = 4 + roomSize / 2;
                        if (exit.DirType.Value == (int)DirectionType.West)
                            deltaX1 = -4 - roomSize / 2;
                        if (exit.DirType.Value == (int)DirectionType.East)
                            deltaX1 = 4 + roomSize / 2;

                        int deltaX2 = 0;
                        int deltaY2 = 0;

                        if (exit.DirToType.Value == (int)DirectionType.South)
                            deltaY2 = 4 + roomSize / 2;
                        if (exit.DirToType.Value == (int)DirectionType.North)
                            deltaY2 = -4 - roomSize / 2;
                        if (exit.DirToType.Value == (int)DirectionType.East)
                            deltaX2 = 4 + roomSize / 2;
                        if (exit.DirToType.Value == (int)DirectionType.West)
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

                        // has a door
                        if (exit.ExitKindID.Value != 0) {
                            int doorDeltaX = 0;
                            int doorDeltaY = 0;

                            // todo: this code looks highly questionable - why are there no brackets?
                            if (exit.DirType.Value == (int)DirectionType.North)
                                doorDeltaX = -4;
                            doorDeltaY = -4;
                            if (exit.DirType.Value == (int)DirectionType.South)
                                doorDeltaX = -4;
                            if (exit.DirType.Value == (int)DirectionType.West)
                                doorDeltaY = -4;
                            doorDeltaX = -4;
                            if (exit.DirType.Value == (int)DirectionType.East)
                                doorDeltaY = -4;
                            doorDeltaX = -4;

                            e.Graphics.DrawRectangle(Pens.Black, x1 + roomSize / 2 + deltaX1 + doorDeltaX, y1 + roomSize / 2 + deltaY1 + doorDeltaY, 8, 8);
                            // e.Graphics.FillRectangle(Brushes.Black, x1 + roomSize / 2 + deltaX1, y1 + roomSize / 2 + deltaY1, 8, 8);
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
                if (room.ObjID.Value == MapData.CurrentVirtualRoomId) {
                    e.Graphics.DrawRectangle(Pens.Red, x - 2, y - 2, roomSize + 4, roomSize + 4); // draw a red hilight around the current spammed-to room
                    e.Graphics.DrawRectangle(Pens.Red, x - 3, y - 3, roomSize + 6, roomSize + 6); // draw a red hilight around the current spammed-to room

                    fillBrush = Brushes.Red;
                    /*if (mapWindow.CurrentVirtualRoomId == mapWindow.CurrentRoomId) {
                        fillBrush = Brushes.Purple;
                    } else {
                        fillBrush = Brushes.Green;
                    }*/
                }

                e.Graphics.FillRectangle(fillBrush, x, y, roomSize, roomSize);
                e.Graphics.DrawRectangle(Pens.DarkGray, x, y, roomSize, roomSize);

                if (room.ObjID.Value == MapData.CurrentRoomId) {
                    e.Graphics.FillRectangle(Brushes.IndianRed, x + 3, y + 3, roomSize - 5, roomSize - 5);
                }

                /*if (room.ObjID.Value == CurrentSmartRoomId) {
                    e.Graphics.FillRectangle(Brushes.HotPink, x+2, y+2, roomSize-4, roomSize-4);
                }*/

                // draw room label
                if (!string.IsNullOrEmpty(room.IDName)) {
                    var font = new Font(SystemFonts.DefaultFont.FontFamily, 9, FontStyle.Regular);

                    var stringSize = e.Graphics.MeasureString(room.IDName, font);
                    var X = stringSize.Width;
                    var Y = stringSize.Height;

                    switch ((DirectionType)(room.LabelDir ?? (int)DirectionType.Other)) {
                        case DirectionType.Center:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x-1, y - 2);
                            break;
                        case DirectionType.North:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x - stringSize.Width/2,y - stringSize.Height - 8);
                            break;
                        case DirectionType.South:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x - stringSize.Width/2, y + 10);
                            break;
                        case DirectionType.East:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x + 10, y - 2);
                            break;
                        case DirectionType.West:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x - stringSize.Width - 12, y - 2);
                            break;
                        case DirectionType.NE:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x + 8 ,y - stringSize.Height - 8);
                            break;
                        case DirectionType.NW:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x - stringSize.Width,y - stringSize.Height - 8);
                            break;
                        case DirectionType.SE:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x + 8, y + 10);
                            break;
                        case DirectionType.SW:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x - stringSize.Width, y + 10);
                            break;
                        case DirectionType.None:
                            break;
                        default:
                            e.Graphics.DrawString(room.IDName, font, Brushes.Black, x + 4, y - 7);
                            break;
                    }

                }

                // var font = new Font(SystemFonts.DefaultFont.FontFamily, 5, FontStyle.Regular);
                // e.Graphics.DrawString(room.ObjID.Value.ToString(), font, Brushes.Black, (float)x + 4, (float)y - 7);
            }
        }
    }
}
