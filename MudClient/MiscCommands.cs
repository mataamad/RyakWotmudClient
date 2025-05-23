using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MudClient {
    public class MiscCommands {
        private string _enemyRace = "light";
        private string _target1 = "";
        private string _target2 = "";

        public MiscCommands() {
            Store.ComplexAlias.SubscribeAsync(async (message) => {
                await ProcessMiscCommand(message);
            });
        }

        private enum RoomSeenState {
            NotStarted,
            SeenTitle,
            SeenDescirption,
            SeenExits
        }

        private async Task ProcessMiscCommand(string output) {
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
                    await Store.ClientInfo.SendAsync($"target race: {_enemyRace}");
                    break;
                case "q":
                    await Store.TcpSend.SendAsync($"kill {_enemyRace}");
                    break;
                case "a":
                    await Store.TcpSend.SendAsync($"kill h.{_enemyRace}");
                    break;
                case "]":
                    await Store.TcpSend.SendAsync($"dia h.{_enemyRace}");
                    break;
                case "og":
                    if (restOfMessage.StartsWith("k ")) {
                        await Store.TcpSend.SendAsync($"order ghar kill {string.Join(" ", splitOutput.Skip(2))}");
                    } else {
                        await Store.TcpSend.SendAsync($"order ghar {restOfMessage}");
                    }
                    break;
                case "ogk":
                    await Store.TcpSend.SendAsync($"order ghar kill {restOfMessage}");
                    break;
                case "tp":
                    _target1 = restOfMessage;
                    await Store.ClientInfo.SendAsync($"target1: {_target1}");
                    break;
                case "p":
                    await Store.TcpSend.SendAsync($"kill {_target1}");
                    break;
                case "i":
                    if (!Program.EnableStabAliases) {
                        await Store.TcpSend.SendAsync($"bash h.{_target1}");
                    } else {
                        await Store.TcpSend.SendAsync($"backstab h.{_target1}");
                    }
                    break;
                case "tm":
                    _target2 = restOfMessage;
                    await Store.ClientInfo.SendAsync($"target2: {_target2}");
                    break;
                case "m":
                    await Store.TcpSend.SendAsync($"kill {_target2}");
                    break;
                case "sc":
                    await Store.TcpSend.SendAsync($"scan {restOfMessage}");
                    break;
                case "b":
                    if (!Program.EnableStabAliases) {
                        await Store.TcpSend.SendAsync($"bash {restOfMessage}");
                    } else {
                        if (!string.IsNullOrEmpty(restOfMessage)) {
                            await Store.TcpSend.SendAsync($"backstab {restOfMessage}");
                        } else {
                            await Store.TcpSend.SendAsync($"backstab h.{_enemyRace}");
                        }
                    }
                    break;
                case "#setstab":
                    var enable = restOfMessage == "1" || restOfMessage.ToLower() == "true";
                    await Store.ClientInfo.SendAsync($"enable stab aliases: {enable}");
                    Program.EnableStabAliases = enable;
                break;
            }
        }

    }
}
