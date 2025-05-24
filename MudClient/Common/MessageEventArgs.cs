using System;

namespace MudClient.Common {
    internal sealed class MessageEventArgs : EventArgs {

		internal MessageEventArgs() : base() { }

		internal MessageEventArgs(string message) : this() {
            Message = message;
		}

		internal string Message { get; set; }
	}
}
