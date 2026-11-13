using Microsoft.AspNetCore.Identity;

namespace LazerNova.Services;

public static class AuthService
{
	private static readonly PasswordHasher<object> Hasher = new();

	public static bool IsAuthorized(HttpRequest request)
	{
		var token = GetToken(request);
		var users = Server.Database.GetCollection<DBUser>();
		var existing = users.FindOne(x => x.AccessToken == token);

		return existing == null;
	}

	public static string? GetToken(HttpRequest request)
	{
		var authHeader = request.Headers.Authorization.ToString();
		return !authHeader.StartsWith("Bearer ")
			? null
			: authHeader["Bearer ".Length..];
	}

	public static IResult Register(string username, string email, string password)
	{
		var users = Server.Database.GetCollection<DBUser>();
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
		var users = Server.Database.GetCollection<DBUser>();
		var existing = users.FindOne(x => x.Username == username);

		return existing == null
			|| Hasher.VerifyHashedPassword(new(), existing.PasswordHash, password)
			!= PasswordVerificationResult.Failed
				? Results.BadRequest(new { error = "Invalid username or password" })
				: Results.Ok();
	}
}