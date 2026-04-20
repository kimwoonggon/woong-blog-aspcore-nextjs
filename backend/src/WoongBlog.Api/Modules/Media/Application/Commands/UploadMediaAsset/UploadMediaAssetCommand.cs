using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using WoongBlog.Api.Modules.Media.Application.Results;

namespace WoongBlog.Api.Modules.Media.Application.Commands.UploadMediaAsset;

public sealed record UploadMediaAssetCommand(
    IFormFile? File,
    string? Bucket,
    ClaimsPrincipal User) : IRequest<MediaUploadResult>;
