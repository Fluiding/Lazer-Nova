using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using osu.Game.Online.Notifications.WebSocket;

namespace LazerNova.Services;

public static class NotificationService
{
	private static ConcurrentDictionary<int, WebSocket> Clients = [];

	private static async Task HandleIncomingMessages(
		WebSocket client,
		HttpContext context)
	{
		while (client.State == WebSocketState.Open &&
		       !context.RequestAborted.IsCancellationRequested)
		{
			var buffer = new byte[4096];
			using var message = new MemoryStream();
			WebSocketReceiveResult result;

			do
			{
				result = await client.ReceiveAsync(
					new ArraySegment<byte>(buffer),
					context.RequestAborted
				);

				if (result.MessageType == WebSocketMessageType.Close)
				{
					if (client.State == WebSocketState.CloseReceived)
						await client.CloseOutputAsync(
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
			Console.WriteLine(text);
		}
	}

	public static async Task ConnectWebsocketAsync(HttpContext context)
	{
		using var client = await context.WebSockets.AcceptWebSocketAsync();
		var user = UserService.GetUserFromRequest(context.Request)!;
		Clients.TryAdd(user.Id, client);

		try
		{
			await HandleIncomingMessages(client, context);
		}
		finally
		{
			Clients.Remove(user.Id, out _);
		}
	}

	public static async Task SendMessageAsync(int userId, SocketMessage data)
	{
		var bytes = Encoding.UTF8.GetBytes(
			JsonConvert.SerializeObject(data));

		await Clients[userId].SendAsync(
			bytes,
			WebSocketMessageType.Text,
			true,
			CancellationToken.None);
	}
}