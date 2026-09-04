using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTracker.Services;

namespace TravelTracker.Tests.Services;

public class BuildInfoServiceTests
{
    [Fact]
    public async Task GetBuildInfoAsync_WhenFileExists_ReturnsAndCachesBuildInfo()
    {
        var root = CreateWebRoot("{\"BuildNumber\":\"42\",\"BranchName\":\"main\"}");
        try
        {
            var service = CreateService(root);

            var first = await service.GetBuildInfoAsync();
            File.Delete(Path.Combine(root, "buildinfo.json"));
            var second = await service.GetBuildInfoAsync();

            Assert.NotNull(first);
            Assert.Equal("42", first.BuildNumber);
            Assert.Same(first, second);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetBuildInfoAsync_WhenFileIsMissing_ReturnsNull()
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var result = await CreateService(root).GetBuildInfoAsync();

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetBuildInfoAsync_WhenFileIsInvalid_ReturnsNull()
    {
        var root = CreateWebRoot("not-json");
        try
        {
            var result = await CreateService(root).GetBuildInfoAsync();

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateWebRoot(string content)
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        File.WriteAllText(Path.Combine(root, "buildinfo.json"), content);
        return root;
    }

    private static BuildInfoService CreateService(string webRoot)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.WebRootPath).Returns(webRoot);
        return new BuildInfoService(environment.Object, new Mock<ILogger<BuildInfoService>>().Object);
    }
}