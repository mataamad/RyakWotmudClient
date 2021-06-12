using MudClient.Management;
using System.Media;

namespace MudClient {
    public class NarrsWriter {
        private readonly MudClientForm _form;

        public NarrsWriter(MudClientForm form) {
            _form = form;

            Store.ParsedOutput.Subscribe((outputs) => {
                foreach (var output in outputs) {
                    if (output.Type != ParsedOutputType.Raw) {
                        continue;
                    }
                    foreach (var line in output.Lines) {
                        // todo: also include my narrates in here
                        // todo: should match on colour and also with a regex instead of doing this
                        if (line.Contains(" narrates '")
                            || line.Contains(" tells you '")
                            || line.Contains(" says '")
                            || line.Contains(" speaks from the ")
                            || line.Contains(" bellows '")
                            || line.Contains(" hisses '")
                            || line.Contains(" chats '")
                            || line == "You are hungry."
                            || line == "You are thirsty.") {

                            if (line == "You are hungry." || line == "You are thirsty.") {
                                SystemSounds.Beep.Play();
                            }


                            // todo: decoding & then converting to rich text will be needlessly wasteful
                            _form.WriteToNarrs(FormatEncodedText.Format(ControlCharacterEncoder.Decode(line + "\n")));
                        }
                    }
                }
            });
        }
    }
}
