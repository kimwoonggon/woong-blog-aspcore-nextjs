using Microsoft.AspNetCore.Http;
using WoongBlog.Api.Common.Application.Files;

namespace WoongBlog.Api.Common.Api;

internal sealed class FormFileUpload(IFormFile file) : IUploadedFile
{
    public string FileName => file.FileName;
    public string ContentType => file.ContentType;
    public long Length => file.Length;
    public Stream OpenReadStream() => file.OpenReadStream();

    public static IUploadedFile? From(IFormFile? file) => file is null ? null : new FormFileUpload(file);
}
