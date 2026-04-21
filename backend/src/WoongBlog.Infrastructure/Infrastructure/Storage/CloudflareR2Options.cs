namespace WoongBlog.Api.Infrastructure.Storage;

public sealed class CloudflareR2Options
{
    public const string SectionName = "CloudflareR2";

    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string BrowserEndpoint { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public bool ForceEnabledInDevelopment { get; set; }

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(AccessKeyId)
               && !string.IsNullOrWhiteSpace(SecretAccessKey)
               && !string.IsNullOrWhiteSpace(BucketName)
               && !string.IsNullOrWhiteSpace(Endpoint)
               && !string.IsNullOrWhiteSpace(PublicUrl);
    }
}
