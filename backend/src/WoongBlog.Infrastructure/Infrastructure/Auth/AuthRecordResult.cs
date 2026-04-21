namespace WoongBlog.Api.Infrastructure.Auth;

public sealed record AuthRecordResult(Guid ProfileId, string Email, string Role, Guid SessionId, string DisplayName);
