namespace WoongBlog.Api.Application.Content;

public sealed class StoredJsonPresentationException : Exception
{
    public StoredJsonPresentationException(
        string entityType,
        string entityKey,
        string fieldName,
        string message,
        Exception? innerException = null) : base(message, innerException)
    {
        EntityType = entityType;
        EntityKey = entityKey;
        FieldName = fieldName;
    }

    public string EntityType { get; }

    public string EntityKey { get; }

    public string FieldName { get; }
}
