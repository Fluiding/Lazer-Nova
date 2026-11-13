using System.Net.WebSockets;
using System.Text;

namespace LazerNova.Services;

public static class NotificationService
{
	private static readonly List<WebSocket> Clients = [];

	public static async Task HandleWebsocket(HttpContext context)
	{
		if (!context.WebSockets.IsWebSocketRequest)
			return;

		Console.WriteLine("Notification WS connected");

		using var socket = await context.WebSockets.AcceptWebSocketAsync();

		Clients.Add(socket);

		try
		{
			var buffer = new byte[4096];

			while (socket.State == WebSocketState.Open &&
			       !context.RequestAborted.IsCancellationRequested)
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

						if (socket.State == WebSocketState.CloseReceived)
							await socket.CloseOutputAsync(
								WebSocketCloseStatus.NormalClosure,
								"Connection closed",
								CancellationToken.None
							);

						return;
					}

					message.Write(buffer, 0, result.Count);
				} while (!result.EndOfMessage);

				if (result.MessageType != WebSocketMessageType.Text)
					continue;

				var text = Encoding.UTF8.GetString(message.ToArray());
				Console.WriteLine($"=== New notification text ===\n{text}\n==========");
			}
		}
		catch (WebSocketException)
		{
			Console.WriteLine("Notification WS disconnected unexpectedly");
		}
		catch (OperationCanceledException)
		{
			Console.WriteLine("Notification WS request cancelled");
		}
		finally
		{
			Clients.Remove(socket);

			if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
				try
				{
					await socket.CloseOutputAsync(
						WebSocketCloseStatus.NormalClosure,
						"Connection closed",
						CancellationToken.None
					);
				}
				catch (WebSocketException) { }

			Console.WriteLine("Notification WS disconnected");
		}
	}
}