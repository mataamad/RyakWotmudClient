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

        // TODO: pretty sure this is fucked
        public static void ReplaceCurrentLine(this RichTextBox richTextBox, string message, Color textColor) {
            int charIndex = richTextBox.GetFirstCharIndexOfCurrentLine();
            var selectionLength = Math.Min(message.Length, richTextBox.TextLength - charIndex);


            // warning: potential index out of bounds errors
            // var charIndex = richTextBox.GetFirstCharIndexFromLine(richTextBox.Lines.Length - 2);
            // var selectionLength = Math.Min(message.Length, richTextBox.TextLength - charIndex - richTextBox.Lines.Last().Length);
            // var selectionLength = Math.Min(message.Length, richTextBox.TextLength - charIndex);
            // var selectionLength = message.Length;

            if (selectionLength > 0 || message.Length > 0) {
                // richTextBox.Lines[richTextBox.Lines.Length - 1] = "";
                // richTextBox.SelectedRtf = "";
                richTextBox.SelectionColor = textColor;
                richTextBox.SelectionStart = charIndex;
                richTextBox.SelectionLength = selectionLength;
                richTextBox.SelectedText = message;
            }

            // richTextBox.SelectionColor = Color.Red;
            // richTextBox.SelectedText = "";
            // (charIndex, richTextBox.TextLength - charIndex);
            // richTextBox.Select(charIndex, richTextBox.TextLength - charIndex);

            // richTextBox.SelectedText = "";
            // richTextBox.SelectedText = message;
            // richTextBox.SelectionColor = textColor;


            // might be useful if I decide I want to replace the whole previous line
            // int lineNumber = richTextBox.Lines.Length - 1;
            // int startOfLineCharIndex = richTextBox.GetFirstCharIndexFromLine(lineNumber);

            // richTextBox.SelectedRtf
            // richTextBox.Rtf
            // var text = richTextBox.Rtf;
            // var otherText = richTextBox.Text;
        }

        /*public static void ReplaceCurrentLine(this RichTextBox richTextBox, string message, Color textColor) {
            int charIndex = richTextBox.GetFirstCharIndexOfCurrentLine();

            richTextBox.Select(charIndex, richTextBox.TextLength - charIndex);

            richTextBox.SelectedText = message;
            richTextBox.SelectionColor = textColor;


            // might be useful if I decide I want to replace the whole previous line
            // int lineNumber = richTextBox.Lines.Length - 1;
            // int startOfLineCharIndex = richTextBox.GetFirstCharIndexFromLine(lineNumber);

            // richTextBox.SelectedRtf
            // richTextBox.Rtf
            // var text = richTextBox.Rtf;
            // var otherText = richTextBox.Text;
        }*/
    }
}
