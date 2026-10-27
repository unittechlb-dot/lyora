using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>View Analytics Reports use case: sales, best sellers, customer demographics, raw-material usage.</summary>
public class AnalyticsService
{
    private readonly AnalyticsRepository _analytics;

    public AnalyticsService(AnalyticsRepository analytics)
    {
        _analytics = analytics;
    }

    public AnalyticsReport GetReport() => _analytics.GetReport();
}
