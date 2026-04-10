namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public static class WorkVideoSourceTypes
{
    public const string YouTube = "youtube";
    public const string Local = "local";
    public const string R2 = "r2";
}

public static class WorkVideoUploadSessionStatuses
{
    public const string Issued = "Issued";
    public const string Uploaded = "Uploaded";
    public const string Confirmed = "Confirmed";
    public const string Expired = "Expired";
    public const string Failed = "Failed";
}

public static class VideoStorageCleanupJobStatuses
{
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}
