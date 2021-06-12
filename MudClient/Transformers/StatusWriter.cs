using MudClient.Management;
using System.Text.RegularExpressions;

namespace MudClient {
    public class StatusWriter {

        public StatusWriter(StatusForm statusForm) {
            var scoreHealthRegex = new Regex(@"^You have (\d*)\((\d*)\) hit(, (\d*)\((\d*)\) (saidin|saidar|dark power))? and (-?\d*)\((\d*)\) movement points.$", RegexOptions.Compiled);
            Store.ParsedOutput.Subscribe((outputs) => {
                foreach (var output in outputs) {
                    if (output.Type == ParsedOutputType.Raw) {
                        foreach (var line in output.Lines) {
                            if (scoreHealthRegex.IsMatch(line)) {
                                statusForm.WriteToOutput(line + "\n", MudColors.ForegroundColor);
                            }
                        }
                    } else if (output.Type == ParsedOutputType.Status) {
                        statusForm.WriteToOutput(output.Lines[0] + "\n", MudColors.ForegroundColor);
                    }
                }
            });
        }
    }
}
