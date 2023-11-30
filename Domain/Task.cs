using System.Text.Json.Serialization;

namespace Domain;

public record Task(
    string id,
    string fileName,
    string originalPath,
    TaskState state
) {
    public string? processedPath { get; init; }
};
