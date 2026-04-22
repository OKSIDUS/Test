using ProzorroAnalytics.Domain.Models;
using System.Text.Json.Serialization;

namespace ProzorroAnalytics.Infrastructure.Models.Http;

public class TenderDetailResponse
{
    public Tender? Data { get; init; }

}

