namespace WoongBlog.Api.Common.Application.Files;

public interface IUploadedFile
{
    string FileName { get; }
    string ContentType { get; }
    long Length { get; }
    Stream OpenReadStream();
}
