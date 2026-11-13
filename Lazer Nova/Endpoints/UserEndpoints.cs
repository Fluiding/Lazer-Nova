using LazerNova.Models;
using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Endpoints;

public static class UserEndpoints
{
	public static void Register()
	{
		Server.App.MapGet("/api/v2/users/{id}", (string id, string? key) => Results.Content(
			JsonConvert.SerializeObject(UserService.SearchUser(id, key)),
			"application/json"
		));

		Server.App.MapGet("/api/v2/users/{id}/osu", (string id, string? key) => Results.Content(
			JsonConvert.SerializeObject(UserService.SearchUser(id, key)),
			"application/json"
		));

		Server.App.MapGet("/api/v2/users/{id}/mania", (string id, string? key) => Results.Content(
			JsonConvert.SerializeObject(UserService.SearchUser(id, key)),
			"application/json"
		));

		Server.App.MapGet("/api/v2/users/{id}/taiko", (string id, string? key) => Results.Content(
			JsonConvert.SerializeObject(UserService.SearchUser(id, key)),
			"application/json"
		));

		Server.App.MapGet("/api/v2/users/{id}/fruits", (string id, string? key) => Results.Content(
			JsonConvert.SerializeObject(UserService.SearchUser(id, key)),
			"application/json"
		));

		Server.App.MapGet("/api/v2/me", (HttpRequest request) =>
		{
			if (!AuthService.IsAuthorized(request))
				return Results.Unauthorized();

			var token = AuthService.GetToken(request);
			var users = Server.Database.GetCollection<User>("users");
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