using MudClient.Common.Extensions;
using MudClient.Core.Common;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MudClient.Management
{
    public partial class DevViewForm : Form {
		
		private bool _shown = false;
		
		public DevViewForm() {
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

		public void WriteToOutput(string message, Color textColor) {
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