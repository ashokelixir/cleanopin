using System.Reflection;
using NetArchTest.Rules;
using FluentAssertions;
using Xunit;

namespace CleanArchTemplate.ArchitectureTests;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(CleanArchTemplate.Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(CleanArchTemplate.Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(CleanArchTemplate.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(CleanArchTemplate.API.Controllers.AuthController).Assembly;

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(ApplicationAssembly.GetName().Name)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_API()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(ApiAssembly.GetName().Name)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_API()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(ApiAssembly.GetName().Name)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Controllers_Should_End_With_Controller()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("CleanArchTemplate.API.Controllers")
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Controllers_Should_Be_In_Controllers_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .ResideInNamespace("CleanArchTemplate.API.Controllers")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Entities_Should_Inherit_From_BaseEntity()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("CleanArchTemplate.Domain.Entities")
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(CleanArchTemplate.Domain.Common.BaseEntity))
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Repositories_Should_End_With_Repository()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("CleanArchTemplate.Infrastructure.Data.Repositories")
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveNameMatching(".*Base.*")
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_Should_End_With_CommandHandler()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryHandlers_Should_End_With_QueryHandler()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void ValueObjects_Should_Be_In_ValueObjects_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("CleanArchTemplate.Domain.ValueObjects")
            .Should()
            .ResideInNamespace("CleanArchTemplate.Domain.ValueObjects")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Events_Should_End_With_Event()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("CleanArchTemplate.Domain.Events")
            .Should()
            .HaveNameEndingWith("Event")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Services_Should_End_With_Service()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("CleanArchTemplate.Infrastructure.Services")
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveNameMatching(".*Dispatcher")
            .And()
            .DoNotHaveNameMatching(".*Result")
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Interfaces_Should_Start_With_I()
    {
        // Arrange & Act
        var applicationResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        var domainResult = Types.InAssembly(DomainAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        // Assert
        applicationResult.IsSuccessful.Should().BeTrue();
        domainResult.IsSuccessful.Should().BeTrue();
    }
}