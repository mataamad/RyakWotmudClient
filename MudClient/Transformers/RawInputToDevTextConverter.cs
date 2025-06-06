﻿using MudClient.Helpers;

namespace MudClient.Transformers {
    internal class RawInputToDevTextConverter {
        internal RawInputToDevTextConverter() {
            Store.TcpReceive.SubscribeAsync(async (message) => {
                await Store.DevText.SendAsync(ControlCharacterEncoder.Encode(message).ToString());
            });
        }
    }
}
