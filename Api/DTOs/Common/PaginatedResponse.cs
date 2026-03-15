using System.Text.Json.Serialization;

namespace QuotationApi.DTOs.Common;

/// <summary>
/// 分頁回應的統一包裝格式
/// { "data": [...], "pagination": { page, pageSize, totalCount, totalPages } }
/// </summary>
public class PaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();

    public static PaginatedResponse<T> Create(List<T> items, int page, int pageSize, int totalCount)
    {
        return new PaginatedResponse<T>
        {
            Data = items,
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }
}

public class PaginationInfo
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}
