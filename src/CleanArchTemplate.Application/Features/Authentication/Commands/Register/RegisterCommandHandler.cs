using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Shared.Models;
using CleanArchTemplate.Shared.Utilities;
using MediatR;

namespace CleanArchTemplate.Application.Features.Authentication.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"=== REGISTER HANDLER HIT === Processing registration for: {request.Email}");
        
        // Check if user already exists
        var email = Email.Create(request.Email);
        Console.WriteLine($"Email object created: {email.Value}");
        
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        Console.WriteLine($"Existing user check completed. Found: {existingUser != null}");
        if (existingUser != null)
        {
            return Result<RegisterResponse>.BadRequest("User with this email already exists");
        }

        // Hash password
        var passwordHash = PasswordHelper.HashPassword(request.Password);

        // Create user
        var user = User.Create(
            email,
            request.FirstName,
            request.LastName,
            passwordHash);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email.Value,
            FullName = user.FullName,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt
        };

        return Result<RegisterResponse>.Success(response);
    }
}