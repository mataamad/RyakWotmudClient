using MudClient.Common;
using MudClient.Common.Extensions;
using MudClient.Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using static MudClient.RawInputToRichTextConverter;

namespace MudClient.Management {
    public partial class MudClientForm : Form {

		private const string COMMAND_ACTION = @"#ACTION";
		private const string COMMAND_ALIAS = @"#ALIAS";
		private const string COMMAND_CONNECT = @"#CONNECT";
		private const string COMMAND_HOTKEY = @"#HOTKEY";
		private const string COMMAND_QUIT = @"#QUIT";
		private const string COMMAND_QUICK_CONNECT = @"#QC";
		
        private readonly HotKeyCollection _hotKeyCollection = new HotKeyCollection();

        private readonly ConnectionClientProducer _connectionClientProducer;

        private readonly CancellationToken _cancellationToken;
        private readonly BufferBlock<string> _sendMessageBuffer;
        private readonly BufferBlock<string> _clientInfoBuffer;

        private readonly Aliases _aliases = new Aliases();

        public DevViewForm DevViewForm { get; private set; }
        public MapWindow MapWindow { get; private set; }

		public MudClientForm(
            CancellationToken cancellationToken,
            ConnectionClientProducer connectionClientProducer,
            BufferBlock<string> sendMessageBuffer,
            BufferBlock<string> clientInfoBuffer) {
            _connectionClientProducer = connectionClientProducer;
            _cancellationToken = cancellationToken;
            _sendMessageBuffer = sendMessageBuffer;
            _clientInfoBuffer = clientInfoBuffer;
			InitializeComponent();
			this.KeyPreview = true;

            DevViewForm = new DevViewForm();
            MapWindow = new MapWindow();
            _aliases.LoadAliases();
		}

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            DevViewForm.Show(this);

            MapWindow.Show(this);

            this.textBox.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
			base.OnKeyDown(e);
			if (!textBox.Focused) {
				textBox.Focus();
			}

