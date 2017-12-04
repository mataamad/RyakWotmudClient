using System.Windows.Forms;

namespace MudClient.Core.Common {
    public class HotKey {
		public string CommandText { get; set; }
		public string ConcatenatedName => $"[{this.KeyCombination.ToString()}] [{this.CommandText}]";
		public Keys KeyCombination { get; set; }

		public HotKey() { }
		public HotKey(Keys keyCombination, string commandText) {
			this.KeyCombination = keyCombination;
			this.CommandText = commandText;
		}
	}
}