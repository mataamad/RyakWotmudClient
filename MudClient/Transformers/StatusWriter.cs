using MudClient.Management;
using System.Text.RegularExpressions;

namespace MudClient {
    internal class StatusWriter {

        internal StatusWriter(StatusForm statusForm) {
            var scoreHealthRegex = new Regex(@"^You have (\d*)\((\d*)\) hit(, (\d*)\((\d*)\) (saidin|saidar|dark power))? and (-?\d*)\((\d*)\) movement points.$", RegexOptions.Compiled);

            var enemyColors = new Regex(@"\*\\x1B\[3(6|5|1)m([^ ]*)\\x1B\[0m\*",RegexOptions.Compiled);

            Store.ParsedOutput.Subscribe((outputs) => {
                foreach (var output in outputs) {
                    if (output.Type == ParsedOutputType.Raw) {
                        foreach (var line in output.Lines) {
                            if (scoreHealthRegex.IsMatch(line)) {
                                statusForm.WriteToOutput(line + "\n", MudColors.ForegroundColor);
                            }
                            if (enemyColors.IsMatch(line)) {
                                // todo: this is inefficient
                                statusForm.WriteToOutput(FormatDecodedText.Format(ControlCharacterEncoder.Decode(line + "\n")));
                            }

                        }
                    } else if (output.Type == ParsedOutputType.Status) {
                        statusForm.WriteToOutput(output.Lines[0] + "\n", MudColors.ForegroundColor);
                    } else if (output.Type == ParsedOutputType.Room) {
                        foreach (var line in output.Creatures) {
                            if (enemyColors.IsMatch(line)) {
                                // todo: this is inefficient
                                statusForm.WriteToOutput(FormatDecodedText.Format(ControlCharacterEncoder.Decode(line + "\n")));
                            }
                        }
                    }
                }
            });
        }
    }
}
