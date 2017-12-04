using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;

namespace MudClient.Core.Common {

    public sealed class HotKeyCollection : Collection<HotKey> {
		public HotKey Add(Keys keyCombination, string commandText) {
			var hotKey = new HotKey(keyCombination, commandText);
			this.Add(hotKey);
			return hotKey;
		}

		/// Gets the <see cref="HotKey"/> with the specified key combination.
		public HotKey this[Keys keyCombination] {
			get { return this.FirstOrDefault(hotkey => hotkey.KeyCombination == keyCombination); }
		}

		public Keys ResolveKeys(string value) {
			if (string.IsNullOrWhiteSpace(value)) return Keys.None;

			var keysToReturn = Keys.None;
			var valuesToConvert = value.ToUpper().Replace("CTRL", "CONTROL").Replace("ESC", "ESCAPE").Split(new[] { ',', '+' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var valueToConvert in valuesToConvert) {
				Keys tempKey;
				if (!Enum.TryParse(valueToConvert, true, out tempKey)) {
					throw new Exception($"The value {valueToConvert} is not a valid key.");
				}
				keysToReturn = keysToReturn | tempKey;
			}
			return keysToReturn;
		}
	}
}