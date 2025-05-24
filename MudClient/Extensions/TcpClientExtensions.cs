using System.Net.Sockets;

namespace MudClient.Extensions {
    internal static class TcpClientExtensions {
		internal static bool IsSocketConnected(this TcpClient client) {
			if (!client.Connected) return false;
			if (client.Client.Poll(0, SelectMode.SelectRead)) {
				var buffer = new byte[1];
				return client.Client.Receive(buffer, SocketFlags.Peek) != 0;
			}
			return true;
		}
	}
}
