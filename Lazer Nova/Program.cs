var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => { options.ListenLocalhost(5176, listen => { listen.UseHttps(); }); });

var app = builder.Build();

app.UseStaticFiles();

app.Use(async (context, next) =>
{
	Console.WriteLine("================================");
	Console.WriteLine($"{context.Request.Method} {context.Request.Path}");
	Console.WriteLine($"Content-Type: {context.Request.ContentType}");
	Console.WriteLine($"Remote IP: {context.Connection.RemoteIpAddress}");

	// Read request body without consuming it
	context.Request.EnableBuffering();

	using var reader = new StreamReader(
		context.Request.Body,
		leaveOpen: true
	);

	var body = await reader.ReadToEndAsync();
	context.Request.Body.Position = 0;

	if (!string.IsNullOrEmpty(body))
		Console.WriteLine($"Body:\n{body}");

	Console.WriteLine("================================\n");

	await next();
});

app.MapGet("/api/v2/seasonal-backgrounds", () => Results.Ok(new
{
	ends_at = "2026-12-01T00:00:00+00:00",
	backgrounds = new[]
	{
		new { url = "http://localhost:5176/test1.jpg" },
		new { url = "http://localhost:5176/test2.jpeg" }
	}
}));

app.Run();