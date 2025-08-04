using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Authentication.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IAuditLogService auditLogService,
        ILogger<LoginCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        // Find user by email
        var email = Email.Create(request.Email);
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Login failed - user not found for email: {Email}", request.Email);
            await _auditLogService.LogSecurityEventAsync(
                "LoginFailedUserNotFound",
                $"Login attempt with non-existent email: {request.Email}",
                additionalData: new { Email = request.Email });
            
            return Result<LoginResponse>.Unauthorized("Invalid email or password");
        }

        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed - invalid password for user: {UserId}", user.Id);
            await _auditLogService.LogSecurityEventAsync(
                "LoginFailedInvalidPassword",
                $"Invalid password attempt for user: {user.Email.Value}",
                user.Id,
                additionalData: new { Email = user.Email.Value });
            
            return Result<LoginResponse>.Unauthorized("Invalid email or password");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed - inactive user: {UserId}", user.Id);
            await _auditLogService.LogSecurityEventAsync(
                "LoginFailedInactiveUser",
                $"Login attempt by inactive user: {user.Email.Value}",
                user.Id,
                additionalData: new { Email = user.Email.Value });
            
            return Result<LoginResponse>.Forbidden("User account is deactivated");
        }

        // Generate tokens
        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
        var refreshTokenValue = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);

        // Create refresh token entity
        var refreshToken = Domain.Entities.RefreshToken.Create(
            refreshTokenValue,
            user.Id,
            DateTime.UtcNow.AddDays(7)); // 7 days expiration

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        // Record login
        user.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log successful login
        await _auditLogService.LogUserActionAsync(
            "UserLoggedIn",
            user.Id,
            $"User successfully logged in: {user.Email.Value}",
            new { 
                Email = user.Email.Value,
                LoginTime = DateTime.UtcNow,
                LastLoginAt = user.LastLoginAt
            });

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // TODO: Get from configuration
            UserEmail = user.Email.Value,
            UserFullName = user.FullName
        };

        return Result<LoginResponse>.Success(response);
    }
}