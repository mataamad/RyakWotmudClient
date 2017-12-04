using System.Drawing;

namespace MudClient.Core.Common {

    public static class Options {
		public static Font Font { get; set; } = new Font(FontFamily.GenericMonospace, 10f, FontStyle.Regular);

		public static char CommandDelimiter { get; set; } = ';';
	}
}
