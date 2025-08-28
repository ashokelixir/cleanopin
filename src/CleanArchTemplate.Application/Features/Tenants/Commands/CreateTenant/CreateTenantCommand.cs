using CleanArchTemplate.Application.Common.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Command to create a new tenant
/// </summary>
public class CreateTenantCommand : IRequest<TenantInfo>
{
    /// <summary>
    /// The tenant's display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The tenant's unique identifier
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Optional database connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Optional tenant configuration
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    /// Optional subscription expiry date
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }
}