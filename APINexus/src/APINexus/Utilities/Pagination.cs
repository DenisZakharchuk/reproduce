namespace APINexus.Utilities;

/// <summary>Zero-based row window derived from a page request.</summary>
public record RowRange(int RowIndex, int RowCount);

/// <summary>Navigation metadata for a paged result. Pages are one-based;
/// <see cref="NextPage"/>/<see cref="PrevPage"/> are null when unavailable.</summary>
public record PageInfo(int CurrentPage, int? NextPage, int? PrevPage, int PageCount);

/// <summary>Helpers for translating between page coordinates and row windows.</summary>
public static class Pagination
{
    /// <summary>Converts a one-based <paramref name="pageIndex"/> and
    /// <paramref name="pageSize"/> into the corresponding zero-based row window.</summary>
    public static RowRange ToRowRange(int pageIndex, int pageSize)
    {
        if (pageIndex < 1) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

        return new RowRange((pageIndex - 1) * pageSize, pageSize);
    }

    /// <summary>Computes page navigation for a row window over a set of
    /// <paramref name="totalCount"/> items.</summary>
    public static PageInfo ToPageInfo(int totalCount, int rowIndex, int rowCount)
    {
        if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount));
        if (rowIndex < 0) throw new ArgumentOutOfRangeException(nameof(rowIndex));
        if (rowCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount));

        var pageCount = (totalCount + rowCount - 1) / rowCount;
        var currentPage = rowIndex / rowCount + 1;
        var nextPage = currentPage < pageCount ? currentPage + 1 : (int?)null;
        var prevPage = currentPage > 1 && currentPage <= pageCount ? currentPage - 1 : (int?)null;

        return new PageInfo(currentPage, nextPage, prevPage, pageCount);
    }
}
