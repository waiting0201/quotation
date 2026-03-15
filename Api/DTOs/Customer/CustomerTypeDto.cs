namespace QuotationApi.DTOs.Customer;

/// <summary>
/// 客戶分類列表項目 DTO
/// 用於 GET /api/customer-types，包含每個分類底下的客戶數量
/// </summary>
public class CustomerTypeListDto
{
    public int CustomerTypeId { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>使用此分類的客戶數量</summary>
    public int CustomerCount { get; set; }
}

/// <summary>
/// 新增/更新客戶分類 DTO
/// 用於 POST /api/customer-types 和 PUT /api/customer-types/{id}
/// </summary>
public class CustomerTypeCreateUpdateDto
{
    public string Title { get; set; } = string.Empty;
}
