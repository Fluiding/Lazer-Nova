using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using osu.Game.Online.API;

namespace LazerNova.Services;

public static class ExternalService
{
	private static OAuthToken? OAuthToken { get; set; }
	public static string ClientID { get; set; } = "";
	public static string ClientSecret { get; set; } = "";

	public static async void UpdateOAuthToken()
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
			if (File.Exists("OAuthCache.json"))
			{
				var authTokenJson = await File.ReadAllTextAsync("OAuthCache.json");
				OAuthToken = JsonConvert.DeserializeObject<OAuthToken>(authTokenJson);

				Server.Http.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", OAuthToken!.AccessToken);
			}

		var response = await Server.Http.GetAsync(url);
		if (response.StatusCode != HttpStatusCode.Forbidden)
			return response;

		UpdateOAuthToken();

		response = await Server.Http.GetAsync(url);
		response.EnsureSuccessStatusCode();
		return response;
	}

	public static async Task<string> DownloadOsuBeatmap(int beatmapID)
	{
		var url = $"https://osu.direct/api/osu/{beatmapID}/raw";

		using var response = await Server.Http.GetAsync(url);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync();
	}
}