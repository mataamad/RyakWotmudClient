using MudClient.Transformers.ParsedOutput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    /// <summary>
    /// Tries to convert mud output into high level machine readable data
    /// </summary>
    internal class ParsedOutputConverter {

        internal ParsedOutputConverter() {
            var simpleParser = new SimpleParser();
            var attackParser = new AttackParser();
            var narrsParser = new NarrsParser();

            Store.TcpReceive.SubscribeAsync(async (message) => {
                var lines = ControlCharacterEncoder.EncodeAndSplit(message);


                var parsedWithStatusSeparate = new StatusParser().Parse(lines);
                var parsedWithStatusAndRoomSeparate = RoomParser.Parse(parsedWithStatusSeparate);

                foreach (var output in parsedWithStatusAndRoomSeparate) {
                    if (output.Type != ParsedOutputType.Raw) {
                        continue;
                    }

                    output.LineMetadata = new LineMetadata[output.Lines.Length];

                    int i = 0;
                    foreach (var line in output.Lines) {

                        // todo: is having a separate lineMetadata array the way to go? it minimizes copies a little bit but it's ugly and also in some ways doesnt
                        output.LineMetadata[i] = new LineMetadata();
                        var result = simpleParser.Parse(line);
                        if (result != LineMetadataType.None) {
                            output.LineMetadata[i].Type = result;
                        } else if (attackParser.Matches(line)) {
                            output.LineMetadata[i].Type = LineMetadataType.Attack;
                        } else if (narrsParser.Matches(line)) {
                            output.LineMetadata[i].Type = LineMetadataType.Communication;
                        }
                        i++;
                    }
                }
                await Store.ParsedOutput.SendAsync(parsedWithStatusAndRoomSeparate);
            });

        }
    }
}
