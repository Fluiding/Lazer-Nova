using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace LazerNova.Hubs;

public class LoggingHubFilter(ILogger<LoggingHubFilter> logger) : IHubFilter
{
	public async ValueTask<object?> InvokeMethodAsync(
		HubInvocationContext context,
		Func<HubInvocationContext, ValueTask<object?>> next)
	{
		logger.LogInformation(
			"""
			===== SIGNALR INVOCATION =====
			Hub: {Hub}
			Method: {Method}
			Connection: {Connection}
			Arguments: {Arguments}
			=============================
			""",
			context.Hub.GetType().Name,
			context.HubMethodName,
			context.Context.ConnectionId,
			JsonSerializer.Serialize(context.HubMethodArguments)
		);

		try
		{
			var result = await next(context);

			logger.LogInformation(
				"SignalR method {Method} returned: {Result}",
				context.HubMethodName,
				JsonSerializer.Serialize(result)
			);

			return result;
		}
		catch (Exception ex)
		{
			logger.LogError(
				ex,
				"SignalR method {Method} failed",
				context.HubMethodName
			);

			throw;
		}
	}

	public async Task OnConnectedAsync(
		HubLifetimeContext context,
		Func<HubLifetimeContext, Task> next)
	{
		logger.LogInformation(
			"SignalR connected: {Hub} ({Connection})",
			context.Hub.GetType().Name,
			context.Context.ConnectionId
		);

		await next(context);
	}

	public async Task OnDisconnectedAsync(
		HubLifetimeContext context,
		Exception? exception,
		Func<HubLifetimeContext, Exception?, Task> next)
	{
		logger.LogInformation(
			exception,
			"SignalR disconnected: {Hub} ({Connection})",
			context.Hub.GetType().Name,
			context.Context.ConnectionId
		);

		await next(context, exception);
	}
}