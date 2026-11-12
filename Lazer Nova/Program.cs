using System.Net.WebSockets;
using System.Text;
using lazer_echo.Hubs;
using Microsoft.AspNetCore.SignalR;

const string Hostname = "https://localhost:5176";
var Builder = WebApplication.CreateBuilder(args);

Builder.WebHost.ConfigureKestrel(options => { options.ListenLocalhost(5176, listen => { listen.UseHttps(); }); });

Builder.Services.AddSingleton<LoggingHubFilter>();
Builder.Services.AddSignalR(options => { options.AddFilter<LoggingHubFilter>(); }).AddMessagePackProtocol();

Builder.Logging.SetMinimumLevel(LogLevel.Trace);
Builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Trace);
Builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Trace);
Builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher", LogLevel.Trace);

var App = Builder.Build();
App.UseStaticFiles();
App.UseWebSockets();

App.Use(async (context, next) =>
{
	var buffer = "";
	buffer += "================================\n";
	buffer += $"{context.Request.Method} {context.Request.Path}\n";
	buffer += $"Content-Type: {context.Request.ContentType}\n";
	buffer += $"Remote IP: {context.Connection.RemoteIpAddress}\n";

	// Read request body without consuming it
	context.Request.EnableBuffering();

	using var reader = new StreamReader(
		context.Request.Body,
		leaveOpen: true
	);

	var body = await reader.ReadToEndAsync();
	context.Request.Body.Position = 0;

	if (!string.IsNullOrEmpty(body))
		buffer += $"Body:\n{body}\n";

	buffer += "================================\n";
	Console.WriteLine(buffer);

	await next();
});

App.MapGet("/api/v2/seasonal-backgrounds", () => Results.Ok(new
{
	ends_at = "2037-12-01T00:00:00Z",
	backgrounds = new[]
	{
		new { url = $"{Hostname}/test1.jpg" }
	}
}));


// Register
App.MapPost("/users", async (HttpRequest request) =>
{
	var form = await request.ReadFormAsync();

	if (form["user[username]"] != "mrekk")
		return Results.BadRequest(new
		{
			error = "invalid_grant",
			error_description = "not mrekk.",
			hint = "not mrekk?",
			message = "not mrekk!"
		});

	return Results.Ok();
});

// Login
App.MapPost("/oauth/token", async (HttpRequest request) =>
{
	var form = await request.ReadFormAsync();
	if (form["username"] != "mrekk")
		return Results.BadRequest(new
		{
			error = "invalid_grant",
			error_description = "not mrekk, preposterous",
			hint = "heheheaw",
			message = "not mrekk!"
		});

	return Results.Ok(new
	{
		token_type = "Bearer",
		expires_in = 86400,
		access_token = "test-access-token",
		refresh_token = "test-refresh-token"
	});
});

var User = new
{
	id = 1,
	join_date = "2025-01-01T00:00:00Z",
	username = "mrekk",
	previous_usernames = (string[])
	[
		"peppy",
		"test"
	],
	rank_highest = new
	{
		rank = 1,
		updated_at = "2025-02-03T00:00:00Z"
	},
	country_code = "UQ",
	profile_colour = "99EB47",
	avatar_url = $"{Hostname}/images.jpeg",

	is_admin = false,
	is_supporter = false,
	support_level = 0,
	is_gmt = false,
	is_qat = false,
	is_bng = false,
	is_bot = false,
	is_active = false,
	is_online = false,
	interests = "none",
	occupation = "drummer",
	title = "champion",
	location = "moon",
	twitter = "",
	discord = "",
	website = "",
	post_count = 0,
	comments_count = 0,
	follower_count = 0,
	mapping_follower_count = 0,
	favourite_beatmapset_count = 0,
	graveyard_beatmapset_count = 0,
	loved_beatmapset_count = 0,
	ranked_beatmapset_count = 0,
	pending_beatmapset_count = 0,
	guest_beatmapset_count = 0,
	nominated_beatmapset_count = 0,
	scores_best_count = 0,
	scores_first_count = 0,
	scores_recent_count = 0,
	scores_pinned_count = 0,
	beatmap_playcounts_count = 0,
	playstyle = (string[])["Keyboard"],
	playmode = "osu",
	profile_order = Array.Empty<string>(),
	kudosu = new
	{
		total = 0,
		available = 0
	},
	global_rank = new
	{
		rank = 30,
		ruleset_id = 0
	},
	rank_history = new
	{
		mode = "osu",
		data = (int[])[50, 30]
	},
	active_tournament_banners = Array.Empty<object>(),
	badges = Array.Empty<object>(),
	user_achievements = Array.Empty<object>(),
	monthly_playcounts = Array.Empty<object>(),
	replays_watched_counts = Array.Empty<object>(),
	statistics_rulesets = new Dictionary<string, object>(),
	groups = Array.Empty<object>(),
	matchmaking_stats = Array.Empty<object>()
};

// User info
App.MapGet("/api/v2/me/", () => Results.Ok(User));

App.Map("/notifications", async context =>
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
});

App.MapGet("/api/v2/notifications", () => Results.Ok(new
{
	has_more = false,
	notifications = Array.Empty<object>(),
	unread_count = 0,
	notification_endpoint = $"{Hostname.Replace("https", "wss")}/notifications"
}));

App.MapGet("/api/v2/friends", () => Results.Ok(
	new List<object>()
));

App.MapGet("/api/v2/blocks", () => Results.Ok(
	new List<object>()
));

App.MapGet("/api/v2/chat/updates", () => Results.Ok(new
{
	Presence = Array.Empty<object>(),
	Messages = Array.Empty<object>()
}));

App.MapGet("/api/v2/chat/channels", () => Results.Ok(
	new List<object>()
));

App.MapPost("/api/v2/chat/ack", () => Results.Ok(new
{
	silences = Array.Empty<object>()
}));

App.MapGet("/api/v2/me/beatmapset-favourites", () => Results.Ok(new
{
	beatmapset_ids = (int[])[]
}));

App.MapGet("/api/v2/beatmapsets/{_}", Results.InternalServerError);

App.MapPost("/api/v2/beatmaps/{_}/solo/scores", () => Results.Ok(new
{
	id = 0
}));

App.MapPut("/api/v2/beatmaps/{_}/solo/scores/{scoreId:int}", (int scoreId) => { });

App.MapHub<MetadataHub>("/signalr/metadata");
App.MapHub<SpectatorHub>("/signalr/spectator");
App.MapHub<MultiplayerHub>("/signalr/multiplayer");

App.Run();