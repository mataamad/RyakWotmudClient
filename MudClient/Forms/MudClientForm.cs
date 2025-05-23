using MudClient.Common;
using MudClient.Common.Extensions;
using MudClient.Core.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MudClient.Management {
    public partial class MudClientForm : Form {

		private const string COMMAND_ACTION = @"#ACTION";
		private const string COMMAND_ALIAS = @"#ALIAS";
		private const string COMMAND_CONNECT = @"#CONNECT";
		private const string COMMAND_QUIT = @"#QUIT";
		private const string COMMAND_QUICK_CONNECT = @"#QC";
		
        private readonly ConnectionClientProducer _connectionClientProducer;

        private readonly CancellationToken _cancellationToken;

        private readonly Aliases _aliases = new Aliases();

        public bool IsShown = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DevViewForm DevViewForm { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public StatusForm StatusForm { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MapWindow MapWindow { get; private set; }

		public MudClientForm(
            CancellationToken cancellationToken,
            ConnectionClientProducer connectionClientProducer) {
            _connectionClientProducer = connectionClientProducer;
            _cancellationToken = cancellationToken;
			InitializeComponent();
			this.KeyPreview = true;

            DevViewForm = new DevViewForm();
            StatusForm = new StatusForm();
            MapWindow = new MapWindow();
            _aliases.LoadAliases();
		}

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            DevViewForm.Show(this);
            StatusForm.Show(this);

            MapWindow.Show(this);

            this.textBox.Focus();

            this.IsShown = true;
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
                if (richTextBox.SelectedText != null) {
                    Clipboard.SetText(richTextBox.SelectedText);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            else if (e.KeyCode == Keys.Enter) {
                HandleInput(this.textBox.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
		}

        // todo: move input handling to a separate file
		private async Task HandleInput(string input) {
			var inputLines = (input ?? string.Empty).Split(new[] { Options.CommandDelimiter }, StringSplitOptions.None);
            bool firstLine = true;
			foreach (var inputLine in inputLines) {
				var inputStringSplit = inputLine.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                if (inputStringSplit.Length == 0) {
                    inputStringSplit = new[] { "" };
                }
                var firstWordToUpper = inputStringSplit[0].ToUpper();
                switch (firstWordToUpper) {
					case COMMAND_CONNECT:
						ConnectToClient(inputStringSplit);
						break;
                    case COMMAND_QUICK_CONNECT:
                        QuickConnect(inputStringSplit);
                        break;
                    case COMMAND_QUIT:
                        Close();
                        break;
					default:
                        if (firstLine && COMMAND_ALIAS.StartsWith(firstWordToUpper) && firstWordToUpper != "") {
                            await ProcessAliasCommand(input);
                            textBox.SelectAll();
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
            textBox.SelectAll();
		}

        private async Task ProcessAliasCommand(string input) {
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

        protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
            this.BindUIFormatting();
            this.textBox.Focus();
		}

		private void BindUIFormatting() {
			this.richTextBox.BackColor = MudColors.BackgroundColor;
			this.richTextBox.ForeColor = MudColors.ForegroundColor;
			this.richTextBox.Font = Options.Font;
			this.narrsRichTextBox.BackColor = MudColors.BackgroundColor;
			this.narrsRichTextBox.ForeColor = MudColors.ForegroundColor;
			this.narrsRichTextBox.Font = Options.Font;
		}

        private void QuickConnect(string[] parameters) {
            ConnectionClientProducer.EventHandler onConnectionEstablished = null;
            onConnectionEstablished = async (args) => {
                var lines = File.ReadAllLines("./quickconnect.txt");
                if (lines.Length > 1) {
                    await Store.TcpSend.SendAsync(lines[0]);
                    await Store.TcpSend.SendAsync(lines[1]);
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
            if (!IsShown) {
                return;
            }

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

        public void WriteToOutput(List<FormattedOutput> outputs) {
            if (IsShown) {
                richTextBox.WriteToTextBox(outputs);
            }
        }


        public void WriteToNarrs(List<FormattedOutput> outputs) {
            if (IsShown) {
                narrsRichTextBox.WriteToTextBox(outputs);
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

        private void hotKeysDevWindowStripMenuItem_Click(object sender, EventArgs e) {
            if (!DevViewForm.Visible) {
                DevViewForm.Show(this);
            }
        }

        private void textBox_Click(object sender, EventArgs e) {
            // this.textBox.SelectAll();
        }
    }
}