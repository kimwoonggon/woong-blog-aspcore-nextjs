using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence.Assets;

namespace WoongBlog.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Policy = "AdminOnly")]
public class UploadsController : ControllerBase
{
    private readonly IAssetStorageService _assetStorageService;

    public UploadsController(IAssetStorageService assetStorageService)
    {
        _assetStorageService = assetStorageService;
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

        var profileIdValue = User.FindFirst(AuthClaimTypes.ProfileId)?.Value;
        Guid? profileId = Guid.TryParse(profileIdValue, out var parsed) ? parsed : null;
        var result = await _assetStorageService.StoreAsync(file, bucket, profileId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!.Kind switch
            {
                AssetStorageErrorKind.InvalidBucket => BadRequest(new { error = result.Error.Message }),
                AssetStorageErrorKind.InvalidPath => Problem(result.Error.Message),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        var asset = result.Asset!;

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
        var result = await _assetStorageService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!.Kind switch
            {
                AssetStorageErrorKind.AssetNotFound => NotFound(new { error = result.Error.Message }),
                AssetStorageErrorKind.StoredPathInvalid => Conflict(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { success = true });
    }
}
