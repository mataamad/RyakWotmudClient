using System;

namespace MudClient.Core.Common {
    internal sealed class MessageEventArgs : EventArgs {

		internal MessageEventArgs() : base() { }

		internal MessageEventArgs(string message) : this() {
			this.Message = message;
		}

		internal string Message { get; set; }
	}
}
