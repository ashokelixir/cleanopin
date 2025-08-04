using CleanArchTemplate.Application.Common.Interfaces;
using Moq;

namespace CleanArchTemplate.TestUtilities.Mocks;

public static class MockUnitOfWork
{
    public static Mock<IUnitOfWork> Create()
    {
        var mock = new Mock<IUnitOfWork>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        
        // Setup repository mocks
        mock.Setup(x => x.Users).Returns(userRepositoryMock.Object);
        mock.Setup(x => x.Roles).Returns(roleRepositoryMock.Object);
        
        // Setup transaction methods
        mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
            
        mock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        mock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        mock.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    public static Mock<IUnitOfWork> WithUserRepository(this Mock<IUnitOfWork> mock, Mock<IUserRepository> userRepository)
    {
        mock.Setup(x => x.Users).Returns(userRepository.Object);
        return mock;
    }

    public static Mock<IUnitOfWork> WithRoleRepository(this Mock<IUnitOfWork> mock, Mock<IRoleRepository> roleRepository)
    {
        mock.Setup(x => x.Roles).Returns(roleRepository.Object);
        return mock;
    }
}