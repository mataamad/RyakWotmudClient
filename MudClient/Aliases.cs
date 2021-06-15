using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MudClient {
    public class Aliases {
        public Dictionary<string, AliasRow> Dictionary { get; private set; } = null;

        public HashSet<string> SpecialAliasesDictionary { get; private set; } = new HashSet<string> {
            "qf", "mv",

            "o", "c", "cll", "opp", "unl", "loc", "pic", // fast open
            // "oi", "cd", // open/close seen door (e.g bump into a door, saves here)
            "on", "oe", "os", "ow", "ou", "od",
            "sn", "se", "ss", "sw", "su", "sd",
            "locn", "loce", "locs", "locw", "locu", "locd",
            "unln", "unle", "unls", "unlw", "unlu", "unld",
            "picn", "pice", "pics", "pic", "picu", "picd",
            // "setdoor" // save a door on the map - requires being able to save the map

            "sr", "q", "a", "]",
            "tp", "p", "i",
            "tm", "m",
            "sc",
            "og", "ogk",

        };

        public class AliasRow {
            public string Alias;
            public string MapsTo;
            public string Comment;
            public int FileIndex;
        };

        private const string _aliasesCsvFilename = "./aliases.csv";
        private int _lastLine = 0;

        public Aliases() {
            if (Program.EnableStabAliases) {
                SpecialAliasesDictionary.Add("b");
            }
        }

        public void LoadAliases() {
            if (Dictionary != null) {
                throw new Exception("Cannot load aliases if some are already loaded");
            }
            Dictionary = new Dictionary<string, AliasRow>();

            string[] lines = File.ReadAllLines(_aliasesCsvFilename);

            int i = 0;
            foreach (var line in lines) {
                if (string.IsNullOrWhiteSpace(line)) {
                    i++;
                    continue;
                }

                var split = line.Split(',');

                AliasRow alias = new AliasRow {
                    Alias = split[0],
                    MapsTo = split[1],
                    Comment = split.Length > 2 ? split[2] : null,
                    FileIndex = i,
                };
                Dictionary.Add(alias.Alias, alias);
                i++;
            }
            _lastLine = i;
        }

        public void SetAlias(string alias, string mapping) {
            if (Dictionary.ContainsKey(alias)) {
                if (string.IsNullOrWhiteSpace(mapping)) {
                    Dictionary.Remove(alias);
                } else {
                    Dictionary[alias].MapsTo = mapping;
                }
                SaveAliasesFile();
            } else {
                if (string.IsNullOrEmpty(mapping)) {
                    return;
                }
                var aliasRow = new AliasRow {
                    Alias = alias,
                    MapsTo = mapping,
                    FileIndex = _lastLine,
                    Comment = null,
                };
                Dictionary.Add(alias, aliasRow);
                _lastLine++;
                AppendAliasFile(aliasRow);
            }

        }

        private void AppendAliasFile(AliasRow al) {
            File.AppendAllText(_aliasesCsvFilename, $"\n{al.Alias},{al.MapsTo},{al.Comment}");
        }

        private void SaveAliasesFile() {
            var lines = Dictionary.Values.OrderBy(al => al.FileIndex)
                .Select(al => $"{al.Alias},{al.MapsTo},{al.Comment}");
            File.WriteAllLines(_aliasesCsvFilename, lines);
        }
    }
}
