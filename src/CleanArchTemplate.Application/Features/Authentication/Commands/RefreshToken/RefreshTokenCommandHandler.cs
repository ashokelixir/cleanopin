using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find the refresh token
        var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (refreshToken == null)
        {
            return Result<RefreshTokenResponse>.Unauthorized("Invalid refresh token");
        }

        // Check if token is active and not expired
        if (!refreshToken.IsActive || refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Result<RefreshTokenResponse>.Unauthorized("Refresh token has expired");
        }

        // Get user with roles and permissions
        var user = await _unitOfWork.Users.GetUserWithRolesByIdAsync(refreshToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return Result<RefreshTokenResponse>.Unauthorized("User not found or inactive");
        }

        // Revoke the old refresh token
        refreshToken.Revoke();

        // Generate new tokens
        var newAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
        var newRefreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);

        // Create new refresh token entity
        var newRefreshTokenEntity = Domain.Entities.RefreshToken.Create(
            newRefreshToken,
            user.Id,
            DateTime.UtcNow.AddDays(7)); // 7 days expiration

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // TODO: Get from configuration
        };

        return Result<RefreshTokenResponse>.Success(response);
    }
}