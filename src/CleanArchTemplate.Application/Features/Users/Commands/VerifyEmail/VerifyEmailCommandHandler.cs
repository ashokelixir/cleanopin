using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Users.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to verify email with token");

            var user = await _unitOfWork.Users.GetByEmailVerificationTokenAsync(request.Token, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Invalid email verification token provided");
                return Result.BadRequest("Invalid verification token.");
            }

            try
            {
                user.VerifyEmail(request.Token);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Email verification failed: {Message}", ex.Message);
                return Result.BadRequest(ex.Message);
            }

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log the audit event
            await _auditLogService.LogUserActionAsync(
                "EmailVerified",
                user.Id,
                $"Email verified for user '{user.Email.Value}'");

            _logger.LogInformation("Email successfully verified for user: {UserId}", user.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while verifying email");
            return Result.InternalError("An error occurred while verifying the email.");
        }
    }
}