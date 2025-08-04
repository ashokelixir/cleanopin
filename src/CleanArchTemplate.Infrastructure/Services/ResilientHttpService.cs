using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// HTTP service with resilience patterns for external API calls
/// </summary>
public class ResilientHttpService
{
    private readonly HttpClient _httpClient;
    private readonly IResilienceService _resilienceService;
    private readonly ILogger<ResilientHttpService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ResilientHttpService(
        HttpClient httpClient,
        IResilienceService resilienceService,
        ILogger<ResilientHttpService> logger)
    {
        _httpClient = httpClient;
        _resilienceService = resilienceService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Performs a GET request with resilience patterns
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>Deserialized response</returns>
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Making GET request to: {Endpoint}", endpoint);
                
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            },
            ApplicationConstants.ResiliencePolicies.ExternalApi);
    }

    /// <summary>
    /// Performs a GET request with fallback mechanism
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="fallbackValue">Fallback value if request fails</param>
    /// <returns>Response or fallback value</returns>
    public async Task<T> GetWithFallbackAsync<T>(string endpoint, T fallbackValue)
    {
        return await _resilienceService.ExecuteWithFallbackAsync(
            async () =>
            {
                _logger.LogDebug("Making GET request with fallback to: {Endpoint}", endpoint);
                
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return result ?? fallbackValue;
            },
            async () =>
            {
                _logger.LogWarning("Using fallback value for GET request to: {Endpoint}", endpoint);
                return await Task.FromResult(fallbackValue);
            },
            ApplicationConstants.ResiliencePolicies.ExternalApi);
    }

    /// <summary>
    /// Performs a POST request with resilience patterns
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="request">Request payload</param>
    /// <returns>Deserialized response</returns>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Making POST request to: {Endpoint}", endpoint);
                
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
            },
            ApplicationConstants.ResiliencePolicies.ExternalApi);
    }

    /// <summary>
    /// Performs a PUT request with resilience patterns
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="request">Request payload</param>
    /// <returns>Deserialized response</returns>
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Making PUT request to: {Endpoint}", endpoint);
                
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
            },
            ApplicationConstants.ResiliencePolicies.ExternalApi);
    }

    /// <summary>
    /// Performs a DELETE request with resilience patterns
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>Success status</returns>
    public async Task<bool> DeleteAsync(string endpoint)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Making DELETE request to: {Endpoint}", endpoint);
                
                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            },
            ApplicationConstants.ResiliencePolicies.ExternalApi);
    }

    /// <summary>
    /// Performs a critical operation with enhanced resilience
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="operation">HTTP operation</param>
    /// <returns>Operation result</returns>
    public async Task<T> ExecuteCriticalOperationAsync<T>(Func<Task<T>> operation)
    {
        return await _resilienceService.ExecuteAsync(
            operation,
            ApplicationConstants.ResiliencePolicies.Critical);
    }
}