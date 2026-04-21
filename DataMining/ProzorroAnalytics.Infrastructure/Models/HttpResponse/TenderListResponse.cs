namespace ProzorroAnalytics.Infrastructure.Models.Http
{
    public class TenderListResponse
    {
        public IReadOnlyList<TenderItem> Data { get; init; } = [];
        public PageInfo Next_Page { get; init; } = new();
        public PageInfo Prev_Page { get; init; } = new();
    }

    public class TenderItem
    {
        public string Id { get; init; } = null!;
        public DateTime DateModified { get; init; }
    }

    public class PageInfo
    {
        public string Offset { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string Uri { get; init; } = string.Empty;
    }

}
