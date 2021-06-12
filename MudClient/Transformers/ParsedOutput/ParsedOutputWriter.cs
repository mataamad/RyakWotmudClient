using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    /// <summary>
    /// Renders parsed output to the screen
    /// </summary>
    public class ParsedOutputWriter {

        public ParsedOutputWriter(MudClientForm form) {
            Store.TcpSend.Subscribe((message) => {
                // output = output + "\n"; // seems to be the most like zMud
                form.WriteToOutput(" " + message + " ", MudColors.CommandColor);
            });

            Store.ClientInfo.Subscribe((message) => {
                form.WriteToOutput("\n" + message + "\n", MudColors.ClientInfoColor);
            });


            Store.ParsedOutput.Subscribe((parsedOutputs) => {
                foreach (var p in parsedOutputs) {
                    switch (p.Type) {
                        case ParsedOutputType.Raw:
                            // todo: dont unsplit things, dont decode things ~ waste of time & effort
                            // probably copy paste a custom version of .FormatOutput
                            var formattedOutput = FormatEncodedText.Format(ControlCharacterEncoder.Decode(string.Join("\n", p.Lines) + "\n"));
                            form.WriteToOutput(formattedOutput);

                            break;
                        case ParsedOutputType.Room:
                            // todo: there's a break between rooms even if it was a dline

                            form.WriteToOutput("\n" + p.Title, MudColors.RoomTitle);
                            // form.WriteToOutput(string.Join("\n", p.Description) + "\n", MudColors.ForegroundColor);
                            form.WriteToOutput(p.Exits + "\n", MudColors.RoomExits);
                            if (p.Tracks.Length > 0) {
                                form.WriteToOutput(string.Join("\n", p.Tracks) + "\n", MudColors.Tracks);
                            }
                            if (p.Items.Length > 0) {
                                form.WriteToOutput(string.Join("\n", p.Items) + "\n", MudColors.ItemsOnFloor);
                            }
                            if (p.Creatures.Length > 0) {
                                // Todo: parse ANSI colors in here
                                form.WriteToOutput(string.Join("\n", p.Creatures) + "\n", MudColors.CreaturesInRoom);
                            }

                            break;
                        case ParsedOutputType.Status:
                            form.WriteToOutput(p.Lines[0] + " ", MudColors.ForegroundColor);
                            break;
                        default:
                            throw new Exception("Unhandled parsed output type");
                    }

                }
            });

        }
    }
}
