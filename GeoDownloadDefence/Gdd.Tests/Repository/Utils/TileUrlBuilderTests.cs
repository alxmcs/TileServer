using Gdd.Domain.Model;
using Gdd.Repository.Utils;

namespace Gdd.Tests.Repository.Utils;

public class TileUrlBuilderTests
{
    [Fact]
    public void Build_ConstructsUrlWithZxyOrder()
    {
        var coords = new TileCoordinates { X = 3, Y = 4, Z = 5 };

        var url = TileUrlBuilder.Build("http://example.com", coords);

        Assert.Equal("http://example.com/tiles/5/3/4", url);
    }

    [Fact]
    public void Build_WithZeroCoordinates_IncludesZeros()
    {
        var coords = new TileCoordinates { X = 0, Y = 0, Z = 0 };

        var url = TileUrlBuilder.Build("https://tiles.test", coords);

        Assert.Equal("https://tiles.test/tiles/0/0/0", url);
    }

    [Fact]
    public void Build_WithEmptyBaseUrl_StartsWithTilesPath()
    {
        var coords = new TileCoordinates { X = 1, Y = 2, Z = 3 };

        var url = TileUrlBuilder.Build(string.Empty, coords);

        Assert.Equal("/tiles/3/1/2", url);
    }

    [Fact]
    public void Build_WithNegativeCoordinates_PreservesNegativeValues()
    {
        var coords = new TileCoordinates { X = -1, Y = -2, Z = -3 };

        var url = TileUrlBuilder.Build("http://host", coords);

        Assert.Equal("http://host/tiles/-3/-1/-2", url);
    }

    [Theory]
    [InlineData("http://a", 10, 20, 30, "http://a/tiles/30/10/20")]
    [InlineData("https://b.example.com:8080", 100, 200, 5, "https://b.example.com:8080/tiles/5/100/200")]
    public void Build_VariousInputs_ReturnsExpected(string baseUrl, int x, int y, int z, string expected)
    {
        var coords = new TileCoordinates { X = x, Y = y, Z = z };

        var url = TileUrlBuilder.Build(baseUrl, coords);

        Assert.Equal(expected, url);
    }
}
