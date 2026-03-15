using Microsoft.AspNetCore.Mvc;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

public class DashboardController
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// GET dashboard
    /// 回傳儀表板所有資料
    /// </summary>
    public async Task<IActionResult> GetDashboard(RouteContext context)
    {
        var data = await _dashboardService.GetDashboardAsync();
        return context.Ok(data);
    }
}
