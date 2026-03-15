using System.Text.Json.Serialization;

namespace QuotationApi.DTOs.Auth;

public class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;

    [JsonPropertyName("user")]
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    [JsonPropertyName("userid")]
    public string Userid { get; set; } = null!;

    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("groupid")]
    public string? Groupid { get; set; }

    /// <summary>
    /// Key: lim key, Value: 對該功能的 CRUD 權限
    /// </summary>
    [JsonPropertyName("permissions")]
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class PermissionDto
{
    [JsonPropertyName("limid")]
    public int Limid { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = null!;

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("isQuery")]
    public bool IsQuery { get; set; }

    [JsonPropertyName("isInsert")]
    public bool IsInsert { get; set; }

    [JsonPropertyName("isUpdate")]
    public bool IsUpdate { get; set; }

    [JsonPropertyName("isDelete")]
    public bool IsDelete { get; set; }
}
