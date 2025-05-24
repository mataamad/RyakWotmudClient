using System.Drawing;

namespace MudClient.Common {

    internal static class Options {
		internal static Font Font { get; set; } = new Font(FontFamily.GenericMonospace, 9f, FontStyle.Regular);

		internal static char CommandDelimiter { get; set; } = ';';
	}
}
