using LazerNova.Endpoints;
using LazerNova.Hubs;
using LazerNova.Models;
using LazerNova.Services;
using LiteDB;
using Microsoft.AspNetCore.SignalR;
using osu.Game.Online;

namespace LazerNova;

public static class Server
{
	public const string Hostname = "https://localhost:5176";
	public static LiteDatabase Database { get; private set; } = null!;
	public static WebApplication App { get; private set; } = null!;
	public static HttpClient Http = new();

	public static async Task Main(string[] args)
	{
		BsonMapper.Global.IncludeFields = true;
		BsonMapper.Global.Entity<User>().Id(x => x.Id);
		BsonMapper.Global.Entity<CachedBeatmapSet>().Id(x => x.Id);

		Database = new LiteDatabase("server.db");

		var builder = WebApplication.CreateBuilder(args);

		ExternalService.ClientID = builder.Configuration["Osu:ClientID"]
			?? throw new Exception(
				"Client ID missing\ndotnet user-secrets init\ndotnet user-secrets set \"Osu:ClientID\" \"YOUR-CLIENT-ID\"");

		ExternalService.ClientSecret = builder.Configuration["Osu:ClientSecret"]
			?? throw new Exception(
				"Client Secret missing\ndotnet user-secrets init\ndotnet user-secrets set \"Osu:ClientSecret\" \"YOUR-CLIENT-SECRET\"");

		ExternalService.UpdateOAuthToken();

		builder.WebHost.ConfigureKestrel(options =>
		{
			options.ListenLocalhost(5176, listen => { listen.UseHttps(); });
		});

		builder.Services.AddSingleton<LoggingHubFilter>();
		builder.Services.AddSignalR(options => { options.AddFilter<LoggingHubFilter>(); })
			.AddMessagePackProtocol(options =>
			{
				options.SerializerOptions = SignalRUnionWorkaroundResolver.OPTIONS;
			});

		builder.Logging.SetMinimumLevel(LogLevel.Trace);
		builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Trace);
		builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Trace);
		builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher", LogLevel.Trace);

		App = builder.Build();
		App.UseStaticFiles();
		App.UseWebSockets();

		App.MapHub<MetadataHub>("/signalr/metadata");
		App.MapHub<SpectatorHub>("/signalr/spectator");
		App.MapHub<MultiplayerHub>("/signalr/multiplayer");

		Middleware.Register();
		AuthEndpoints.Register();
		UserEndpoints.Register();
		BeatmapEndpoints.Register();
		NotificationEndpoints.Register();
		ChatEndpoints.Register();
		MiscEndpoints.Register();

		await App.RunAsync();
	}
}