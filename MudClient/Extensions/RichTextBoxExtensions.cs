using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
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
                richTextBox.SelectionStart = charIndex;
                richTextBox.SelectionLength = selectionLength;
                richTextBox.SelectionColor = textColor;
                richTextBox.SelectedText = message;
            }
        }

        public static void WriteToTextBox(this RichTextBox target, List<FormattedOutput> outputs) {

            if (!outputs.Any()) {
                return;
            }

            Action WriteText = () => {
                foreach (var output in outputs) {
                    // Debug.Write(output.Text);
                    // richTextBox.AppendFormattedText("X" + output.Text, output.TextColor);

                    if (output.Beep) {
                        SystemSounds.Beep.Play();
                        continue;
                    }

                    if (output.ReplaceCurrentLine) {
                        // richTextBox.ClearCurrentLine();
                        target.ReplaceCurrentLine(output.Text, output.TextColor);
                        // richTextBox.AppendFormattedText("X" + output.Text + "Y", output.TextColor);
                    } else {
                        target.AppendFormattedText(output.Text, output.TextColor);
                    }
                }
            };

            if (target.InvokeRequired) {
                target.Invoke(WriteText);
            } else {
                WriteText();
            }
        }

    }
}
