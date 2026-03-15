using Microsoft.AspNetCore.Mvc;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

public class LookupController
{
    private readonly LookupService _lookupService;

    public LookupController(LookupService lookupService)
    {
        _lookupService = lookupService;
    }

    public async Task<IActionResult> GetPermissions(RouteContext context)
    {
        var tree = await _lookupService.GetPermissionTreeAsync();
        return context.Ok(tree);
    }
}
