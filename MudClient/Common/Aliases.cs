using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient.Common {
    public class Aliases {
        public Dictionary<string, AliasRow> Dictionary { get; private set; } = null;

        public class AliasRow {
            public string Alias;
            public string MapsTo;
            public string Comment;
            public int FileIndex;
        };

        private const string _aliasesCsvFilename = "./aliases.csv";
        private int _lastLine = 0;

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
