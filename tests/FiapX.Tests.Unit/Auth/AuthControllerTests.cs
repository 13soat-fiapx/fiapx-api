using FiapX.Api.Controllers;
using FiapX.Application.Abstractions.Auth;
using FiapX.Application.Auth.Responses;
using FiapX.Application.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FiapX.Tests.Unit.Auth;

[TestClass]
public sealed class AuthControllerTests
{
    [TestMethod]
    public async Task GetCurrentUserAsync_ShouldReturnCurrentUserProfile()
    {
        var userProfileServiceMock = new Mock<IUserProfileService>(MockBehavior.Strict);
        userProfileServiceMock
            .Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(
                "auth0|user-1",
                "User One",
                "user.one@example.com"));

        var appService = new AuthAppService(userProfileServiceMock.Object);
        var controller = new AuthController(appService);

        var result = await controller.GetCurrentUserAsync(CancellationToken.None);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);

        var response = okResult.Value as CurrentUserResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual("auth0|user-1", response.Id);
        Assert.AreEqual("User One", response.Name);
        Assert.AreEqual("user.one@example.com", response.Email);

        userProfileServiceMock.Verify(
            service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
