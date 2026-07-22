using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutVent.Infrastructure.Api;

/// <summary>All AutVent API responses are wrapped in this envelope.</summary>
internal sealed record ApiResponse<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("errors")] string[]? Errors);

/// <summary>Separate record for refresh-token response (differs from LoginResponse).</summary>
internal sealed record RefreshTokenApiResponse(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("refreshToken")] string RefreshToken,
    [property: JsonPropertyName("refreshTokenExpiresAtUtc")] DateTime RefreshTokenExpiresAtUtc);

public abstract class ApiClientBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Reads a raw (non-wrapped) response — used only for login/refresh where the data IS the envelope.</summary>\n    protected static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)\n    {\n        var body = await response.Content.ReadAsStringAsync(cancellationToken);\n\n        if (!response.IsSuccessStatusCode)\n        {\n            var errorMsg = TryExtractMessage(body)\n                ?? $\"{(int)response.StatusCode} {response.ReasonPhrase}: Please try again.\";\n            throw new HttpRequestException(errorMsg);\n        }\n\n        T? payload;\n        try\n        {\n            payload = JsonSerializer.Deserialize<T>(body, JsonOptions);\n        }\n        catch\n        {\n            throw new HttpRequestException(\"Unexpected response format from server.\");\n        }\n\n        if (payload is null)\n        {\n            throw new HttpRequestException(\"Server returned an empty response.\");\n        }\n\n        return payload;\n    }

    /// <summary>Reads a wrapped ApiResponse{T} envelope and returns the inner data.</summary>
    protected static async Task<T> ReadApiResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Try to extract a human-readable message from the envelope before falling back to raw body
            var errorMsg = TryExtractMessage(body)
                ?? $"{(int)response.StatusCode} {response.ReasonPhrase}: Please try again.";
            throw new HttpRequestException(errorMsg);
        }

        ApiResponse<T>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ApiResponse<T>>(body, JsonOptions);
        }
        catch
        {
            throw new HttpRequestException("Unexpected response format from server.");
        }

        if (envelope is null || !envelope.Success || envelope.Data is null)
        {
            var msg = envelope?.Message;
            if (string.IsNullOrWhiteSpace(msg) && envelope?.Errors?.Length > 0)
                msg = string.Join(" ", envelope.Errors);
            throw new HttpRequestException(string.IsNullOrWhiteSpace(msg)
                ? "The server returned an unsuccessful response."
                : msg);
        }

        return envelope.Data;
    }

    /// <summary>POSTs and reads the wrapped envelope; throws if not successful.</summary>
    protected static async Task PostVoidApiAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var errorMsg = TryExtractMessage(body)
            ?? $"{(int)response.StatusCode} {response.ReasonPhrase}: Please try again.";
        throw new HttpRequestException(errorMsg);
    }

    /// <summary>Tries to pull message/errors from an API envelope JSON string. Returns null if not parseable.</summary>
    private static string? TryExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Collect validation errors array first (most specific)
            if (root.TryGetProperty("errors", out var errorsEl) && errorsEl.ValueKind == JsonValueKind.Array)
            {
                var parts = errorsEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                if (parts.Count > 0) return string.Join(" ", parts);
            }

            // Fall back to message field
            if (root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
            {
                var msg = msgEl.GetString();
                if (!string.IsNullOrWhiteSpace(msg)) return msg;
            }
        }
        catch { /* not valid JSON — caller will use fallback */ }

        return null;
    }
}

