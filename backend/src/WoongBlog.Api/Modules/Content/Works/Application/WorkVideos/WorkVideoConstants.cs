namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public static class WorkVideoSourceTypes
{
    public const string YouTube = "youtube";
    public const string Local = "local";
    public const string R2 = "r2";
    public const string Hls = "hls";
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

public static class WorkVideoHlsSourceKey
{
    private const char Separator = ':';

    public static string Create(string storageType, string manifestStorageKey)
    {
        return $"{storageType}{Separator}{manifestStorageKey}";
    }

    public static bool TryParse(string sourceKey, out string storageType, out string manifestStorageKey)
    {
        storageType = string.Empty;
        manifestStorageKey = string.Empty;

        var separatorIndex = sourceKey.IndexOf(Separator, StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == sourceKey.Length - 1)
        {
            return false;
        }

        storageType = sourceKey[..separatorIndex];
        manifestStorageKey = sourceKey[(separatorIndex + 1)..];
        return true;
    }
}
