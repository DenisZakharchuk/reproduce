namespace DataProvider.Models;

public record PagedResult<T>(IReadOnlyList<T> Items, int Offset, int PageSize);
