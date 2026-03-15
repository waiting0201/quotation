namespace QuotationApi.DTOs.Lookup;

public class PermissionNodeDto
{
    public int LimId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int ParentId { get; set; }
    public List<PermissionNodeDto>? Children { get; set; }
}
