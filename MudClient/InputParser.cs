using MudClient.Common;
using MudClient.Transformers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MudClient {
    internal class InputParser {
        private const string COMMAND_ACTION = @"#ACTION";
        private const string COMMAND_ALIAS = @"#ALIAS";
        private const string COMMAND_CONNECT = @"#CONNECT";
        private const string COMMAND_QUICK_CONNECT = @"#QC";

        private static readonly Aliases _aliases = new();

        private readonly ConnectionClientProducer _connectionClientProducer;
        private readonly CancellationToken _cancellationToken;


        internal InputParser(ConnectionClientProducer connectionClientProducer, CancellationToken cancellationToken) {
            _cancellationToken = cancellationToken;
            _connectionClientProducer = connectionClientProducer;
            _aliases.LoadAliases();
        }

        internal async Task HandleInput(string input) {
            var inputLines = (input ?? string.Empty).Split([Options.CommandDelimiter], StringSplitOptions.None);
            bool firstLine = true;
            foreach (var inputLine in inputLines) {
                var inputStringSplit = inputLine.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                if (inputStringSplit.Length == 0) {
                    inputStringSplit = [""];
                }
                var firstWordToUpper = inputStringSplit[0].ToUpper();
                switch (firstWordToUpper) {
                    case COMMAND_CONNECT:
                        ConnectToClient(inputStringSplit);
                        break;
                    case COMMAND_QUICK_CONNECT:
                        QuickConnect(inputStringSplit);
                        break;
                    default:
                        if (firstLine && COMMAND_ALIAS.StartsWith(firstWordToUpper) && firstWordToUpper != "") {
                            await ProcessAliasCommand(input);
                            return;
                        } else if (_aliases.SpecialAliasesDictionary.Contains(inputStringSplit[0].Trim())) {
                            await Store.ComplexAlias.SendAsync(inputLine.Trim());
                        } else if (_aliases.Dictionary.TryGetValue(inputLine.Trim(), out var alias)) {
                            await HandleInput(alias.MapsTo);
                        } else {
                            await Store.TcpSend.SendAsync(inputLine);
                        }
                        break;
                }

                firstLine = false;
            }
        }

        private  async Task ProcessAliasCommand(string input) {
            var inputParts = input.Split(' ');
            if (inputParts.Length == 1) {
                string aliasesDescription = "Aliases:\n" + string.Join("\n", _aliases.Dictionary.Values.OrderBy(al => al.FileIndex).Select(al => $"{al.Alias} => {al.MapsTo}"));
                await Store.ClientInfo.SendAsync(aliasesDescription);
                return;
            }

            var alias = inputParts[1];
            var mapTo = string.Join(" ", inputParts.Skip(2));

            if (string.IsNullOrWhiteSpace(mapTo)) {
                _aliases.SetAlias(alias, null);
                await Store.ClientInfo.SendAsync($"Cleared Alias: {alias}");
            } else {
                _aliases.SetAlias(alias, mapTo);
                await Store.ClientInfo.SendAsync($"Set Alias: {alias} => {mapTo}");
            }

        }


        private void QuickConnect(string[] parameters) {
            async void onConnectionEstablished(MessageEventArgs args) {
                var lines = File.ReadAllLines("./quickconnect.txt");
                if (lines.Length > 1) {
                    await Store.TcpSend.SendAsync(lines[0]);
                    await Store.TcpSend.SendAsync(lines[1]);
                }
                _connectionClientProducer.OnConnectionEstablished -= onConnectionEstablished;
            }

            _connectionClientProducer.OnConnectionEstablished += onConnectionEstablished;

            ConnectToClient(parameters);
        }

        private void ConnectToClient(string[] parameters) {
            string hostAddress = "game.wotmud.org";
            string hostPortString = "2224";
            if (parameters.Length == 3) {
                hostAddress = parameters[1];
                hostPortString = parameters[2];
            }

            if (!int.TryParse(hostPortString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hostPort)) {
                throw new Exception($"The value {hostPortString} is not a valid port.");
            }
            _connectionClientProducer.LoopOnNewThreads(hostAddress, hostPort, _cancellationToken);
        }
    }
}
