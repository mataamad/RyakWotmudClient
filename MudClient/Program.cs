using MudClient.Management;
using System;
using System.Threading;

/*

    todos:
    group this list into easy, medium hard

    ctrl+f through log history; at least ctrl+a ctrl+c tocopy it to notepad and do it there
    ctrl+f for map room (or an alias)
    view room details
    press up to scroll through previous commands

    clean up RoomParser.cs

    switch to ParsedOutput for map

    cant see dlines in formatted output anymore

    highlight windows taskbar when something happens
    tidy up map scripts
    make a view of the world where the console doesn't scroll & battle spam is removed
    map dark mode?
    overspam counter? show in status how far overspammed I am
    add manual map move commands  - e.g. mv s
    colour things progressively as they get better/worse -e.g. mvs or some shit
    make track directions stand out & player leave directions
*/

namespace MudClient {
    public class Program {

        public const bool EnableStabAliases = false;
        // public const bool EnableStabAliases = false;

        public const bool ReadFromLogFile = false;
        // public const bool ReadFromLogFile = false;
        // public const string LogFilename = "./test_only_bash.csv";
        // public const string LogFilename = "./Log_2017-11-27 17-34-35.csv";
        // public const string LogFilename = "one_room.csv";
        // public const string LogFilename = "ASushiAppears.csv";
        public const string LogFilename = "incorrectColorReset.csv";
        // public const string LogFilename = "LdLog.csv";

        [STAThread]
        public static void Main(string[] args) {
            // LogMiner.MineStatusLogs(); return;

            var cts = new CancellationTokenSource();

            var csvLogFileProducer = new CsvLogFileProducer();
            if (ReadFromLogFile) {
                csvLogFileProducer.LoopOnNewThread(LogFilename, cts.Token);
            }

            var connectionClientProducer = new ConnectionClientProducer();
            var rawInputToDevTextConverter = new RawInputToDevTextConverter();
            var parsedOutputConverter = new ParsedOutputConverter();
            var csvWriter = new CsvLogFileWriter();

            using (var form = new MudClientForm(cts.Token, connectionClientProducer)) {
                var parsedOutputWriter = new ParsedOutputWriter(form);
                var statusWriter = new StatusWriter(form.StatusForm);
                var devOutputWriter = new DevOutputWriter(form.DevViewForm);
                var roomFinder = new RoomFinder(form.MapWindow);
                var doorsCommands = new DoorsCommands(form.MapWindow);
                var miscCommands = new MiscCommands();
                var narrsWriter = new NarrsWriter(form);

                form.ShowDialog();
            }
            cts.Cancel();
            csvWriter.CloseFile();
        }
    }
}
