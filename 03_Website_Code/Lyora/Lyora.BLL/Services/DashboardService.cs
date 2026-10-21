using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

public class DashboardService
{
    private readonly DashboardRepository _dashboard;

    public DashboardService(DashboardRepository dashboard)
    {
        _dashboard = dashboard;
    }

    public DashboardSummary GetSummary() => _dashboard.GetSummary();
}
