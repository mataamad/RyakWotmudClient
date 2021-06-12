using MudClient.Management;
using System.Drawing;

namespace MudClient {
    public class DevOutputWriter {
        private readonly DevViewForm _form;

        public DevOutputWriter(DevViewForm form) {
            _form = form;

            Store.TcpSend.Subscribe((message) => {
                // adding \n seems to be the most like zMud
                // _form.WriteToOutput(message + "\n", MudColors.CommandColor);
            });


            /*Store.DevText.Subscribe((message) => {
                _form.WriteToOutput(message, MudColors.ForegroundColor);
            });*/

            Store.ParsedOutput.Subscribe((parsedOutputs) => {
                foreach (var p in parsedOutputs) {
                    if (p.Type == ParsedOutputType.Raw) {
                        _form.WriteToOutput("RAW:\n", Color.AliceBlue);
                        _form.WriteToOutput(string.Join("\n", p.Lines) + "\n", MudColors.ForegroundColor);
                    } else if (p.Type == ParsedOutputType.Room) {
                        _form.WriteToOutput("ROOM:\n", Color.LightSalmon);
                        _form.WriteToOutput(p.Title + "\n", Color.Blue);
                        _form.WriteToOutput(string.Join("\n", p.Description) + "\n", MudColors.ForegroundColor);
                        _form.WriteToOutput(p.Exits + "\n", Color.Green);
                        _form.WriteToOutput(string.Join("\n", p.Tracks) + "\n", Color.DarkSeaGreen);
                        _form.WriteToOutput(string.Join("\n", p.Items) + "\n", Color.Yellow);
                        _form.WriteToOutput(string.Join("\n", p.Creatures) + "\n", Color.HotPink);

                    } else if (p.Type == ParsedOutputType.Status) {
                        _form.WriteToOutput("STATUS:\n", Color.AliceBlue);
                        _form.WriteToOutput(p.Lines[0] + "\n", MudColors.ForegroundColor);
                    }
                }
            });
        }
    }
}
