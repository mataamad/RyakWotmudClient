using MudClient.Management;

namespace MudClient {
    public class OutputWriter {
        private readonly MudClientForm _form;

        public OutputWriter(MudClientForm form) {
            _form = form;

            Store.TcpSend.Subscribe((message) => {
                // output = "\n" + output + "\n";
                // output = "\n" + output;
                // output = output + "\n"; // seems to be the most like zMud
                _form.WriteToOutput(message + "\n", MudColors.CommandColor);
            });

            // Store.FormattedTextWithoutStatusLine.Subscribe((formattedOutput) => {
            Store.FormattedText.Subscribe((formattedOutput) => {

                var strippedOutput = RoomDescriptionStripper.StripRoomDescriptions(formattedOutput);
                _form.WriteToOutput(strippedOutput);

                // _form.WriteToOutput(formattedOutput);
            });

            Store.ClientInfo.Subscribe((message) => {
                _form.WriteToOutput("\n" + message + "\n", MudColors.ClientInfoColor);
            });
        }
    }
}
