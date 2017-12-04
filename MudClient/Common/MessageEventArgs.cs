using System;

namespace MudClient.Core.Common {
    public sealed class MessageEventArgs : EventArgs {

		public MessageEventArgs() : base() { }

		public MessageEventArgs(string message) : this() {
			this.Message = message;
		}

		public string Message { get; set; }
	}
}
