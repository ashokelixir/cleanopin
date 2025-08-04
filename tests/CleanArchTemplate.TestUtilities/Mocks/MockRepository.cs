using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Common;
using Moq;
using System.Linq.Expressions;

namespace CleanArchTemplate.TestUtilities.Mocks;

public static class MockRepository
{
    public static Mock<IRepository<T>> Create<T>() where T : BaseEntity
    {
        var mock = new Mock<IRepository<T>>();
        
        // Setup default behaviors
        mock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => null);
            
        mock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<T>());
            
        mock.Setup(x => x.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((T entity, CancellationToken ct) => entity);
            
        mock.Setup(x => x.UpdateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        mock.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    public static Mock<IRepository<T>> WithEntity<T>(this Mock<IRepository<T>> mock, T entity) 
        where T : BaseEntity
    {
        mock.Setup(x => x.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
            
        return mock;
    }

    public static Mock<IRepository<T>> WithEntities<T>(this Mock<IRepository<T>> mock, IEnumerable<T> entities) 
        where T : BaseEntity
    {
        mock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
            
        foreach (var entity in entities)
        {
            mock.Setup(x => x.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
        }
            
        return mock;
    }

    public static Mock<IRepository<T>> WithQuery<T>(this Mock<IRepository<T>> mock, IQueryable<T> queryable) 
        where T : BaseEntity
    {
        mock.Setup(x => x.Query())
            .Returns(queryable);
            
        return mock;
    }
}