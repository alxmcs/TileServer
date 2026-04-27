using Gdd.Domain.Model;
using Gdd.Domain.Model.Requests;
using Gdd.Domain.Services;
using Moq;

namespace Gdd.Tests.Domain.Services.RequestsDatabase;

public class IntrusionDetectionServiceTests
{
    private const string ClientIp = "10.0.0.1";

    private static Request MakeRequest(int seq, int x, int y, int z)
    {
        // seq controls RequestTime; larger seq => later time
        return new Request
        {
            ClientIp = ClientIp,
            RequestTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seq),
            Coordinates = new TileCoordinates { X = x, Y = y, Z = z }
        };
    }

    [Fact]
    public async Task IsPotentialIntruderAsync_AddsCurrentRequestToRepository()
    {
        var repo = new Mock<IRequestsRepository>();
        repo.Setup(r => r.AddRequest(It.IsAny<Request>())).ReturnsAsync("id-1").Verifiable();
        repo.Setup(r => r.GetAllRequestsByIp(ClientIp, It.IsAny<GetListRequestFilter>()))
            .ReturnsAsync(new List<Request>());
        var sut = new IntrusionDetectionService(repo.Object);

        var current = MakeRequest(0, 0, 0, 0);
        await sut.IsPotentialIntruderAsync(current);

        repo.Verify(r => r.AddRequest(current), Times.Once);
    }

    [Fact]
    public async Task IsPotentialIntruderAsync_ReturnsFalse_WhenFewerThan100Requests()
    {
        var requests = Enumerable.Range(0, 99).Select(i => MakeRequest(i, i, i, i)).ToList();
        var repo = new Mock<IRequestsRepository>();
        repo.Setup(r => r.AddRequest(It.IsAny<Request>())).ReturnsAsync("id");
        repo.Setup(r => r.GetAllRequestsByIp(ClientIp, It.IsAny<GetListRequestFilter>()))
            .ReturnsAsync(requests);
        var sut = new IntrusionDetectionService(repo.Object);

        var result = await sut.IsPotentialIntruderAsync(MakeRequest(100, 0, 0, 0));

        Assert.False(result);
    }

    [Fact]
    public async Task IsPotentialIntruderAsync_ReturnsTrue_WhenTimeOrderMatchesCoordinateOrderAscending()
    {
        // Coordinates ordered by (Z, X, Y) ascending coincide with ascending request time.
        var requests = Enumerable.Range(0, 100).Select(i => MakeRequest(i, i, i, i)).ToList();
        var repo = new Mock<IRequestsRepository>();
        repo.Setup(r => r.AddRequest(It.IsAny<Request>())).ReturnsAsync("id");
        repo.Setup(r => r.GetAllRequestsByIp(ClientIp, It.IsAny<GetListRequestFilter>()))
            .ReturnsAsync(requests);
        var sut = new IntrusionDetectionService(repo.Object);

        var result = await sut.IsPotentialIntruderAsync(MakeRequest(999, 0, 0, 0));

        Assert.True(result);
    }

    [Fact]
    public async Task IsPotentialIntruderAsync_ReturnsTrue_WhenTimeOrderIsReverseOfCoordinateOrder()
    {
        // Later timestamps paired with smaller coordinates => reverse-order match.
        var requests = Enumerable.Range(0, 100)
            .Select(i => MakeRequest(seq: i, x: 99 - i, y: 99 - i, z: 99 - i))
            .ToList();
        var repo = new Mock<IRequestsRepository>();
        repo.Setup(r => r.AddRequest(It.IsAny<Request>())).ReturnsAsync("id");
        repo.Setup(r => r.GetAllRequestsByIp(ClientIp, It.IsAny<GetListRequestFilter>()))
            .ReturnsAsync(requests);
        var sut = new IntrusionDetectionService(repo.Object);

        var result = await sut.IsPotentialIntruderAsync(MakeRequest(999, 0, 0, 0));

        Assert.True(result);
    }

    [Fact]
    public async Task IsPotentialIntruderAsync_ReturnsFalse_WhenCoordinatesAreUnrelatedToTime()
    {
        // Deterministically scrambled coordinate assignment that is neither ascending
        // nor descending when sorted by time.
        var requests = new List<Request>(100);
        for (var i = 0; i < 100; i++)
        {
            var scrambled = (i * 37) % 100; // permutation distinct from ascending/descending
            requests.Add(MakeRequest(seq: i, x: scrambled, y: scrambled, z: scrambled));
        }

        var repo = new Mock<IRequestsRepository>();
        repo.Setup(r => r.AddRequest(It.IsAny<Request>())).ReturnsAsync("id");
        repo.Setup(r => r.GetAllRequestsByIp(ClientIp, It.IsAny<GetListRequestFilter>()))
            .ReturnsAsync(requests);
        var sut = new IntrusionDetectionService(repo.Object);

        var result = await sut.IsPotentialIntruderAsync(MakeRequest(999, 0, 0, 0));

        Assert.False(result);
    }

    [Fact]
    public async Task IsPotentialIntruderAsync_QueriesRepositoryWithExpectedFilter()
    {
        GetListRequestFilter? capturedFilter = null;
        var repo = new Mock<IRequestsRepository>();
        repo.Setup(r => r.AddRequest(It.IsAny<Request>())).ReturnsAsync("id");
        repo.Setup(r => r.GetAllRequestsByIp(ClientIp, It.IsAny<GetListRequestFilter>()))
            .Callback<string, GetListRequestFilter?>((_, f) => capturedFilter = f)
            .ReturnsAsync(new List<Request>());
        var sut = new IntrusionDetectionService(repo.Object);

        await sut.IsPotentialIntruderAsync(MakeRequest(0, 0, 0, 0));

        Assert.NotNull(capturedFilter);
        Assert.Equal(0, capturedFilter!.From);
        Assert.Equal(10000, capturedFilter.Size);
        Assert.Equal(20, capturedFilter.Take);
    }
}
