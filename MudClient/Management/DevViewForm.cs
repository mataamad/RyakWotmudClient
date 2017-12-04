using MudClient.Common.Extensions;
using MudClient.Core.Common;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MudClient.Management
{
    public partial class DevViewForm : Form {
		
		public DevViewForm() {
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
            richTextBox.BackColor = Options.BackgroundColor;
            richTextBox.ForeColor = Options.ForegroundColor;
            richTextBox.Font = Options.Font;
        }

		public void WriteToOutput(string message, Color textColor) {
            Action<string> AppendText = (messageToWrite) => {
                richTextBox.AppendFormattedText(message, textColor);
            };

			if (richTextBox.InvokeRequired) {
				richTextBox.Invoke(AppendText, message);
			} else {
				AppendText(message);
			}
        }
    }
}