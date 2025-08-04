using AutoFixture;
using AutoFixture.Kernel;
using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CleanArchTemplate.TestUtilities.Common;

public abstract class BaseTest
{
    protected readonly IFixture Fixture;
    protected readonly IServiceProvider ServiceProvider;

    protected BaseTest()
    {
        Fixture = new Fixture();
        ConfigureFixture();
        ServiceProvider = CreateServiceProvider();
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add in-memory database for unit tests
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        return services.BuildServiceProvider();
    }

    private void ConfigureFixture()
    {
        // Configure AutoFixture to handle circular references
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Configure custom specimen builders for domain entities
        Fixture.Customizations.Add(new BaseEntitySpecimenBuilder());
        
        // Configure string length limits
        Fixture.Customizations.Add(new StringPropertyLengthOmitter());
    }
}

public class BaseEntitySpecimenBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && typeof(BaseEntity).IsAssignableFrom(type))
        {
            var specimen = context.Resolve(type);
            if (specimen is BaseEntity entity && entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }
            return specimen;
        }
        
        return new NoSpecimen();
    }
}

public class StringPropertyLengthOmitter : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is PropertyInfo propertyInfo && propertyInfo.PropertyType == typeof(string))
        {
            var maxLength = GetMaxLength(propertyInfo);
            if (maxLength > 0)
            {
                return context.Create<string>()[..Math.Min(maxLength - 1, context.Create<string>().Length)];
            }
        }
        
        return new NoSpecimen();
    }

    private static int GetMaxLength(PropertyInfo propertyInfo)
    {
        var maxLengthAttribute = propertyInfo.GetCustomAttribute<System.ComponentModel.DataAnnotations.MaxLengthAttribute>();
        return maxLengthAttribute?.Length ?? 0;
    }
}