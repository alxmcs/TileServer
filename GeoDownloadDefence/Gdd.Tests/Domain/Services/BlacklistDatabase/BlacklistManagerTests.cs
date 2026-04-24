using Gdd.Domain.Services;
using Moq;

namespace Gdd.Tests.Domain.Services.BlacklistDatabase;

public class BlacklistManagerTests
{
    [Fact]
    public async Task BlockIpAddressAsync_DelegatesToRepositoryAddIp()
    {
        var repo = new Mock<IBlacklistRepository>();
        repo.Setup(r => r.AddIp("1.2.3.4")).Returns(Task.CompletedTask).Verifiable();
        var manager = new BlacklistManager(repo.Object);

        await manager.BlockIpAddressAsync("1.2.3.4");

        repo.Verify(r => r.AddIp("1.2.3.4"), Times.Once);
    }

    [Fact]
    public async Task IsBlockedAsync_ReturnsTrue_WhenRepositoryReportsIpInDatabase()
    {
        var repo = new Mock<IBlacklistRepository>();
        repo.Setup(r => r.IsInDatabase("1.2.3.4")).ReturnsAsync(true);
        var manager = new BlacklistManager(repo.Object);

        var result = await manager.IsBlockedAsync("1.2.3.4");

        Assert.True(result);
        repo.Verify(r => r.IsInDatabase("1.2.3.4"), Times.Once);
    }

    [Fact]
    public async Task IsBlockedAsync_ReturnsFalse_WhenRepositoryReportsIpNotInDatabase()
    {
        var repo = new Mock<IBlacklistRepository>();
        repo.Setup(r => r.IsInDatabase(It.IsAny<string>())).ReturnsAsync(false);
        var manager = new BlacklistManager(repo.Object);

        var result = await manager.IsBlockedAsync("8.8.8.8");

        Assert.False(result);
    }

    [Fact]
    public async Task BlockIpAddressAsync_PropagatesExceptions()
    {
        var repo = new Mock<IBlacklistRepository>();
        repo.Setup(r => r.AddIp(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("db down"));
        var manager = new BlacklistManager(repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.BlockIpAddressAsync("1.1.1.1"));
    }
}
