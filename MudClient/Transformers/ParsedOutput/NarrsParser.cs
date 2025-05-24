using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MudClient.Transformers.ParsedOutput {
    internal class NarrsParser {

        // todo: won't get mob or enemy communication but do I even care about that?
        private readonly Regex _narrsTellsAndSays = new(@"^(\\x1B\[3(3|1)m)?(\w*) (says|narrates|tells you|bellows|hisses|chats) '(.*)'(\\x1B\[0m)?$", RegexOptions.Compiled);
        internal NarrsParser() {
        }

        internal bool Matches(string line) {
            return _narrsTellsAndSays.IsMatch(line);
        }
    }
}