            if (e.Control && e.KeyCode == Keys.A) {
                textBox.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.C) {
                Clipboard.SetText(richTextBox.SelectedText);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            var hotKey = _hotKeyCollection[e.KeyData];
            if (hotKey != null) {
                HandleInput(hotKey.CommandText);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter) {
                HandleInput(this.textBox.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
		}

		private async Task HandleInput(string input) {
			var inputLines = (input ?? string.Empty).Split(new[] { Options.CommandDelimiter }, StringSplitOptions.None);
            bool firstLine = true;
			foreach (var inputLine in inputLines) {
				var inputStringSplit = inputLine.Split(' ');
                var firstWordToUpper = inputStringSplit[0].ToUpper();
                switch (firstWordToUpper) {
					case COMMAND_CONNECT:
						ConnectToClient(inputStringSplit);
						break;
                    case COMMAND_QUICK_CONNECT:
                        QuickConnect(inputStringSplit);
                        break;
					case COMMAND_HOTKEY:
						ProcessHotKeyCommand(inputStringSplit);
						break;
                    case COMMAND_QUIT:
                        Close();
                        break;
					default:
                        if (firstLine && COMMAND_ALIAS.StartsWith(firstWordToUpper) && firstWordToUpper != "") {
                            await ProcessAliasCommand(input);
                            textBox.SelectAll();
                            return;
                        } else if (_aliases.Dictionary.TryGetValue(inputLine, out var alias)) {
                            await HandleInput(alias.MapsTo);
                        } else {
                            await SendMessage(inputLine);
                        }
						break;
				}

                firstLine = false;
			}
            textBox.SelectAll();
		}

		private async Task SendMessage(string inputLine) {
            await _sendMessageBuffer.SendAsync(inputLine);
        }

		private void ProcessHotKeyCommand(string[] inputParts) {
			if (inputParts.Length != 3) {
				throw new Exception($"The {COMMAND_HOTKEY} command requires 3 parts.");
			}

			var keys = _hotKeyCollection.ResolveKeys(inputParts[1]);
			if (keys == Keys.Enter) {
				throw new Exception(@"Enter cannot be used as a hotkey.");
			}

			if (_hotKeyCollection[keys] != null) {
				throw new Exception($"The key combination already exists.");
			}

			if (string.IsNullOrWhiteSpace(inputParts[2])) {
				throw new Exception(@"The command text cannot be blank.");
			}

            _hotKeyCollection.Add(keys, inputParts[2]);
		}

        private async Task ProcessAliasCommand(string input) {
            var inputParts = input.Split(' ');
            if (inputParts.Length == 1) {
                string aliasesDescription = "Aliases:\n" + string.Join("\n", _aliases.Dictionary.Values.OrderBy(al => al.FileIndex).Select(al => $"{al.Alias} => {al.MapsTo}"));
                await _clientInfoBuffer.SendAsync(aliasesDescription);
                return;
            }

            var alias = inputParts[1];
            var mapTo = string.Join(" ", inputParts.Skip(2));

            if (string.IsNullOrWhiteSpace(mapTo)) {
                _aliases.SetAlias(alias, null);
                await _clientInfoBuffer.SendAsync($"Cleared Alias: {alias}");
            } else {
                _aliases.SetAlias(alias, mapTo);
                await _clientInfoBuffer.SendAsync($"Set Alias: {alias} => {mapTo}");
            }

        }

        protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
            this.BindUIFormatting();
            this.textBox.Focus();
		}

		private void BindUIFormatting() {
			this.richTextBox.BackColor = MudColors.BackgroundColor;
			this.richTextBox.ForeColor = MudColors.ForegroundColor;
			this.richTextBox.Font = Options.Font;
		}

        private void QuickConnect(string[] parameters) {
            ConnectionClientProducer.EventHandler onConnectionEstablished = null;
            onConnectionEstablished = async (args) => {
                var lines = File.ReadAllLines("./quickconnect.txt");
                if (lines.Length > 1) {
                    await this.SendMessage(lines[0]);
                    await this.SendMessage(lines[1]);
                }
                _connectionClientProducer.OnConnectionEstablished -= onConnectionEstablished;
            };

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

            if (!int.TryParse(hostPortString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hostPort))
            {
                throw new Exception($"The value {hostPortString} is not a valid port.");
            }
            _connectionClientProducer.LoopOnNewThreads(hostAddress, hostPort, _cancellationToken);
		}

        public void WriteToOutput(string message, Color textColor)
        {
            Action AppendText = () => {
                richTextBox.AppendFormattedText(message, textColor);
                // Debug.Write(message);
            };

			if (richTextBox.InvokeRequired) {
				richTextBox.Invoke(AppendText);
			} else {
				AppendText();
			}
        }

        public void WriteToOutput(List<FormattedOutput> outputs)
        {
            if (!outputs.Any()) {
                return;
            }

            Action AppendText = () => {
                foreach (var output in outputs) {
                    // Debug.Write(output.Text);
                    // richTextBox.AppendFormattedText("X" + output.Text, output.TextColor);

                    if (output.ReplaceCurrentLine) {
                        // richTextBox.ClearCurrentLine();
                        richTextBox.ReplaceCurrentLine(output.Text, output.TextColor);
                        // richTextBox.AppendFormattedText("X" + output.Text + "Y", output.TextColor);
                    } else {
                        richTextBox.AppendFormattedText(output.Text, output.TextColor);
                    }
                }
            };

			if (this.richTextBox.InvokeRequired) {
				this.richTextBox.Invoke(AppendText);
			} else {
				AppendText();
			}
        }

		private void connectionClient_Connected(object sender) {
			this.toolStripStatusLabel.Text = @"Connected.";
		}

		private void connectionClient_Disconnected(object sender) {			
			this.toolStripStatusLabel.Text = @"Disconnected.";
		}

		private void closeToolStripMenuItem_Click(object sender, EventArgs e) {
			this.Close();
		}

		private void hotKeysToolStripMenuItem_Click(object sender, EventArgs e) {
            using (var form = new HotKeysForm(_hotKeyCollection)) {
                form.ShowDialog(this);
            }
		}

        private void hotKeysDevWindowStripMenuItem_Click(object sender, EventArgs e) {
            DevViewForm.Show(this);
        }
    }
}