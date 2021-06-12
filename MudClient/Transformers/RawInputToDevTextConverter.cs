
namespace MudClient {
    public class RawInputToDevTextConverter {
        public RawInputToDevTextConverter() {
            Store.TcpReceive.SubscribeAsync(async (message) => {
                await Store.DevText.SendAsync(ControlCharacterEncoder.Encode(message).ToString());
            });
        }
    }
}
