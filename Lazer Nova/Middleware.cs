using System.Security.Claims;

namespace LazerNova;

public static class Middleware
{
	public static void Register()
	{
		Server.App.Use(Logger);
		Server.App.Use(Authentication);
	}

	private static async Task Logger(HttpContext context, RequestDelegate next)
	{
		var buffer = "\n================================\n";
		buffer += $"{context.Request.Method} {context.Request.Path}\n";
		buffer += $"Query String: {context.Request.QueryString}\n";

		context.Request.EnableBuffering();
		using var reader = new StreamReader(context.Request.Body, leaveOpen: true);

		var body = await reader.ReadToEndAsync();
		context.Request.Body.Position = 0;

		if (!string.IsNullOrEmpty(body))
			buffer += $"Body:\n{body}\n";

		buffer += "================================\n";
		Console.WriteLine(buffer);

		await next(context);
	}

	private static async Task Authentication(HttpContext context, RequestDelegate next)
	{
		string? token = null;
		var authorization = context.Request.Headers.Authorization.ToString();

		if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
			token = authorization["Bearer ".Length..].Trim();

		if (token == null && context.Request.Path.StartsWithSegments("/signalr"))
			token = context.Request.Query["access_token"].ToString();

		if (!string.IsNullOrEmpty(token))
		{
			var users = Server.Database.GetCollection<User>("users");
			var user = users.FindOne(x => x.AccessToken == token);

			if (user != null)
			{
				var claims = new[]
				{
					new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
				};

				var identity = new ClaimsIdentity(claims, "Bearer");
				context.User = new ClaimsPrincipal(identity);
			}
		}

		await next(context);
	}
}