using System.Net.Http.Headers;
using Newtonsoft.Json;
using osu.Game.Online.API;

namespace LazerNova.Services;

public static class ExternalService
{
	private static OAuthToken? OAuthToken { get; set; }
	public static string ClientID { get; set; } = "";
	public static string ClientSecret { get; set; } = "";

	public static async Task<string> ProxyRequest(HttpRequest request)
	{
		var path = $"{request.PathBase}{request.Path}{request.QueryString}";
		using var response = await GetAsync($"https://osu.ppy.sh{path}");
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task UpdateOAuthToken()
	{
		using var response = await Server.Http.PostAsync(
			"https://osu.ppy.sh/oauth/token",
			new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["client_id"] = ClientID,
				["client_secret"] = ClientSecret,
				["grant_type"] = "client_credentials",
				["scope"] = "public"
			})
		);

		response.EnsureSuccessStatusCode();
		var bodyJson = await response.Content.ReadAsStringAsync();
		await File.WriteAllTextAsync("OAuthCache.json", bodyJson);

		OAuthToken = JsonConvert.DeserializeObject<OAuthToken>(bodyJson);
		Server.Http.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", OAuthToken!.AccessToken);
	}

	public static async Task<HttpResponseMessage> GetAsync(string url)
	{
		if (OAuthToken == null)
		{
			if (File.Exists("OAuthCache.json"))
			{
				var authTokenJson = await File.ReadAllTextAsync("OAuthCache.json");
				OAuthToken = JsonConvert.DeserializeObject<OAuthToken>(authTokenJson);

				Server.Http.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", OAuthToken!.AccessToken);
			}
			else
				await UpdateOAuthToken();
		}

		var response = await Server.Http.GetAsync(url);
		if (response.IsSuccessStatusCode)
			return response;

		await UpdateOAuthToken();

		response = await Server.Http.GetAsync(url);
		response.EnsureSuccessStatusCode();
		return response;
	}

	public static async Task<int> GetRankFromPP(double pp)
	{
		var url = $"https://osudaily.net/data/getPPRank.php?t=pp&v={pp}&m=0";
		using var response = await Server.Http.GetAsync(url);
		response.EnsureSuccessStatusCode();

		var rankString = await response.Content.ReadAsStringAsync();
		return int.Parse(rankString);
	}

	public static async Task<string> GetBeatmapLeaderboards(HttpRequest request)
	{
		var query = request.QueryString.Value!.Contains("limit=50")
			? request.QueryString.Value!.Replace("limit=50", "limit=100")
			: request.QueryString.Value + "&limit=100";

		var path = $"{request.PathBase}{request.Path}{query}";
		using var response = await GetAsync($"https://osu.ppy.sh{path}");
		return await response.Content.ReadAsStringAsync();
	}
}