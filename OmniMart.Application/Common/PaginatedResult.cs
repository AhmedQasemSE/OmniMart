namespace OmniMart.Application.Common;

public class PaginatedResult<T>
{
    public List<T> Data { get; }

    public int CurrentPage { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }

    public bool HasPrevious => CurrentPage > 1 && TotalPages > 0;
    public bool HasNext => CurrentPage < TotalPages;
    public PaginatedResult(List<T> data, int count, int pageNumber, int pageSize)
    {
        Data = data;
        TotalCount = count;
        CurrentPage = pageNumber;
        PageSize = pageSize;

        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    }
}