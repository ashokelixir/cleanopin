using CleanArchTemplate.Application.Common.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Command to update a tenant
/// </summary>
public class UpdateTenantCommand : IRequest<TenantInfo>
{
    /// <summary>
    /// The tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The tenant's display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant configuration
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
}