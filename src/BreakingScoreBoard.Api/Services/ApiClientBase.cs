using System.Net.Http.Json;
using System.Text.Json;
using BreakingScoreBoard.Api.Contracts;

namespace BreakingScoreBoard.Api.Services;

/// <summary>
/// Base class for API clients with common error handling and utilities
/// </summary>
public abstract class ApiClientBase
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;

    protected ApiClientBase(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Execute GET request and deserialize response
    /// </summary>
    protected async Task<T> GetAsync<T>(string url, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            await EnsureSuccessWithErrorDetails(response);
            
            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize response from {url}");
            }
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for GET {Url}", url);
            throw new InvalidOperationException($"Failed to connect to API: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response from GET {Url}", url);
            throw new InvalidOperationException($"Invalid response from API", ex);
        }
    }

    /// <summary>
    /// Execute POST request and deserialize response
    /// </summary>
    protected async Task<TResponse> PostAsync<TRequest, TResponse>(
        string url, 
        TRequest request, 
        string? pin = null,
        CancellationToken ct = default)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request)
            };

            if (!string.IsNullOrEmpty(pin))
            {
                httpRequest.Headers.Add("X-Pin", pin);
            }

            var response = await _httpClient.SendAsync(httpRequest, ct);
            await EnsureSuccessWithErrorDetails(response);
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize response from {url}");
            }
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for POST {Url}", url);
            throw new InvalidOperationException($"Failed to connect to API: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response from POST {Url}", url);
            throw new InvalidOperationException($"Invalid response from API", ex);
        }
    }

    /// <summary>
    /// Execute POST request without response body
    /// </summary>
    protected async Task PostAsync<TRequest>(
        string url, 
        TRequest request, 
        string? pin = null,
        CancellationToken ct = default)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request)
            };

            if (!string.IsNullOrEmpty(pin))
            {
                httpRequest.Headers.Add("X-Pin", pin);
            }

            var response = await _httpClient.SendAsync(httpRequest, ct);
            await EnsureSuccessWithErrorDetails(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for POST {Url}", url);
            throw new InvalidOperationException($"Failed to connect to API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Execute PATCH request and deserialize response
    /// </summary>
    protected async Task<TResponse> PatchAsync<TRequest, TResponse>(
        string url, 
        TRequest request, 
        string? pin = null,
        CancellationToken ct = default)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = JsonContent.Create(request)
            };

            if (!string.IsNullOrEmpty(pin))
            {
                httpRequest.Headers.Add("X-Pin", pin);
            }

            var response = await _httpClient.SendAsync(httpRequest, ct);
            await EnsureSuccessWithErrorDetails(response);
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize response from {url}");
            }
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for PATCH {Url}", url);
            throw new InvalidOperationException($"Failed to connect to API: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response from PATCH {Url}", url);
            throw new InvalidOperationException($"Invalid response from API", ex);
        }
    }

    /// <summary>
    /// Execute DELETE request
    /// </summary>
    protected async Task DeleteAsync(
        string url,
        string? pin = null,
        CancellationToken ct = default)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Delete, url);

            if (!string.IsNullOrEmpty(pin))
            {
                httpRequest.Headers.Add("X-Pin", pin);
            }

            var response = await _httpClient.SendAsync(httpRequest, ct);
            await EnsureSuccessWithErrorDetails(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for DELETE {Url}", url);
            throw new InvalidOperationException($"Failed to connect to API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ensure HTTP success status code and extract error details if available
    /// </summary>
    private async Task EnsureSuccessWithErrorDetails(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string errorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Error))
            {
                errorMessage = errorResponse.Error;
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    errorMessage += $": {content}";
                }
            }
        }
        catch
        {
            // Ignore deserialization errors, use default error message
        }

        _logger.LogWarning("API request failed: {StatusCode} - {Error}", response.StatusCode, errorMessage);
        throw new HttpRequestException(errorMessage);
    }
}
