namespace AiDotnetBuilder.Api.Models;

public sealed record CodeGenerationResponse(
    string Summary,
    IReadOnlyList<GeneratedFile> Files,
    IReadOnlyList<string> NextSteps);
