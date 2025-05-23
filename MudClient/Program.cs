using MudClient.Management;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/*

    todos:
    ctrl+f through log history; at least ctrl+a ctrl+c tocopy it to notepad and do it there
        perhaps have a button to open the history in a new window that is then static and scrollable?
    ctrl+f for map room (or an alias)
    press up to scroll through previous commands


    fix escape codes messing up my AttackParser - resets & line clears in particular.
        consider completely removing colors from my regexes
        or alternatively just allow em and have optional resets at the start of each one
        I THINK ITS WORTH HAVING A VERSION OF LINES WITH CONTROL CHARACTERS REMOVED
            or maybe one with just color resets & \x00's removed?

    put line category to the left of the formatted output

    clean up RoomParser.cs

    tidy up map scripts
    make a view of the world where the console doesn't scroll & battle spam is removed
    overspam counter? show in status how far overspammed I am
    colour things progressively as they get better/worse -e.g. mvs or some shit
    add way to tell map to stop auto-searching
    add a help command or similar telling me all my dang aliases etc.
    fix output formatter to not be inefficient
    profile the app when loading a log from file

    had some issues with the map not following shian for a bit - either the map was taking ages load or maybe autoscan was messing it up (or day/night for a human?)

    easy things:
        view room details somehow (either when manual 'look' command, or when type a specific command)
        make track directions stand out & player leave directions
        highlight unridden horses in rooms (?)
        add a command to mark a specific part of a log for review later
        'mv rk', 'mv blight' etc.
        autospam scripts

    lower prio easy things:
        nicer looking map dark mode
        allow moving in the mud with e.g. 2e2wsen4e

    minor issues:
    cant see dlines in formatted output anymore
    support autoscan in room parser
*/

namespace MudClient {
    public class Program {
        public static bool EnableStabAliases = false;

        public const bool ReadFromLogFile = false;
        // public const bool ReadFromLogFile = false;
        // public const string LogFilename = "./test_only_bash.csv";
        // public const string LogFilename = "./Log_2017-11-27 17-34-35.csv";
        // public const string LogFilename = "one_room.csv";
        // public const string LogFilename = "ASushiAppears.csv";
        // public const string LogFilename = "Log_2021-06-12 12-52-06.csv"; // a long one
        // public const string LogFilename = "incorrectColorReset.csv"; // contains a bunch of interesting test cases, not just colors
        public const string LogFilename = "eqinvwhowhere.csv"; // contains some logs for 'eq', 'inv', 'who', and 'where'
        // public const string LogFilename = "LdLog.csv";

        [STAThread]
        public static void Main(string[] args) {
            // LogMiner.MineStatusLogs(); return;
            // LogMiner.MineAttackLogs(); return;


            var cts = new CancellationTokenSource();

            var csvLogFileProducer = new CsvLogFileProducer();
            if (ReadFromLogFile) {
                csvLogFileProducer.LoopOnNewThread(LogFilename, cts.Token);
            }

            var connectionClientProducer = new ConnectionClientProducer();
            var rawInputToDevTextConverter = new RawInputToDevTextConverter();
            var parsedOutputConverter = new ParsedOutputConverter();
            CsvLogFileWriter csvWriter = null;
            if (!ReadFromLogFile) {
                csvWriter = new CsvLogFileWriter();
            }

            using (var form = new MudClientForm(cts.Token, connectionClientProducer)) {
                var parsedOutputWriter = new ParsedOutputWriter(form);
                var statusWriter = new StatusWriter(form.StatusForm);
                var devOutputWriter = new DevOutputWriter(form.DevViewForm);
                var roomFinder = new RoomFinder(form.MapWindow);
                var doorsCommands = new DoorsCommands(form.MapWindow);
                var miscCommands = new MiscCommands();
                var narrsWriter = new NarrsWriter(form);

                /*List<string> data = new();
                Store.ParsedOutput.Subscribe(o => {
                    data.AddRange(
                        o.Where(x => x.Type == ParsedOutputType.Raw)
                        .SelectMany(x =>  x.Lines.Zip(x.LineMetadata, (l, m) => new { l, m }))
                        .Where(line => line.m.Type == LineMetadataType.None)
                        .Select(l => l.l)
                        );
                });
                Task.Run(() => {
                    Thread.Sleep(60000);
                    File.WriteAllLines("unparsedLinesGrouped4.txt", data.GroupBy(d => d).OrderByDescending(g => g.Count()).Select(g => $"{g.Count()} {g.Key}"));
                });*/



                form.ShowDialog();
            }
            cts.Cancel();
            csvWriter?.CloseFile();
        }
    }
}
