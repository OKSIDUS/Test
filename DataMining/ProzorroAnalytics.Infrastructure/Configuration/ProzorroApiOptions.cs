namespace ProzorroAnalytics.Infrastructure.Configuration;

public class ProzorroApiOptions
{
    public const string SectionName = "ProzorroApi";

    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
}