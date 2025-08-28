using CleanArchTemplate.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.DeactivateTenant;

/// <summary>
/// Handler for DeactivateTenantCommand
/// </summary>
public class DeactivateTenantCommandHandler : IRequestHandler<DeactivateTenantCommand>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<DeactivateTenantCommandHandler> _logger;

    public DeactivateTenantCommandHandler(
        ITenantService tenantService,
        ILogger<DeactivateTenantCommandHandler> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the DeactivateTenantCommand
    /// </summary>
    /// <param name="request">The command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating tenant: {TenantId}", request.TenantId);

        await _tenantService.DeactivateTenantAsync(request.TenantId, cancellationToken);

        _logger.LogInformation("Tenant deactivated successfully: {TenantId}", request.TenantId);
    }
}