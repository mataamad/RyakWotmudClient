using MudClient.Helpers;
using MudClient.Management;
using MudClient.Transformers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient.Transformers.ParsedOutput {
    /// <summary>
    /// Renders parsed output to the screen
    /// </summary>
    internal class ParsedOutputWriter {

        internal ParsedOutputWriter(MudClientForm form) {
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

                            /*var lines = p.Lines;
                            for (int i = 0; i < p.Lines.Length; i++) {
                                if (!string.IsNullOrWhiteSpace(p.Lines[i])) {
                                    lines[i] = p.LineMetadata[i].Type.ToString().PadRight(15)[..15] + " " + p.Lines[i];
                                }
                            }*/

                            var formattedOutput = FormatDecodedText.Format(ControlCharacterEncoder.Decode(string.Join("\n", p.Lines) + "\n"));
                            form.WriteToOutput(formattedOutput);

                            if (!form.ContainsFocus) {
                                WindowFlasher.Flash(form);
                            }
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
                                // Todo: does this work correctly? also it's inefficient
                                form.WriteToOutput(FormatDecodedText.Format(ControlCharacterEncoder.Decode("\\x1B[33m" + string.Join("\n", p.Creatures) + "\n")));
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
