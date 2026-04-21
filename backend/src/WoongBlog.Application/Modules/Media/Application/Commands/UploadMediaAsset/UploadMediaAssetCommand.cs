using System.Security.Claims;
using MediatR;
using WoongBlog.Api.Common.Application.Files;
using WoongBlog.Api.Modules.Media.Application.Results;

namespace WoongBlog.Api.Modules.Media.Application.Commands.UploadMediaAsset;

public sealed record UploadMediaAssetCommand(
    IUploadedFile? File,
    string? Bucket,
    ClaimsPrincipal User) : IRequest<MediaUploadResult>;
