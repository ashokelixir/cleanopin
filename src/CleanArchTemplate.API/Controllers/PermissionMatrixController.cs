using CleanArchTemplate.API.Attributes;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Application.Features.Permissions.Commands.BulkAssignPermissions;
using CleanArchTemplate.Application.Features.Permissions.Queries.GetRolePermissionMatrix;
using CleanArchTemplate.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for managing role-permission matrix operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionMatrixController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionMatrixController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the complete role-permission matrix
    /// </summary>
    /// <returns>Role-permission matrix data</returns>
    [HttpGet]
    [RequirePermission("PermissionMatrix.Read")]
    [ProducesResponseType(typeof(PermissionMatrixDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermissionMatrixDto>> GetRolePermissionMatrix()
    {
        var query = new GetRolePermissionMatrixQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get role-permission matrix for a specific role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <returns>Role-permission matrix data for the specified role</returns>
    [HttpGet("role/{roleId:guid}")]
    [RequirePermission("PermissionMatrix.Read")]
    [ProducesResponseType(typeof(PermissionMatrixDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermissionMatrixDto>> GetRolePermissionMatrix(Guid roleId)
    {
        var query = new GetRolePermissionMatrixQuery { RoleId = roleId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Bulk assign permissions to a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="request">Permission assignment request</param>
    /// <returns>Updated role-permission assignments</returns>
    [HttpPost("role/{roleId:guid}/permissions")]
    [RequirePermission("PermissionMatrix.Update")]
    [ProducesResponseType(typeof(BulkAssignPermissionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkAssignPermissionsResult>> BulkAssignPermissionsToRole(
        Guid roleId, 
        [FromBody] BulkAssignPermissionsRequest request)
    {
        var command = new BulkAssignPermissionsCommand
        {
            RoleId = roleId,
            Permissions = request.PermissionIds.Select(id => new PermissionAssignmentDto
            {
                PermissionId = id,
                State = PermissionState.Grant
            }),
            Reason = request.Reason
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Remove permissions from a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="request">Permission removal request</param>
    /// <returns>Updated role-permission assignments</returns>
    [HttpDelete("role/{roleId:guid}/permissions")]
    [RequirePermission("PermissionMatrix.Update")]
    [ProducesResponseType(typeof(BulkAssignPermissionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkAssignPermissionsResult>> RemovePermissionsFromRole(
        Guid roleId, 
        [FromBody] BulkRemovePermissionsRequest request)
    {
        var command = new BulkAssignPermissionsCommand
        {
            RoleId = roleId,
            Permissions = request.PermissionIds.Select(id => new PermissionAssignmentDto
            {
                PermissionId = id,
                State = PermissionState.Deny // Using Deny to indicate removal
            }),
            Reason = request.Reason
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Get matrix statistics and summary information
    /// </summary>
    /// <returns>Permission matrix statistics</returns>
    [HttpGet("statistics")]
    [RequirePermission("PermissionMatrix.Read")]
    [ProducesResponseType(typeof(PermissionMatrixStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermissionMatrixStatisticsDto>> GetMatrixStatistics()
    {
        var query = new GetRolePermissionMatrixQuery { IncludeStatistics = true };
        var result = await _mediator.Send(query);
        
        var statistics = new PermissionMatrixStatisticsDto
        {
            SystemStatistics = new SystemPermissionStatisticsDto
            {
                TotalRoles = result.Roles.Count(),
                TotalPermissions = result.Permissions.Count(),
                TotalRolePermissionAssignments = result.Assignments.Count(),
                ActivePermissions = result.Permissions.Count(p => p.IsActive),
                InactivePermissions = result.Permissions.Count(p => !p.IsActive),
                AveragePermissionsPerRole = result.Roles.Any() ? 
                    (double)result.Assignments.Count() / result.Roles.Count() : 0
            },
            RoleStatistics = result.Roles
                .OrderByDescending(r => result.Assignments.Count(a => a.RoleId == r.Id))
                .Take(5)
                .Select(r => new RolePermissionStatisticsDto 
                { 
                    Role = r,
                    PermissionCount = result.Assignments.Count(a => a.RoleId == r.Id)
                }),
            PermissionUsage = result.Permissions
                .OrderByDescending(p => result.Assignments.Count(a => a.PermissionId == p.Id))
                .Take(5)
                .Select(p => new PermissionUsageStatisticsDto 
                { 
                    Permission = p,
                    RoleCount = result.Assignments.Count(a => a.PermissionId == p.Id),
                    RoleUsagePercentage = result.Roles.Any() ? 
                        (double)result.Assignments.Count(a => a.PermissionId == p.Id) / result.Roles.Count() * 100 : 0
                })
        };

        return Ok(statistics);
    }

    /// <summary>
    /// Export permission matrix data
    /// </summary>
    /// <param name="format">Export format (csv, excel, json)</param>
    /// <returns>Exported matrix data</returns>
    [HttpGet("export")]
    [RequirePermission("PermissionMatrix.Export")]
    [ProducesResponseType(typeof(PermissionMatrixExportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermissionMatrixExportDto>> ExportMatrix([FromQuery] string format = "json")
    {
        if (!new[] { "csv", "excel", "json" }.Contains(format.ToLower()))
        {
            return BadRequest("Invalid export format. Supported formats: csv, excel, json");
        }

        var query = new GetRolePermissionMatrixQuery();
        var result = await _mediator.Send(query);

        var exportData = new PermissionMatrixExportDto
        {
            Format = format.ToLower(),
            ExportedAt = DateTime.UtcNow,
            Data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(result),
            ContentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/json"
            },
            FileName = $"permission-matrix-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{format.ToLower()}",
            Metadata = new PermissionMatrixExportMetadataDto
            {
                RoleCount = result.Roles.Count(),
                PermissionCount = result.Permissions.Count(),
                AssignmentCount = result.Assignments.Count()
            }
        };

        return Ok(exportData);
    }
}

/// <summary>
/// Request model for bulk permission assignment
/// </summary>
public class BulkAssignPermissionsRequest
{
    public IEnumerable<Guid> PermissionIds { get; set; } = new List<Guid>();
    public string? Reason { get; set; }
}

/// <summary>
/// Request model for bulk permission removal
/// </summary>
public class BulkRemovePermissionsRequest
{
    public IEnumerable<Guid> PermissionIds { get; set; } = new List<Guid>();
    public string? Reason { get; set; }
}

