namespace ProzorroAnalytics.Application.Options;

public sealed class NightlyImportOptions
{
    public const string SectionName = "NightlyImport";
    public int UtcHour { get; init; } = 2;
}
