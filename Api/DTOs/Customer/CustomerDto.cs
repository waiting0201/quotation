namespace QuotationApi.DTOs.Customer;

/// <summary>
/// 客戶列表項目 DTO
/// 用於 GET /api/customers
/// </summary>
public class CustomerListDto
{
    public int CustomerId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CustomerTypeName { get; set; }
    public string? Phone { get; set; }
    public DateTime? CreateDate { get; set; }

    /// <summary>是否有關聯報價單（用於刪除保護）</summary>
    public bool HasQuotations { get; set; }
}

/// <summary>
/// 客戶詳情 DTO
/// 用於 GET /api/customers/{id}
/// </summary>
public class CustomerDetailDto
{
    public int CustomerId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int? CustomerTypeId { get; set; }
    public string? CustomerTypeName { get; set; }
    public int? CountryId { get; set; }
    public string? CountryName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? VatNumber { get; set; }
    public string? Logo { get; set; }
    public DateTime? CreateDate { get; set; }
    public List<ContactDto> Contacts { get; set; } = new();
}

/// <summary>
/// 聯絡人 DTO（巢狀於 CustomerDetailDto，也用於建立/更新）
/// </summary>
public class ContactDto
{
    public Guid? ContactId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Ext { get; set; }
}

/// <summary>
/// 建立/更新客戶 DTO
/// 用於 POST /api/customers 和 PUT /api/customers/{id}
/// </summary>
public class CustomerCreateUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int? CustomerTypeId { get; set; }
    public int? CountryId { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? VatNumber { get; set; }
    public List<ContactDto>? Contacts { get; set; }
}
