using MudClient.Helpers;
using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MudClient {
    public class StatusBarStripper {

        private readonly Regex _statusBarRegex;

        public StatusBarStripper() {
            var hp = string.Join("|", Statuses.Hp.Keys);
            var sp = string.Join("|", Statuses.SpOrDp.Keys);
            var mv = string.Join("|", Statuses.Mv.Keys);

            // while incap you don't actually get a * or 'o' to start the line but it's such a corner case I think I'll leave it out to have a faster regex match
            // todo: fill in the .* that can be e.g. - a Ko'bal trolloc: Scratched - Gretchen: Critical
            _statusBarRegex = new Regex($@"^(\*|o)( R)?( S)? HP:({hp}) ((SP|DP):({sp}) )?MV:({mv}).* > ", RegexOptions.Compiled);

            Store.FormattedText.SubscribeAsync(async (outputs) => {
                await StripStatusBars(outputs); 
            });
        }

        private async Task StripStatusBars(List<FormattedOutput> outputs) {
            var formattedOutputsWithoutStatus = new List<FormattedOutput>();

            foreach (var output in outputs) {
                string[] splitIntoLines = output.Text.Split('\n');

                bool anyStatus = false;
                var linesWithoutStatus = new List<string>();
                foreach (var line in splitIntoLines) {
                    if (_statusBarRegex.IsMatch(line)) {
                        anyStatus = true;
                        var statusEndIndex = line.IndexOf('>');

                        var lineWithoutStatus = line.Substring(statusEndIndex + 2); // + 2 because there's always a space after the '>'
                        var status = line.Substring(0, statusEndIndex + 1);

                        await Store.StatusLine.SendAsync(status);

                        if (lineWithoutStatus.Length > 0) {
                            linesWithoutStatus.Add(lineWithoutStatus);
                        }
                    } else {
                        linesWithoutStatus.Add(line);
                    }
                }

                if (anyStatus) {
                    formattedOutputsWithoutStatus.Add(new FormattedOutput {
                        Beep = output.Beep,
                        ReplaceCurrentLine = output.ReplaceCurrentLine,
                        Text = string.Join("\n", linesWithoutStatus),
                        TextColor = output.TextColor,
                    });
                } else {
                    formattedOutputsWithoutStatus.Add(output);
                }
            }

            await Store.FormattedTextWithoutStatusLine.SendAsync(formattedOutputsWithoutStatus);
        }
    }
}
