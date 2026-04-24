using System.Security.Claims;
using MediatR;
using WoongBlog.Api.Common.Application.Files;
using WoongBlog.Application.Modules.Media.Results;

namespace WoongBlog.Application.Modules.Media.Commands.UploadMediaAsset;

public sealed record UploadMediaAssetCommand(
    IUploadedFile? File,
    string? Bucket,
    ClaimsPrincipal User) : IRequest<MediaUploadResult>;
