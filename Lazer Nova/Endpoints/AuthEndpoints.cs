using LazerNova.Services;

namespace LazerNova.Endpoints;

public static class AuthEndpoints
{
	public static void Register()
	{
		Server.App.MapPost("/users", async (HttpRequest request) =>
		{
			var form = await request.ReadFormAsync();
			var username = form["user[username]"];
			var email = form["user[user_mail]"];
			var password = form["user[password"];

			return AuthService.Register(username!, email!, password!);
		});

		Server.App.MapPost("/oauth/token", async (HttpRequest request) =>
		{
			var form = await request.ReadFormAsync();
			var username = form["username"];
			var password = form["password"];

			return AuthService.Login(username!, password!);
		});
	}
}