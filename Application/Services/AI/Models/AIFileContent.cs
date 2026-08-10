namespace Application.Services.AI.Models;

public sealed class AIFileContent
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public byte[] Content { get; init; } = [];

    public bool IsEmpty => Content.Length == 0;
}
