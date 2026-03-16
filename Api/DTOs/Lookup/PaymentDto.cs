namespace QuotationApi.DTOs.Lookup;

/// <summary>
/// 付款條件列表項目 DTO
/// 用於 GET /api/payments
/// </summary>
public class PaymentListDto
{
    public int PaymentId { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 新增/更新付款條件 DTO
/// 用於 POST /api/payments 和 PUT /api/payments/{id}
/// </summary>
public class PaymentCreateUpdateDto
{
    public string? Remark { get; set; }
}
