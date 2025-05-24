using MudClient.Helpers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MudClient.Transformers.ParsedOutput {
    /// <summary>
    /// Parses status from mud output
    /// </summary>
    internal class StatusParser {

        private readonly Regex _statusBarRegex;

        internal StatusParser() {
            var hp = string.Join("|", Statuses.Hp.Keys);
            var sp = string.Join("|", Statuses.SpOrDp.Keys);
            var mv = string.Join("|", Statuses.Mv.Keys);

            // while incap you don't actually get a * or 'o' to start the line but it's such a corner case I think I'll leave it out to have a faster regex match
            // todo: fill in the .* that can be e.g. - a Ko'bal trolloc: Scratched - Gretchen: Critical
            _statusBarRegex = new Regex($@"^(\*|o)( R)?( S)? HP:({hp}) ((SP|DP):({sp}) )?MV:({mv}).* > ", RegexOptions.Compiled);
        }

        internal List<ParsedOutput> Parse(List<string> devTextLines) {
            var parsedWithStatusSeparate = new List<ParsedOutput>();
            var linesWithoutStatus = new List<string>();
            foreach (var line in devTextLines) {
                if (_statusBarRegex.IsMatch(line)) {

                    if (linesWithoutStatus.Count > 0) {
                        var raw = new ParsedOutput {
                            Type = ParsedOutputType.Raw,
                            Lines = [.. linesWithoutStatus],
                        };
                        parsedWithStatusSeparate.Add(raw);
                        linesWithoutStatus.Clear();
                    }

                    var statusEndIndex = line.IndexOf('>');

                    var lineWithoutStatus = line.Substring(statusEndIndex + 2); // + 2 because there's always a space after the '>'
                    var status = line.Substring(0, statusEndIndex + 1);

                    var parsedStatus = new ParsedOutput {
                        Type = ParsedOutputType.Status,
                        Lines = [status],
                    };
                    parsedWithStatusSeparate.Add(parsedStatus);

                    if (lineWithoutStatus.Length > 0) {
                        linesWithoutStatus.Add(lineWithoutStatus);
                    }
                } else {
                    linesWithoutStatus.Add(line);
                }
            }

            if (linesWithoutStatus.Count > 0) {
                var raw = new ParsedOutput {
                    Type = ParsedOutputType.Raw,
                    Lines = [.. linesWithoutStatus],
                };
                parsedWithStatusSeparate.Add(raw);
            }

            return parsedWithStatusSeparate;
        }
    }
}
