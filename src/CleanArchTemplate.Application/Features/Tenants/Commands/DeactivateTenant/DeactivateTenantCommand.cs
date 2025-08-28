using MediatR;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.DeactivateTenant;

/// <summary>
/// Command to deactivate a tenant
/// </summary>
public class DeactivateTenantCommand : IRequest
{
    /// <summary>
    /// The tenant ID
    /// </summary>
    public Guid TenantId { get; set; }
}