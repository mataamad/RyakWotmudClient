using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MudClient.Common.Extensions {
    public static class RichTextBoxExtensions {
		public static void AppendFormattedText(this RichTextBox richTextBox, string message, Color textColor) {
            /*richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.SelectionLength = 0;
            richTextBox.SelectionColor = textColor;
            richTextBox.AppendText(message);*/

            // apparently this is faster than the previous method
            richTextBox.DeselectAll();
            richTextBox.SelectionColor = textColor;
            richTextBox.AppendText(message);
		}

        public static void ReplaceCurrentLine(this RichTextBox richTextBox, string message, Color textColor) {
            int charIndex = richTextBox.GetFirstCharIndexOfCurrentLine();
            var selectionLength = Math.Min(message.Length, richTextBox.TextLength - charIndex);

            if (selectionLength > 0 || message.Length > 0) {
                richTextBox.SelectionColor = textColor;
                richTextBox.SelectionStart = charIndex;
                richTextBox.SelectionLength = selectionLength;
                richTextBox.SelectedText = message;
            }
        }
    }
}
