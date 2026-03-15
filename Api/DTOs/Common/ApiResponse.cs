using System.Text.Json.Serialization;

namespace QuotationApi.DTOs.Common;

/// <summary>
/// 成功回應的統一包裝格式
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data) => new() { Data = data };
}

/// <summary>
/// 錯誤回應的統一包裝格式
/// </summary>
public class ApiErrorResponse
{
    [JsonPropertyName("error")]
    public ApiError Error { get; set; } = null!;

    public static ApiErrorResponse Create(string code, string message, string? details = null)
        => new()
        {
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
}

public class ApiError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
