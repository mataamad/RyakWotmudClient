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
    internal partial class MudClientForm : Form {


        private const string COMMAND_QUIT = @"#QUIT";
        private readonly InputParser _inputParser;


        internal bool IsShown = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal DevViewForm DevViewForm { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal StatusForm StatusForm { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal MapWindow MapWindow { get; private set; }

		internal MudClientForm(InputParser inputParser) {
            _inputParser = inputParser;
			InitializeComponent();
			this.KeyPreview = true;

            DevViewForm = new DevViewForm();
            StatusForm = new StatusForm();
            MapWindow = new MapWindow();
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
                _inputParser.HandleInput(this.textBox.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (this.textBox.Text == COMMAND_QUIT) {
                    Close();
                }

                textBox.SelectAll();
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


        internal void WriteToOutput(string message, Color textColor)
        {
            if (!IsShown) {
                return;
            }

            void AppendText() {
                richTextBox.AppendFormattedText(message, textColor);
                // Debug.Write(message);
            }

            if (richTextBox.InvokeRequired) {
				richTextBox.Invoke(AppendText);
			} else {
				AppendText();
			}
        }

        internal void WriteToOutput(List<FormattedOutput> outputs) {
            if (IsShown) {
                richTextBox.WriteToTextBox(outputs);
            }
        }


        internal void WriteToNarrs(List<FormattedOutput> outputs) {
            if (IsShown) {
                narrsRichTextBox.WriteToTextBox(outputs);
            }
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