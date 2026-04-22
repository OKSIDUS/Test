namespace ProzorroAnalytics.Infrastructure.Configuration;

public class ProzorroApiOptions
{
    public const string SectionName = "ProzorroApi";

    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public int RequestsPerSecond { get; init; } = 5;
    public int BurstSize { get; init; } = 10;
    public int QueueLimit { get; init; } = 100;
}