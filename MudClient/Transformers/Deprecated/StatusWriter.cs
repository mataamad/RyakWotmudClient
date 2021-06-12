using MudClient.Management;
using System.Text.RegularExpressions;

namespace MudClient {
    public class StatusWriter {

        public StatusWriter(StatusForm statusForm) {

            Store.StatusLine.Subscribe((line) => {
                WriteStatus(line, statusForm);
            });

            // todo: check this always starts on a new line after status has been stripped (seems to)
            // 'score' hp finder - e.g. You have 390(398) hit and 173(243) movement points.
            var scoreHealthRegex = new Regex(@"^You have (\d*)\((\d*)\) hit(, (\d*)\((\d*)\) (saidin|saidar|dark power))? and (-?\d*)\((\d*)\) movement points.$", RegexOptions.Compiled);
            Store.FormattedTextWithoutStatusLine.Subscribe((outputs) => {
                foreach (var output in outputs) {
                    foreach (var line in output.Text.Split('\n')) {
                        if (scoreHealthRegex.IsMatch(line)) {
                            statusForm.WriteToOutput(line + "\n", MudColors.ForegroundColor);
                        }
                    }
                }
            });
        }

        private void WriteStatus(string statusLine, StatusForm statusForm) {
            statusForm.WriteToOutput(statusLine + "\n", MudColors.ForegroundColor);
        }
    }
}
