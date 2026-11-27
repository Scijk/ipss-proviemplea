using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts;

public sealed record ApiResponse<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] T? Data = default);

public sealed record PagedApiResponse<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] IReadOnlyList<T> Data,
    [property: JsonPropertyName("meta")] PaginationMetadata Meta);

public sealed record PaginationMetadata(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("totalPages")] int TotalPages);
