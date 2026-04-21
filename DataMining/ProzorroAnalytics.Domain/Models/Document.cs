namespace ProzorroAnalytics.Domain.Models;

public class Document
{
    public string Id { get; init; } = null!;
    public string? DocumentType { get; init; }
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? Format { get; init; }
    public string? Hash { get; init; }
    public string? Description { get; init; }
    public string? Confidentiality { get; init; }
    public string? DocumentOf { get; init; }
    public string? Author { get; init; }
    public string? Language { get; init; }
    public string? RelatedItem { get; init; }
    public DateTimeOffset? DatePublished { get; init; }
    public DateTimeOffset? DateModified { get; init; }
}
