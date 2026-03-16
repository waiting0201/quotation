namespace QuotationApi.DTOs.Lookup;

/// <summary>
/// 國家列表項目 DTO
/// 用於 GET /api/countries，包含每個國家底下的客戶數量
/// </summary>
public class CountryListDto
{
    public int CountryId { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>使用此國家的客戶數量</summary>
    public int CustomerCount { get; set; }
}

/// <summary>
/// 新增/更新國家 DTO
/// 用於 POST /api/countries 和 PUT /api/countries/{id}
/// </summary>
public class CountryCreateUpdateDto
{
    public string Title { get; set; } = string.Empty;
}
