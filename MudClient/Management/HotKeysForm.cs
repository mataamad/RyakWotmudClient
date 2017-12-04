using MudClient.Core;
using MudClient.Core.Common;
using System;
using System.Linq;
using System.Windows.Forms;

namespace MudClient.Management {
	public partial class HotKeysForm : Form {

		private readonly HotKeyCollection _hotKeys;
		public HotKeysForm(HotKeyCollection hotKeys) {
			InitializeComponent();
			_hotKeys = hotKeys;
		}
		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			BindHotKeys();
		}

		private void BindHotKeys() {
            hotKeys.DataSource = null;
            hotKeys.DataSource = _hotKeys.ToList();
            hotKeys.DisplayMember = @"ConcatenatedName";
		}

		private void addHotkey_Click(object sender, EventArgs e) {
            if (string.IsNullOrWhiteSpace(this.keyCombination.Text)) {
                throw new Exception(@"The key combination cannot be blank.");
            }

            if (string.IsNullOrWhiteSpace(this.commandText.Text)) {
                throw new Exception(@"The command text cannot be blank.");
            }


            var keys = _hotKeys.ResolveKeys(this.keyCombination.Text);
            if (keys == Keys.Enter) {
                throw new Exception(@"Enter cannot be used as a hotkey.");
            }

            if (keys == Keys.None) {
                throw new Exception(@"None is not a valid key!");
            }

            if (_hotKeys[keys] != null) {
                throw new Exception($"The hotkey {keys} already exists.");
            }

            _hotKeys.Add(keys, this.commandText.Text);
            BindHotKeys();
		}

		private void cancel_Click(object sender, EventArgs e) {
			DialogResult = DialogResult.Cancel;
		}

		private void save_Click(object sender, EventArgs e) {
			DialogResult = DialogResult.OK;
		}
	}
}
