using MudClient.Extensions;
using MudClient.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public class MiscCommands {
        private readonly BufferBlock<List<FormattedOutput>> _outputBuffer;
        private readonly BufferBlock<string> _sendMessageBuffer;
        private readonly BufferBlock<string> _sendSpecialMessageBuffer;
        private readonly BufferBlock<string> _clientInfoBuffer;

        private string _enemyRace = "human";
        private string _target1 = "";
        private string _target2 = "";

        public MiscCommands(BufferBlock<List<FormattedOutput>> outputBuffer,
            BufferBlock<string> sendMessageBuffer,
            BufferBlock<string> sendSpecialMessageBuffer,
            BufferBlock<string> clientInfoBuffer) {

            _outputBuffer = outputBuffer;
            _sendMessageBuffer = sendMessageBuffer;
            _sendSpecialMessageBuffer = sendSpecialMessageBuffer;
            _clientInfoBuffer = clientInfoBuffer;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken) {
            // Task.Run(() => LoopFormattedOutput(cancellationToken));
            Task.Run(() => LoopSpecialMessage(cancellationToken));
        }

        private enum RoomSeenState {
            NotStarted,
            SeenTitle,
            SeenDescirption,
            SeenExits
        }

        private async Task LoopSpecialMessage(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                string output = await _sendSpecialMessageBuffer.ReceiveAsyncIgnoreCanceled(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }


                // process the command the player entered
                var splitOutput = output.Split(' ');
                string command = splitOutput.First().ToLower();
                string restOfMessage = string.Join(" ", splitOutput.Skip(1));
                // todo: parse the message

                if (string.IsNullOrWhiteSpace(_target1)) {
                    _target1 = _enemyRace;
                }
                if (string.IsNullOrWhiteSpace(_target2)) {
                    _target2 = _enemyRace;
                }

                switch (command) {
                    case "sr":
                        _enemyRace = restOfMessage;
                        await _clientInfoBuffer.SendAsync($"target race: {_enemyRace}");
                        break;
                    case "q":
                        await _sendMessageBuffer.SendAsync($"kill {_enemyRace}");
                        break;
                    case "a":
                        await _sendMessageBuffer.SendAsync($"kill h.{_enemyRace}");
                        break;
                    case "]":
                        await _sendMessageBuffer.SendAsync($"dia h.{_enemyRace}");
                        break;
                    case "og":
                        if (restOfMessage.StartsWith("k ")) {
                            await _sendMessageBuffer.SendAsync($"order ghar kill {string.Join(" ", splitOutput.Skip(2))}");
                        } else {
                            await _sendMessageBuffer.SendAsync($"order ghar {restOfMessage}");
                        }
                        break;
                    case "ogk":
                        await _sendMessageBuffer.SendAsync($"order ghar kill {restOfMessage}");
                        break;
                    case "tp":
                        _target1 = restOfMessage;
                        await _clientInfoBuffer.SendAsync($"target1: {_target1}");
                        break;
                    case "p":
                        await _sendMessageBuffer.SendAsync($"kill {_target1}");
                        break;
                    case "i":
                        if (!Program.EnableStabAliases) {
                            await _sendMessageBuffer.SendAsync($"bash h.{_target1}");
                        } else {
                            await _sendMessageBuffer.SendAsync($"backstab h.{_target1}");
                        }
                        break;
                    case "tm":
                        _target2 = restOfMessage;
                        await _clientInfoBuffer.SendAsync($"target2: {_target2}");
                        break;
                    case "m":
                        await _sendMessageBuffer.SendAsync($"kill {_target2}");
                        break;
                    case "sc":
                        await _sendMessageBuffer.SendAsync($"scan {restOfMessage}");
                        break;
                    case "b":
                        if (!Program.EnableStabAliases) {
                            throw new InvalidOperationException();
                        }
                        if (!string.IsNullOrEmpty(restOfMessage)) {
                            await _sendMessageBuffer.SendAsync($"backstab {restOfMessage}");
                        } else {
                            await _sendMessageBuffer.SendAsync($"backstab h.{_enemyRace}");
                        }
                        break;
                }
            }
        }
    }
}
