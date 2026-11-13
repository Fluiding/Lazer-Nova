using System.Security.Cryptography;
using LazerNova.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using osu.Game.Online.API;

namespace LazerNova.Services;

public static class AuthService
{
	private static readonly PasswordHasher<object> Hasher = new();

	public static bool IsAuthorized(HttpRequest request)
	{
		var token = GetToken(request);
		var users = Server.Database.GetCollection<DBUser>("users");
		var existing = users.FindOne(x => x.AccessToken == token);

		return existing != null;
	}

	public static string? GetToken(HttpRequest request)
	{
		var authHeader = request.Headers.Authorization.ToString();
		return !authHeader.StartsWith("Bearer ")
			? null
			: authHeader["Bearer ".Length..];
	}

	private static bool VerifyHash(string hash, string password)
	{
		return Hasher.VerifyHashedPassword(new(), hash, password) != PasswordVerificationResult.Failed;
	}

	public static IResult Register(string username, string email, string password)
	{
		var users = Server.Database.GetCollection<DBUser>("users");
		var existing = users.FindOne(x => x.Username == username);

		if (existing != null)
			return Results.BadRequest(new { error = "Username already taken" });

		var newUser = UserService.GetNewUser();
		newUser.Username = username;
		newUser.Email = email;
		newUser.PasswordHash = Hasher.HashPassword(new(), password);

		users.Insert(newUser);
		return Results.Ok();
	}


	public static IResult Login(string username, string password)
	{
		var users = Server.Database.GetCollection<DBUser>("users");
		var user = users.FindOne(x => x.Username == username);

		if (user == null || !VerifyHash(user.PasswordHash, password))
			return Results.BadRequest(new { error = "Invalid username or password" });

		user.AccessToken = RandomNumberGenerator.GetHexString(64, true);
		user.RefreshToken = RandomNumberGenerator.GetHexString(64, true);

		users.Update(user);

		return Results.Content(JsonConvert.SerializeObject(
			new OAuthToken
			{
				AccessToken = user.AccessToken,
				RefreshToken = user.RefreshToken,
				ExpiresIn = 86400
			}
		));
	}
}