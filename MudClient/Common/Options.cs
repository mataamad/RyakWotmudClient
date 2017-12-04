using System.Drawing;

namespace MudClient.Core.Common {

    public static class Options {
		public static Color BackgroundColor { get; set; } = Color.Black;
		public static Color ForegroundColor { get; set; } = Color.White;
		public static Color CommandColor { get; set; } = Color.Gold;
		public static Color ClientInfoColor { get; set; } = Color.CornflowerBlue;

		public static Font Font { get; set; } = new Font(FontFamily.GenericMonospace, 10f, FontStyle.Regular);

		public static char CommandDelimiter { get; set; } = ';';
	}
}
