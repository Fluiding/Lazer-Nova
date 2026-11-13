namespace LazerNova;

public class Middleware
{
	public static void Register()
	{
		Server.App.Use(Logger);
	}

	private static async Task Logger(HttpContext context, RequestDelegate next)
	{
		var buffer = "\n================================\n";
		buffer += $"{context.Request.Method} {context.Request.Path}\n";
		buffer += $"Content-Type: {context.Request.ContentType}\n";
		buffer += $"Remote IP: {context.Connection.RemoteIpAddress}\n";

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
}