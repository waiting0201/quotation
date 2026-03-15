using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Lookup;
using QuotationApi.Models;

namespace QuotationApi.Services;

public class LookupService
{
    private readonly QuotationDbContext _db;

    public LookupService(QuotationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 取得權限樹（排除 limid == 1 的根節點），以 parentId 建構階層結構。
    /// 頂層節點 parentId == 0，子項目掛在對應的頂層節點下。
    /// </summary>
    public async Task<List<PermissionNodeDto>> GetPermissionTreeAsync()
    {
        var allLims = await _db.Lims
            .AsNoTracking()
            .Where(l => l.Limid != 1) // 排除根節點
            .OrderBy(l => l.Freq)
            .ThenBy(l => l.Limid)
            .ToListAsync();

        // 轉為 DTO
        var dtos = allLims.Select(l => new PermissionNodeDto
        {
            LimId = l.Limid,
            Key = l.Key ?? string.Empty,
            Value = l.Value ?? string.Empty,
            ParentId = l.Parentid
        }).ToList();

        // 建構樹：頂層 = parentId == 0 或 parentId == 1
        var lookup = dtos.ToLookup(d => d.ParentId);
        var roots = dtos.Where(d => d.ParentId == 0 || d.ParentId == 1).ToList();

        foreach (var root in roots)
        {
            var children = lookup[root.LimId].ToList();
            root.Children = children.Count > 0 ? children : null;
        }

        return roots;
    }
}
