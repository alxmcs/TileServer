using Gdd.Application.Services;
using Gdd.Domain.Model;
using Gdd.Domain.Model.Requests;
using Gdd.Domain.Services;
using Gdd.Domain.Shared.Enums;
using Moq;

namespace Gdd.Tests.Application.Services;

public class IpSecurityServiceTests
{
    private readonly Mock<IBlacklistManager> _blacklist = new();
    private readonly Mock<IIntrusionDetectionService> _intrusion = new();
    private readonly TileCoordinates _coords = new() { X = 1, Y = 2, Z = 3 };

    private IpSecurityService CreateSut() => new(_blacklist.Object, _intrusion.Object);

    [Fact]
    public async Task ValidateRequestAndReturnIpStatus_ReturnsBlocked_WhenIpIsOnBlacklist()
    {
        _blacklist.Setup(b => b.IsBlockedAsync("1.1.1.1")).ReturnsAsync(true);

        var status = await CreateSut().ValidateRequestAndReturnIpStatus("1.1.1.1", _coords);

        Assert.Equal(IpStatus.Blocked, status);
        _intrusion.Verify(i => i.IsPotentialIntruderAsync(It.IsAny<Request>()), Times.Never);
        _blacklist.Verify(b => b.BlockIpAddressAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ValidateRequestAndReturnIpStatus_ReturnsPotentialIntruderAndBlocksIp_WhenIntrusionDetected()
    {
        _blacklist.Setup(b => b.IsBlockedAsync("2.2.2.2")).ReturnsAsync(false);
        _intrusion.Setup(i => i.IsPotentialIntruderAsync(It.IsAny<Request>())).ReturnsAsync(true);

        var status = await CreateSut().ValidateRequestAndReturnIpStatus("2.2.2.2", _coords);

        Assert.Equal(IpStatus.PotentialIntruder, status);
        _blacklist.Verify(b => b.BlockIpAddressAsync("2.2.2.2"), Times.Once);
    }

    [Fact]
    public async Task ValidateRequestAndReturnIpStatus_ReturnsSafe_WhenNotBlockedAndNotIntruder()
    {
        _blacklist.Setup(b => b.IsBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _intrusion.Setup(i => i.IsPotentialIntruderAsync(It.IsAny<Request>())).ReturnsAsync(false);

        var status = await CreateSut().ValidateRequestAndReturnIpStatus("3.3.3.3", _coords);

        Assert.Equal(IpStatus.Safe, status);
        _blacklist.Verify(b => b.BlockIpAddressAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ValidateRequestAndReturnIpStatus_PassesClientIpAndCoordinatesToIntrusionDetection()
    {
        Request? captured = null;
        _blacklist.Setup(b => b.IsBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _intrusion.Setup(i => i.IsPotentialIntruderAsync(It.IsAny<Request>()))
                  .Callback<Request>(r => captured = r)
                  .ReturnsAsync(false);

        var before = DateTime.UtcNow;
        await CreateSut().ValidateRequestAndReturnIpStatus("4.4.4.4", _coords);
        var after = DateTime.UtcNow;

        Assert.NotNull(captured);
        Assert.Equal("4.4.4.4", captured!.ClientIp);
        Assert.Equal(_coords.X, captured.Coordinates.X);
        Assert.Equal(_coords.Y, captured.Coordinates.Y);
        Assert.Equal(_coords.Z, captured.Coordinates.Z);
        Assert.InRange(captured.RequestTime, before, after);
    }
}
