namespace AiDotnetBuilder.Api.Models;

public sealed record AppBlueprintRequest(
    string Goal,
    string? Domain,
    string? Stack,
    string? Constraints,
    string? OutputLanguage = "pt-BR");
