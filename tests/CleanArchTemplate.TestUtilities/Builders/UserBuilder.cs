using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;

namespace CleanArchTemplate.TestUtilities.Builders;

public class UserBuilder
{
    private string _emailString = "test@example.com";
    private string _firstName = "Test";
    private string _lastName = "User";
    private string _passwordHash = "hashedpassword";
    private bool _isEmailVerified = true;
    private bool _isActive = true;
    private DateTime? _lastLoginAt = null;
    private Guid _id = Guid.NewGuid();
    private DateTime _createdAt = DateTime.UtcNow;
    private string _createdBy = "system";

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _emailString = email;
        return this;
    }

    public UserBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public UserBuilder AsUnverified()
    {
        _isEmailVerified = false;
        return this;
    }

    public UserBuilder WithLastLogin(DateTime lastLogin)
    {
        _lastLoginAt = lastLogin;
        return this;
    }

    public UserBuilder CreatedBy(string createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public UserBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public User Build()
    {
        var email = Email.Create(_emailString);
        if (email == null)
            throw new ArgumentException($"Invalid email: {_emailString}");
            
        var user = User.Create(email, _firstName, _lastName, _passwordHash);
        
        // Set email verification status for testing
        user.SetEmailVerificationForTesting(_isEmailVerified);
        
        if (!_isActive)
        {
            user.Deactivate();
        }
        
        if (_lastLoginAt.HasValue)
        {
            user.RecordLogin();
        }
        
        return user;
    }

    public static implicit operator User(UserBuilder builder) => builder.Build();
}