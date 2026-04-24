using Gdd.Domain.Model;
using Gdd.Domain.Services;
using Gdd.Domain.Services.Tiles;
using Moq;

namespace Gdd.Tests.Domain.Services.Tiles;

public class TileManagerTests
{
    [Fact]
    public async Task GetTile_DelegatesToRepository()
    {
        var coords = new TileCoordinates { X = 1, Y = 2, Z = 3 };
        var expected = new byte[] { 1, 2, 3, 4 };
        var repo = new Mock<ITileRepository>();
        repo.Setup(r => r.GetTile(coords)).ReturnsAsync(expected);
        var manager = new TileManager(repo.Object);

        var result = await manager.GetTile(coords);

        Assert.Same(expected, result);
        repo.Verify(r => r.GetTile(coords), Times.Once);
    }

    [Fact]
    public async Task GetTile_PropagatesRepositoryExceptions()
    {
        var repo = new Mock<ITileRepository>();
        repo.Setup(r => r.GetTile(It.IsAny<TileCoordinates>())).ThrowsAsync(new InvalidDataException("boom"));
        var manager = new TileManager(repo.Object);

        await Assert.ThrowsAsync<InvalidDataException>(() => manager.GetTile(new TileCoordinates()));
    }
}
