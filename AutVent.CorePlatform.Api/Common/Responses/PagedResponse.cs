namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class PagedResponse<T>
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyCollection<T> Items { get; init; } = [];
}
