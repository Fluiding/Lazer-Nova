using System.Net.WebSockets;
using System.Text;
using lazer_echo.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Online.Rooms;
using osu.Game.Scoring;
using osu.Game.Users;

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

var User = new APIUser
{
	Id = 1,
	JoinDate = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
	Username = "mrekk",

	PreviousUsernames =
	[
		"peppy",
		"test"
	],

	RankHighest = new APIUser.UserRankHighest
	{
		Rank = 1,
		UpdatedAt = DateTimeOffset.Parse("2025-02-03T00:00:00Z")
	},

	CountryCode = CountryCode.AQ,
	Colour = "99EB47",
	AvatarUrl = $"{Hostname}/images.jpeg",

	IsAdmin = false,
	IsSupporter = false,
	SupportLevel = 0,
	IsGMT = false,
	IsQAT = false,
	IsBNG = false,
	IsBot = false,
	Active = false,
	WasRecentlyOnline = false,

	Interests = "none",
	Occupation = "drummer",
	Title = "champion",
	Location = "moon",

	Twitter = "",
	Discord = "",
	Website = "",

	PostCount = 0,
	CommentsCount = 0,
	FollowerCount = 0,
	MappingFollowerCount = 0,

	FavouriteBeatmapsetCount = 0,
	GraveyardBeatmapsetCount = 0,
	LovedBeatmapsetCount = 0,
	RankedBeatmapsetCount = 0,
	PendingBeatmapsetCount = 0,
	GuestBeatmapsetCount = 0,
	NominatedBeatmapsetCount = 0,

	ScoresBestCount = 0,
	ScoresFirstCount = 0,
	ScoresRecentCount = 0,
	ScoresPinnedCount = 0,

	BeatmapPlayCountsCount = 0,

	PlayStyles =
	[
		APIPlayStyle.Keyboard
	],

	PlayMode = "osu",
	ProfileOrder = [],

	Kudosu = new APIUser.KudosuCount
	{
		Total = 0,
		Available = 0
	},

	Rank = new APIUser.GlobalRank
	{
		Rank = 30,
		RulesetId = 0
	},

	TournamentBanners = [],
	Badges = [],
	Achievements = [],
	MonthlyPlayCounts = [],
	ReplaysWatchedCounts = [],

	RulesetsStatistics = new Dictionary<string, UserStatistics>(),

	Groups = [],
	MatchmakingStatistics = []
};

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

// Notifications WebSocket
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
			error_description = "not mrekk.",
			hint = "not mrekk?",
			message = "not mrekk!"
		});

	return Results.Content(JsonConvert.SerializeObject(
		new OAuthToken
		{
			AccessToken = "test-access-token",
			RefreshToken = "test-refresh-token",
			ExpiresIn = 86400
		}
	));
});

App.MapGet("/api/v2/seasonal-backgrounds", () => Results.Content(JsonConvert.SerializeObject(
	new APISeasonalBackgrounds
	{
		EndDate = DateTimeOffset.Parse("2037-12-01T00:00:00Z"),
		Backgrounds =
		[
			new APISeasonalBackground { Url = $"{Hostname}/test1.jpg" }
		]
	}
), "application/json"));

// User info
App.MapGet("/api/v2/me/", () => Results.Content(JsonConvert.SerializeObject(
	User
), "application/json"));

// Notifications WebSocket handshake
App.MapGet("/api/v2/notifications", () => Results.Content(JsonConvert.SerializeObject(
	new APINotificationsBundle
	{
		HasMore = false,
		Notifications = [],
		Endpoint = $"{Hostname.Replace("https", "wss")}/notifications"
	}
), "application/json"));

App.MapGet("/api/v2/friends", () => Results.Content(JsonConvert.SerializeObject(
	new List<APIRelation>()
), "application/json"));

App.MapGet("/api/v2/blocks", () => Results.Content(JsonConvert.SerializeObject(
	new List<APIRelation>()
), "application/json"));

App.MapGet("/api/v2/chat/updates", () => Results.Content(JsonConvert.SerializeObject(
	new GetUpdatesResponse
	{
		Presence = [],
		Messages = []
	}
), "application/json"));

App.MapGet("/api/v2/chat/channels", () => Results.Content(JsonConvert.SerializeObject(
	new List<Channel>()
), "application/json"));

App.MapPost("/api/v2/chat/ack", () => Results.Content(JsonConvert.SerializeObject(
	new ChatAckResponse()
), "application/json"));

App.MapGet("/api/v2/me/beatmapset-favourites", () => Results.Content(JsonConvert.SerializeObject(
	new GetMyFavouriteBeatmapSetsResponse()
), "application/json"));

App.MapGet("/api/v2/beatmapsets/{beatmapId}", Results.InternalServerError);

App.MapPost("/api/v2/beatmaps/{beatmapId}/solo/scores", () => Results.Content(JsonConvert.SerializeObject(
	new APIScoreToken
	{
		ID = 0
	}
), "application/json"));

App.MapPut("/api/v2/beatmaps/{beatmapId}/solo/scores/{scoreId}", async (ScoreInfo score) =>
{
	// var calculator = new OsuPerformanceCalculator();
	// var attributes = await calculator.CalculateAsync(score, );
	return Results.InternalServerError();
});

App.MapHub<MetadataHub>("/signalr/metadata");
App.MapHub<SpectatorHub>("/signalr/spectator");
App.MapHub<MultiplayerHub>("/signalr/multiplayer");

App.Run();