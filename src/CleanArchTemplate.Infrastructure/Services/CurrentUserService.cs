using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Service implementation for accessing current user context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

    public string? UserEmail => _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

    public string? UserName => _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string GetAuditIdentifier()
    {
        if (IsAuthenticated)
        {
            return UserEmail ?? UserId ?? UserName ?? "authenticated-user";
        }

        return "system";
    }
}