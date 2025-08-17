using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Users.Commands.InvalidateUserSessions;

public class InvalidateUserSessionsCommandHandler : IRequestHandler<InvalidateUserSessionsCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<InvalidateUserSessionsCommandHandler> _logger;

    public InvalidateUserSessionsCommandHandler(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<InvalidateUserSessionsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(InvalidateUserSessionsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to invalidate all sessions for user: {UserId}", request.UserId);

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", request.UserId);
                return Result.NotFound("User not found.");
            }

            // Revoke all refresh tokens for the user
            user.RevokeAllRefreshTokens();
            
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log the audit event
            await _auditLogService.LogSecurityEventAsync(
                "SessionsInvalidated",
                $"All sessions invalidated for user '{user.Email.Value}'",
                request.UserId);

            _logger.LogInformation("All sessions successfully invalidated for user: {UserId}", request.UserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while invalidating sessions for user: {UserId}", request.UserId);
            return Result.InternalError("An error occurred while invalidating user sessions.");
        }
    }
}