namespace Shared.Infrastructure.Messaging;

public sealed class SqsOptions
{
    public const string SectionName = "Sqs";

    /// <summary>Set for LocalStack / local dev; left null in real AWS so the SDK uses the region endpoint.</summary>
    public string? ServiceUrl { get; set; }
    public string Region { get; set; } = "us-east-1";

    /// <summary>Maps a published event's short type name to the queue URLs of every module subscribed to it.</summary>
    public Dictionary<string, List<string>> Routes { get; set; } = new();
}
