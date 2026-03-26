using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Domain.Entities;
using Portfolio.Api.Infrastructure.Auth;
using Portfolio.Api.Infrastructure.Persistence;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Policy = "AdminOnly")]
public class UploadsController : ControllerBase
{
    private readonly PortfolioDbContext _dbContext;
    private readonly AuthOptions _authOptions;

    public UploadsController(PortfolioDbContext dbContext, Microsoft.Extensions.Options.IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _authOptions = authOptions.Value;
    }

    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        var formData = await Request.ReadFormAsync(cancellationToken);
        var file = formData.Files["file"];
        var bucket = formData["bucket"].ToString();

        if (file is null)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        bucket = string.IsNullOrWhiteSpace(bucket) ? "media" : bucket;
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(bucket, fileName).Replace('\\', '/');
        var physicalPath = Path.Combine(_authOptions.MediaRoot, relativePath);
        var directory = Path.GetDirectoryName(physicalPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return Problem("The upload path could not be resolved.");
        }

        Directory.CreateDirectory(directory);

        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var profileIdValue = User.FindFirst(AuthClaimTypes.ProfileId)?.Value;
        Guid? profileId = Guid.TryParse(profileIdValue, out var parsed) ? parsed : null;

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Bucket = bucket,
            Path = relativePath,
            PublicUrl = $"/media/{relativePath}",
            MimeType = file.ContentType,
            Size = file.Length,
            Kind = GetKind(file.ContentType),
            CreatedBy = profileId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = asset.Id,
            url = asset.PublicUrl,
            path = asset.Path
        });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        var asset = await _dbContext.Assets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (asset is null)
        {
            return NotFound(new { error = "Asset not found" });
        }

        var physicalPath = Path.Combine(_authOptions.MediaRoot, asset.Path);
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }

        _dbContext.Assets.Remove(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }

    private static string GetKind(string mimeType)
    {
        if (mimeType.StartsWith("image/")) return "image";
        if (mimeType == "application/pdf") return "pdf";
        if (mimeType.StartsWith("audio/")) return "audio";
        return "other";
    }
}
