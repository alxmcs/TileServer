using System.Net;
using Gdd.Domain.Model;
using Gdd.Domain.Services;
using Gdd.Domain.Shared.Enums;
using Gdd.HttpApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gdd.Tests.HttpApi.Controllers;

public class TileProxyControllerTests
{
    private readonly Mock<IIpSecurityService> _ipSecurity = new();
    private readonly Mock<ITileManager> _tileManager = new();

    private TileProxyController CreateSut(IPAddress? remoteIp)
    {
        var controller = new TileProxyController(
            NullLogger<TileProxyController>.Instance,
            _ipSecurity.Object,
            _tileManager.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteIp;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    [Fact]
    public async Task GetTile_ReturnsBadRequest_WhenRemoteIpAddressIsNull()
    {
        var controller = CreateSut(remoteIp: null);

        var result = await controller.GetTile(1, 2, 3);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, status.StatusCode);
        _ipSecurity.VerifyNoOtherCalls();
        _tileManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTile_ReturnsForbidden_WhenIpIsBlocked()
    {
        _ipSecurity
            .Setup(s => s.ValidateRequestAndReturnIpStatus(It.IsAny<string>(), It.IsAny<TileCoordinates>()))
            .ReturnsAsync(IpStatus.Blocked);
        var controller = CreateSut(IPAddress.Parse("10.0.0.1"));

        var result = await controller.GetTile(1, 2, 3);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
        _tileManager.Verify(t => t.GetTile(It.IsAny<TileCoordinates>()), Times.Never);
    }

    [Fact]
    public async Task GetTile_ReturnsForbidden_WhenIpIsPotentialIntruder()
    {
        _ipSecurity
            .Setup(s => s.ValidateRequestAndReturnIpStatus(It.IsAny<string>(), It.IsAny<TileCoordinates>()))
            .ReturnsAsync(IpStatus.PotentialIntruder);
        var controller = CreateSut(IPAddress.Parse("10.0.0.2"));

        var result = await controller.GetTile(1, 2, 3);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
        _tileManager.Verify(t => t.GetTile(It.IsAny<TileCoordinates>()), Times.Never);
    }

    [Fact]
    public async Task GetTile_ReturnsFileWithPngContentType_WhenIpIsSafe()
    {
        var tileBytes = new byte[] { 9, 8, 7 };
        _ipSecurity
            .Setup(s => s.ValidateRequestAndReturnIpStatus(It.IsAny<string>(), It.IsAny<TileCoordinates>()))
            .ReturnsAsync(IpStatus.Safe);
        _tileManager
            .Setup(t => t.GetTile(It.Is<TileCoordinates>(c => c.Z == 5 && c.X == 6 && c.Y == 7)))
            .ReturnsAsync(tileBytes);
        var controller = CreateSut(IPAddress.Parse("10.0.0.3"));

        var result = await controller.GetTile(z: 5, x: 6, y: 7);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Same(tileBytes, file.FileContents);
    }

    [Fact]
    public async Task GetTile_ForwardsClientIpAndCoordinatesToIpSecurityService()
    {
        string? capturedIp = null;
        TileCoordinates? capturedCoords = null;
        _ipSecurity
            .Setup(s => s.ValidateRequestAndReturnIpStatus(It.IsAny<string>(), It.IsAny<TileCoordinates>()))
            .Callback<string, TileCoordinates>((ip, c) => { capturedIp = ip; capturedCoords = c; })
            .ReturnsAsync(IpStatus.Safe);
        _tileManager
            .Setup(t => t.GetTile(It.IsAny<TileCoordinates>()))
            .ReturnsAsync(Array.Empty<byte>());
        var controller = CreateSut(IPAddress.Parse("127.0.0.1"));

        await controller.GetTile(z: 10, x: 20, y: 30);

        Assert.Equal("127.0.0.1", capturedIp);
        Assert.NotNull(capturedCoords);
        Assert.Equal(10, capturedCoords!.Z);
        Assert.Equal(20, capturedCoords.X);
        Assert.Equal(30, capturedCoords.Y);
    }

    [Fact]
    public async Task GetTile_Returns500_WhenTileManagerThrowsInvalidDataException()
    {
        _ipSecurity
            .Setup(s => s.ValidateRequestAndReturnIpStatus(It.IsAny<string>(), It.IsAny<TileCoordinates>()))
            .ReturnsAsync(IpStatus.Safe);
        _tileManager
            .Setup(t => t.GetTile(It.IsAny<TileCoordinates>()))
            .ThrowsAsync(new InvalidDataException("bad tile"));
        var controller = CreateSut(IPAddress.Parse("10.0.0.4"));

        var result = await controller.GetTile(1, 2, 3);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, obj.StatusCode);
        Assert.Contains("bad tile", obj.Value?.ToString());
    }
}
