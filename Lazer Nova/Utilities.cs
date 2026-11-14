namespace LazerNova;

public static class Utilities
{
	public static List<T> GetPaginatedList<T>(List<T> list, int? offset, int? limit)
	{
		return [.. list.Skip(offset ?? 0).Take(limit ?? int.MaxValue)];
	}
}