using System.Net.WebSockets;
using System.Text;

namespace LazerNova.Services;

public static class NotificationService
{
	public static async void HandleWebsocket(HttpContext context)
	{
		if (!context.WebSockets.IsWebSocketRequest)
			return;

		Console.WriteLine("Notification WS connected");

		using var socket = await context.WebSockets.AcceptWebSocketAsync();
		var buffer = new byte[4096];

		while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
		{
			using var message = new MemoryStream();
			WebSocketReceiveResult result;
			do
			{
				result = await socket.ReceiveAsync(
					new ArraySegment<byte>(buffer),
					context.RequestAborted
				);

				if (result.MessageType == WebSocketMessageType.Close)
				{
					Console.WriteLine("Notification WS closed");
					return;
				}

				message.Write(buffer, 0, result.Count);
			} while (!result.EndOfMessage);

			if (result.MessageType == WebSocketMessageType.Text)
			{
				var text = Encoding.UTF8.GetString(message.ToArray());
				Console.WriteLine($"=== New notification text ===\n{text}\n==========");
			}
			else
			{
				Console.WriteLine("Received notification WS binary");
			}
		}
	}
}