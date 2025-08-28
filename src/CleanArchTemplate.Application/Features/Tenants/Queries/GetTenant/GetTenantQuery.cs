using CleanArchTemplate.Application.Common.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Tenants.Queries.GetTenant;

/// <summary>
/// Query to get a tenant by ID
/// </summary>
public class GetTenantQuery : IRequest<TenantInfo?>
{
    /// <summary>
    /// The tenant ID
    /// </summary>
    public Guid TenantId { get; set; }
}