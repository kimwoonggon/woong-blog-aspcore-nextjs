using System.Net.Http.Json;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public class UploadsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UploadsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_WithoutFile_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("public-resume"), "bucket");

        var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_Pdf_CreatesAsset_AndDelete_RemovesIt()
    {
        var client = _factory.CreateAuthenticatedClient();
        using var form = new MultipartFormDataContent();
        using var stream = new MemoryStream(new byte[] { 37, 80, 68, 70 });
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "resume.pdf");
        form.Add(new StringContent("public-resume"), "bucket");

        var uploadResponse = await client.PostAsync("/api/uploads", form);
        uploadResponse.EnsureSuccessStatusCode();
        var payload = await uploadResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var assetId = payload!["id"];
        var relativePath = payload["path"];

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            Assert.True(dbContext.Assets.Any(asset => asset.Id == Guid.Parse(assetId)));

            var mediaRoot = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value.MediaRoot;
            Assert.True(File.Exists(Path.Combine(mediaRoot, relativePath)));
        }

        var deleteResponse = await client.DeleteAsync($"/api/uploads?id={assetId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using var validationScope = _factory.Services.CreateScope();
        var validationDb = validationScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        Assert.False(validationDb.Assets.Any(asset => asset.Id == Guid.Parse(assetId)));

        var validationMediaRoot = validationScope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value.MediaRoot;
        Assert.False(File.Exists(Path.Combine(validationMediaRoot, relativePath)));
    }

    [Fact]
    public async Task Upload_UnsupportedImage_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        using var form = new MultipartFormDataContent();
        using var stream = new MemoryStream([1, 2, 3]);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/bmp");
        form.Add(fileContent, "file", "legacy.bmp");
        form.Add(new StringContent("blogs/inline"), "bucket");

        var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unsupported image type", body);
    }

    [Fact]
    public async Task Delete_MissingAsset_ReturnsNotFound()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/uploads?id={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
