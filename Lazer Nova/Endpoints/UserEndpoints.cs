using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Endpoints;

public static class UserEndpoints
{
	public static void Register()
	{
		Server.App.MapGet("/api/v2/users/{id}", UserService.SearchUser);
		Server.App.MapGet("/api/v2/users/{id}/osu", UserService.SearchUser);
		Server.App.MapGet("/api/v2/users/{id}/mania", UserService.SearchUser);
		Server.App.MapGet("/api/v2/users/{id}/taiko", UserService.SearchUser);
		Server.App.MapGet("/api/v2/users/{id}/fruits", UserService.SearchUser);

		Server.App.MapGet("/api/v2/me", (HttpRequest request) =>
		{
			if (!AuthService.IsAuthorized(request))
				return Results.Unauthorized();

			var token = AuthService.GetToken(request);
			var users = Server.Database.GetCollection<DBUser>();
			var existing = users.FindOne(x => x.AccessToken == token);

			return Results.Content(
				JsonConvert.SerializeObject(existing),
				"application/json"
			);
		});

		Server.App.MapGet("/api/v2/friends", (HttpRequest request) =>
		{
			var user = UserService.GetCurrentUser(request);
			if (user == null)
				return Results.Unauthorized();

			var friends = user.Relations
				.Where(r => r.RelationType == RelationType.Friend)
				.ToList();

			return Results.Content(
				JsonConvert.SerializeObject(friends),
				"application/json"
			);
		});

		Server.App.MapGet("/api/v2/blocks", (HttpRequest request) =>
		{
			var user = UserService.GetCurrentUser(request);
			if (user == null)
				return Results.Unauthorized();

			var friends = user.Relations
				.Where(r => r.RelationType == RelationType.Block)
				.ToList();

			return Results.Content(
				JsonConvert.SerializeObject(friends),
				"application/json"
			);
		});

		Server.App.MapGet("/api/v2/me/beatmapset-favourites", (HttpRequest request) =>
		{
			var user = UserService.GetCurrentUser(request);
			if (user == null)
				return Results.Unauthorized();

			return Results.Content(
				JsonConvert.SerializeObject(new GetMyFavouriteBeatmapSetsResponse()),
				"application/json"
			);
		});
	}
}