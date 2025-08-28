using CleanArchTemplate.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.ActivateTenant;

/// <summary>
/// Handler for ActivateTenantCommand
/// </summary>
public class ActivateTenantCommandHandler : IRequestHandler<ActivateTenantCommand>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<ActivateTenantCommandHandler> _logger;

    public ActivateTenantCommandHandler(
        ITenantService tenantService,
        ILogger<ActivateTenantCommandHandler> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the ActivateTenantCommand
    /// </summary>
    /// <param name="request">The command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating tenant: {TenantId}", request.TenantId);

        await _tenantService.ActivateTenantAsync(request.TenantId, cancellationToken);

        _logger.LogInformation("Tenant activated successfully: {TenantId}", request.TenantId);
    }
}