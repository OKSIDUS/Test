namespace ProzorroAnalytics.Application.DTOs.Prozorro
{
    public class TenderListPage
    {
        public List<TenderItem> Items { get; init; } = new();
        public string? Offset { get; init; } = string.Empty;
    }

    public class TenderItem
    {
        public required string Id { get; init; }
        public required DateTime DateModified { get; init; }
    }
}
