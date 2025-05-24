using MudClient.Common;
using MudClient.Extensions;
using MudClient.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MudClient.Management
{
    internal partial class StatusForm : Form {
		
		private bool _shown = false;
		
		internal StatusForm() {
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
            richTextBox.BackColor = MudColors.BackgroundColor;
            richTextBox.ForeColor = MudColors.ForegroundColor;
            richTextBox.Font = Options.Font;
        }
		
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
			_shown = true;
		}

		internal void WriteToOutput(List<FormattedOutput> outputs) {
			if (_shown) {
				richTextBox.WriteToTextBox(outputs);
			}
		}

		internal void WriteToOutput(string message, Color textColor) {
			if (!_shown) {
				return;
			}

            Action AppendText = () => {
                richTextBox.AppendFormattedText(message, textColor);
            };

			if (richTextBox.InvokeRequired) {
				richTextBox.Invoke(AppendText);
			} else {
				AppendText();
			}
        }
    }
}