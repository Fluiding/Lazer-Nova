using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Models;

public class User : APIUser
{
	public string Email { get; set; } = "";
	public string PasswordHash { get; set; } = "";
	public string AccessToken { get; set; } = "";
	public string RefreshToken { get; set; } = "";

	public List<APIRelation> Relations { get; set; } = [];
}

public class CachedBeatmapSet
{
	public int Id { get; init; }
	public string Json { get; init; } = "";
}