namespace AiDotnetBuilder.Api.Models;

public sealed record CodeGenerationRequest(
    string Goal,
    string? Domain,
    string? Stack,
    string? Constraints,
    string? OutputLanguage = "pt-BR",
    int MaxFiles = 12);
