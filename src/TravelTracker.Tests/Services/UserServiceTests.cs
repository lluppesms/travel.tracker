using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser_WhenUserExists()
    {
        // Arrange
        int userId = 123;
        var expectedUser = new User 
        { 
            Id = userId, 
            Email = "test@example.com",
            Username = "testuser"
        };

        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        var service = new UserService(mockRepository.Object);

        // Act
        var result = await service.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        Assert.Equal("test@example.com", result.Email);
        mockRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        // Arrange
        int userId = 999;

        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        var service = new UserService(mockRepository.Object);

        // Act
        var result = await service.GetUserByIdAsync(userId);

        // Assert
        Assert.Null(result);
        mockRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_CreatesNewUser_WhenUserDoesNotExist()
    {
        // Arrange
        var entraIdUserId = "entra-123";
        var username = "newuser";
        var email = "newuser@example.com";

        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(repo => repo.GetByEntraIdAsync(entraIdUserId))
            .ReturnsAsync((User?)null);
        mockRepository.Setup(repo => repo.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var service = new UserService(mockRepository.Object);

        // Act
        var result = await service.GetOrCreateUserAsync(entraIdUserId, username, email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entraIdUserId, result.EntraIdUserId);
        Assert.Equal(email, result.Email);
        Assert.Equal(username, result.Username);
        Assert.NotNull(result.LastLoginDate);
        mockRepository.Verify(repo => repo.GetByEntraIdAsync(entraIdUserId), Times.Once);
        mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Once);
        mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_UpdatesExistingUser_WhenUserExists()
    {
        // Arrange
        var entraIdUserId = "entra-123";
        var username = "existinguser";
        var email = "existing@example.com";

        var existingUser = new User 
        { 
            Id = 123, 
            EntraIdUserId = entraIdUserId,
            Email = email,
            Username = username,
            LastLoginDate = null
        };

        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(repo => repo.GetByEntraIdAsync(entraIdUserId))
            .ReturnsAsync(existingUser);
        mockRepository.Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var service = new UserService(mockRepository.Object);

        // Act
        var result = await service.GetOrCreateUserAsync(entraIdUserId, username, email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entraIdUserId, result.EntraIdUserId);
        Assert.NotNull(result.LastLoginDate);
        mockRepository.Verify(repo => repo.GetByEntraIdAsync(entraIdUserId), Times.Once);
        mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Once);
        mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_UpdatesLastLoginDate()
    {
        // Arrange
        int userId = 123;
        var existingUser = new User 
        { 
            Id = userId, 
            Email = "test@example.com",
            LastLoginDate = DateTime.UtcNow.AddDays(-7)
        };

        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        mockRepository.Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var service = new UserService(mockRepository.Object);

        // Act
        await service.UpdateLastLoginAsync(userId);

        // Assert
        mockRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
        mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task GetUserByEntraIdAsync_ReturnsUser_WhenUserExists()
    {
        // Arrange
        var entraIdUserId = "entra-123";
        var expectedUser = new User 
        { 
            Id = 123,
            EntraIdUserId = entraIdUserId,
            Email = "test@example.com",
            Username = "testuser"
        };

        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(repo => repo.GetByEntraIdAsync(entraIdUserId))
            .ReturnsAsync(expectedUser);

        var service = new UserService(mockRepository.Object);

        // Act
        var result = await service.GetUserByEntraIdAsync(entraIdUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entraIdUserId, result!.EntraIdUserId);
        Assert.Equal("test@example.com", result.Email);
        mockRepository.Verify(repo => repo.GetByEntraIdAsync(entraIdUserId), Times.Once);
    }
}
