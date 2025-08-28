using MediatR;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.ActivateTenant;

/// <summary>
/// Command to activate a tenant
/// </summary>
public class ActivateTenantCommand : IRequest
{
    /// <summary>
    /// The tenant ID
    /// </summary>
    public Guid TenantId { get; set; }
}