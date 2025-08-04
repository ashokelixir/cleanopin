using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.TestUtilities.Builders;

public class RoleBuilder
{
    private string _name = "TestRole";
    private string _description = "Test role description";
    private bool _isActive = true;
    private Guid _id = Guid.NewGuid();
    private DateTime _createdAt = DateTime.UtcNow;
    private string _createdBy = "system";

    public RoleBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public RoleBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RoleBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public RoleBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public RoleBuilder CreatedBy(string createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public RoleBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public Role Build()
    {
        var role = new Role(_name, _description);
        
        if (!_isActive)
        {
            role.Deactivate();
        }
        
        return role;
    }

    public static implicit operator Role(RoleBuilder builder) => builder.Build();
}