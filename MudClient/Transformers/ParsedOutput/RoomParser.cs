using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MudClient.Transformers.ParsedOutput {
    /// <summary>
    /// Parses rooms from mud output - should be run after StatusParser
    /// </summary>
    internal static class RoomParser {

        private static readonly Regex _roomNameRegex = new(@"^\\x1B\[36m(((?!(from the dark|answers your prayers)).)*)\\x1B\[0m$", RegexOptions.Compiled);

        internal static List<ParsedOutput> Parse(List<ParsedOutput> parsedWithStatusSeparate) {
            var parsedWithStatusAndRoomSeparate = new List<ParsedOutput>();
            foreach (var output in parsedWithStatusSeparate) {
                if (output.Type != ParsedOutputType.Raw) {
                    parsedWithStatusAndRoomSeparate.Add(output);
                } else {

                    // clone lines array because we modify it to handle dline room name detection
                    // todo: probably don't need to do it at all
                    // var lines = output.Lines.Clone() as string[];
                    var lines = output.Lines;
                    List<string> nonRoomLines = new();
                    int i = 0;
                    while (i < lines.Length) {

                        var (matched, roomLineEnd, room) = TryMatchRoom(lines, i);

                        if (matched) {
                            if (nonRoomLines.Count > 0) {
                                parsedWithStatusAndRoomSeparate.Add(new ParsedOutput {
                                    Type = ParsedOutputType.Raw,
                                    Lines = nonRoomLines.ToArray(),
                                });
                                nonRoomLines.Clear();
                            }

                            parsedWithStatusAndRoomSeparate.Add(room);

                            var noItemsOrCreatures = "\\x1B[32m\\x1B[33m\\x1B[0m";
                            var noItems = "\\x1B[33m\\x1B[0m";
                            var itemsAndCreatures = "\\x1B[0m";
                            var endLine = lines[roomLineEnd];

                            // nonRoomLines may contain a room title due to dlining so have to modify 'lines' & loop through it

                            if (endLine.StartsWith(noItemsOrCreatures)) {
                                if (endLine.Length > noItemsOrCreatures.Length) {
                                    lines[roomLineEnd] = endLine[noItemsOrCreatures.Length..];
                                    i = roomLineEnd - 1;
                                } else {
                                    i = roomLineEnd;
                                }
                            } else if (endLine.StartsWith(noItems)) {
                                if (endLine.Length > noItems.Length) {
                                    lines[roomLineEnd] = endLine[noItems.Length..];
                                    i = roomLineEnd - 1;
                                } else {
                                    i = roomLineEnd;
                                }
                            } else if (endLine.StartsWith(itemsAndCreatures)) {
                                if (endLine.Length > itemsAndCreatures.Length) {
                                    lines[roomLineEnd] = endLine[itemsAndCreatures.Length..];
                                    i = roomLineEnd - 1;
                                } else {
                                    i = roomLineEnd;
                                }
                            }

                        } else {
                            var line = lines[i];
                            nonRoomLines.Add(line);
                        }
                        i++;
                    }


                    if (nonRoomLines.Count > 0) {
                        parsedWithStatusAndRoomSeparate.Add(new ParsedOutput {
                            Type = ParsedOutputType.Raw,
                            Lines = nonRoomLines.ToArray(),
                        });
                        nonRoomLines.Clear();
                    }
                }
            }
            return parsedWithStatusAndRoomSeparate;
        }

        private static (bool match, int endLineNumber, ParsedOutput room) TryMatchRoom(string[] lines, int i) {
            var roomName = "";

            var line = lines[i];

            var match = _roomNameRegex.Match(line);
            if (!match.Success) {
                return (false, i, null);
            }

            var roomStartLine = i;
            roomName = match.Groups[1].Value; // todo which group should it be?
            var description = new List<string>();

            // found a potential room
            // try to loop until we find the exits

            int exitsLine = -1;
            int j = roomStartLine + 1;
            while (exitsLine == -1) {
                if (j >= lines.Length) {
                    break;
                }

                if (lines[j].StartsWith("[ obvious exits: ")) {
                    exitsLine = j;
                    break;
                }

                description.Add(lines[j]);
                j++;
            }

            if (exitsLine == -1) {
                return (false, i, null);
            }

            if (lines.Length <= exitsLine) {
                return (false, i, null);
            }


            // todo autoscan

            // handle track (skip lines until hit items)
            var tracksStartLine = exitsLine + 1;
            var itemsStartLine = -1;
            for (int k = tracksStartLine; k < lines.Length; k++) {
                if (lines[k].StartsWith("\\x1B[32m")) {
                    itemsStartLine = k;
                    break;
                }
            }

            if (itemsStartLine == -1) {
                return (false, i, null);
            }



            line = lines[itemsStartLine];

            if (!line.StartsWith("\\x1B[32m")) {
                return (false, i, null);
            }

            // handle items & creatures

            var creaturesStartLine = -1;
            var endRoomLine = -1;
            int creatureStartIndex = 0;
            int itemStartIndex = 0;
            if (line.StartsWith("\\x1B[32m\\x1B[33m\\x1B[0m")) {
                // no items or creatures
                creaturesStartLine = itemsStartLine;
                endRoomLine = creaturesStartLine;

            } else if (line.StartsWith("\\x1B[32m\\x1B[33m")) {
                // no items, some creatures
                creaturesStartLine = itemsStartLine;
                creatureStartIndex = "\\x1B[32m\\x1B[33m".Length;

            } else {
                // some items, indeterminate creatures
                for (int k = itemsStartLine; k < lines.Length; k++) {
                    if (lines[k].StartsWith("\\x1B[33m")) {
                        creaturesStartLine = k;
                        break;
                    }
                }

                itemStartIndex = "\\x1B[32m".Length;
                creatureStartIndex = "\\x1B[33m".Length;

                if (creaturesStartLine == -1) {
                    return (false, i, null);
                }
            }


            for (int k = creaturesStartLine; k < lines.Length; k++) {
                if (lines[k].EndsWith("\\x1B[0m") || lines[k].StartsWith("\\x1B[0m")) {
                    endRoomLine = k;
                    break;
                }
            }

            if (endRoomLine == -1) {
                return (false, i, null);
            }

            var itemLines = lines[itemsStartLine..creaturesStartLine];
            if (itemLines.Length > 0) {
                itemLines[0] = itemLines[0][itemStartIndex..];
            }

            var creatureLines = lines[creaturesStartLine..endRoomLine];
            if (creatureLines.Length > 0) {
                creatureLines[0] = creatureLines[0][creatureStartIndex..];
            };

            if (endRoomLine == -1) {
                return (false, i, null);
            }

            return (true, endRoomLine, new ParsedOutput {
                Type = ParsedOutputType.Room,
                Lines = lines[i..endRoomLine],
                Title = roomName,
                Description = description.ToArray(),
                Exits = lines[exitsLine],
                Tracks = lines[tracksStartLine..itemsStartLine],
                Items = itemLines,
                Creatures = creatureLines,
            });
        }
    }
}
