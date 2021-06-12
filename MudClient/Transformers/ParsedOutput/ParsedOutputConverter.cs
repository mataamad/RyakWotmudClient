using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    /// <summary>
    /// Tries to convert mud output into high level machine readable data
    /// </summary>
    public class ParsedOutputConverter {

        public ParsedOutputConverter() {

            Store.TcpReceive.SubscribeAsync(async (message) => {
                var lines = ControlCharacterEncoder.EncodeAndSplit(message);


                var parsedWithStatusSeparate = new StatusParser().Parse(lines);
                var parsedWithStatusAndRoomSeparate = RoomParser.Parse(parsedWithStatusSeparate);

                await Store.ParsedOutput.SendAsync(parsedWithStatusAndRoomSeparate);
            });

        }
    }
}
