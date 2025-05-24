using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MudClient {


    public static class LogMiner {
        public class LogLine {
            public DateTime Time { get; set; }
            public double MsSinceStart { get; set; }
            public string MessageType { get; set; } = CsvLogFileWriter.LOG_TYPE_MUD_INPUT;
            public string Text { get; set; }
        }

        public static void MineAttackLogs() {

            var r = new Regex(" (blast|cleave|crush|hack|hit|lance|pierce|pound|scythe|shoot|slash|slice|smite|stab|sting|strike|whip)");

            var filename = "./Log_2021-06-12 12-52-06.csv";

            var logLines = LoadLog(filename);


            var matches = logLines
                .Where(line => line.MessageType == CsvLogFileWriter.LOG_TYPE_MUD_OUTPUT)
                .Select(l => l.Text.Replace("\\r\\n", "\n").Replace("\\r", "").Split('\n')).SelectMany(m => m)
                .Where(line => r.IsMatch(line))
                .Select(line => ControlCharacterEncoder.Encode(line, forCsv: true))
                .GroupBy(l => l)
                .OrderByDescending(g => g.Key)
                .OrderByDescending(group => group.Count())
                .Select(g => $"{g.Count()}, {g.Key}")
                .ToList();


            File.WriteAllLines("./AttackLines.txt", matches);

        }

        public static void MineStatusLogs() {
            var filename = "./Log_2021-06-07 18-50-31.csv";

            var logLines = LoadLog(filename);

            var maybeStatusLines = logLines
                .Where(line => line.MessageType == CsvLogFileWriter.LOG_TYPE_MUD_OUTPUT && line.Text.Contains(" >"))
                .Select(line => ControlCharacterEncoder.Encode(line.Text, forCsv: true))
                .ToList();


            var grouped = maybeStatusLines
                .GroupBy(sl => sl)
                .OrderByDescending(group => group.Count())
                .Select((group) => $"{group.Count()}, {group.Key}");


            // File.WriteAllLines("./minedStatusLines.txt", grouped);


            var splitLinedStatusLines = maybeStatusLines.Select(msl => msl.Replace("\\r\\n", "\n").Split('\n')).SelectMany(m => m).ToList();

            var x = splitLinedStatusLines.Where(line => line.Contains(" >")).ToList();

            var y = x.Where(s => s.StartsWith('o') || s.StartsWith('*')).Distinct().ToList();
            var z = x.Where(s => !s.StartsWith('o') && !s.StartsWith('*')).ToList();

            File.WriteAllLines("./splitMinedStatusLines.txt", y);
        }

        public static List<LogMiner.LogLine> LoadLog(string filename) {
            var lines = File.ReadAllLines(filename);

            return lines.Select((line) => {
                var splitLine = line.Split(',');

                return new LogMiner.LogLine {
                    Time = DateTime.Parse(splitLine[0]),
                    MsSinceStart = double.Parse(splitLine[1]),
                    MessageType = splitLine[2],
                    Text = ControlCharacterEncoder.Decode(splitLine[3]),
                };
            }).ToList();
        }
    }
}
