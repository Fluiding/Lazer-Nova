using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Endpoints;

public static class UserEndpoints
{
	public static void Register()
	{
		Server.App.MapGet("/api/v2/users/{id}", async (HttpRequest request, string id, string? key) =>
			Results.Content(
				JsonConvert.SerializeObject(await UserService.SearchUser(request, id, key)),
				"application/json"
			));

		Server.App.MapGet("/api/v2/users/{id}/osu", async (HttpRequest request, string id, string? key) =>
			Results.Content(
				JsonConvert.SerializeObject(await UserService.SearchUser(request, id, key)),
				"application/json"
			));

		Server.App.MapGet("/api/v2/users/{id}/mania", async (HttpRequest request, string id, string? key) =>
			Results.Content(
				JsonConvert.SerializeObject(await UserService.SearchUser(request, id, key)),
				"application/json"
			));

		Server.App.MapGet("/api/v2/users/{id}/taiko", async (HttpRequest request, string id, string? key) =>
			Results.Content(
				JsonConvert.SerializeObject(await UserService.SearchUser(request, id, key)),
				"application/json"
			));

		Server.App.MapGet("/api/v2/users/{id}/fruits", async (HttpRequest request, string id, string? key) =>
			Results.Content(
				JsonConvert.SerializeObject(await UserService.SearchUser(request, id, key)),
				"application/json"
			));

		Server.App.MapGet("/api/v2/users/{id}/kudosu",
			async (HttpRequest request, string id, string? key, int? offset, int? limit) =>
			{
				var user = await UserService.SearchUser(request, id, key);
				var kudosuHistory = Utilities.GetPaginatedList(user!.KudosuHistory, offset, limit);

				return Results.Content(
					JsonConvert.SerializeObject(kudosuHistory),
					"application/json"
				);
			});

		Server.App.MapGet("/api/users/{userId:int}/scores/{scoresType}",
			async (HttpRequest request, int userId, string scoresType, int? offset, int? limit)
				=> await UserService.GetUserScores(request, userId, scoresType, offset, limit));

		Server.App.MapGet("/api/users/{userId:int}/beatmapsets/{beatmapType}",
			async (HttpRequest request, int userId, string beatmapType, int? offset, int? limit)
				=> await UserService.GetUserBeatmaps(request, userId, beatmapType, offset, limit));

		Server.App.MapGet("/api/v2/me", (HttpRequest request) =>
		{
			if (!AuthService.IsAuthorized(request))
				return Results.Unauthorized();

			var user = UserService.GetUserFromRequest(request);

			return Results.Content(
				JsonConvert.SerializeObject(user),
				"application/json"
			);
		});

		Server.App.MapGet("/api/v2/friends", (HttpRequest request) =>
		{
			var user = UserService.GetUserFromRequest(request);
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
			var user = UserService.GetUserFromRequest(request);
			if (user == null)
				return Results.Unauthorized();

			var blocks = user.Relations
				.Where(r => r.RelationType == RelationType.Block)
				.ToList();

			return Results.Content(
				JsonConvert.SerializeObject(blocks),
				"application/json"
			);
		});

		Server.App.MapGet("/api/v2/me/beatmapset-favourites", (HttpRequest request) =>
		{
			var user = UserService.GetUserFromRequest(request);
			if (user == null)
				return Results.Unauthorized();

			return Results.Content(
				JsonConvert.SerializeObject(new GetMyFavouriteBeatmapSetsResponse()),
				"application/json"
			);
		});
	}
}