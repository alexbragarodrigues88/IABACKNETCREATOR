using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiDotnetBuilder.Api.Models;

namespace AiDotnetBuilder.Api.Services;

public sealed class OpenAiBlueprintService
{
    private const string Endpoint = "https://api.openai.com/v1/responses";
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiBlueprintService> _logger;

    public OpenAiBlueprintService(HttpClient httpClient, ILogger<OpenAiBlueprintService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AppBlueprintResponse> GenerateBlueprintAsync(AppBlueprintRequest request, string apiKey, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = "gpt-4.1-mini",
            input = BuildBlueprintPrompt(request)
        };

        var json = await CallResponsesApiAsync(payload, apiKey, cancellationToken);
        var text = ExtractText(json);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("A IA não retornou conteúdo no formato esperado.");
        }

        return new AppBlueprintResponse(text);
    }

    public async Task<CodeGenerationResponse> GenerateFilesAsync(CodeGenerationRequest request, string apiKey, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = "gpt-4.1-mini",
            input = BuildCodePrompt(request),
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "code_generation",
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            summary = new { type = "string" },
                            files = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        path = new { type = "string" },
                                        content = new { type = "string" }
                                    },
                                    required = new[] { "path", "content" }
                                }
                            },
                            nextSteps = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            }
                        },
                        required = new[] { "summary", "files", "nextSteps" }
                    }
                }
            }
        };

        var json = await CallResponsesApiAsync(payload, apiKey, cancellationToken);
        var text = ExtractText(json);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("A IA não retornou JSON para geração de arquivos.");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<CodeGenerationResponseDto>(text, options)
            ?? throw new InvalidOperationException("Não foi possível desserializar a resposta de geração de arquivos.");

        var files = result.Files
            .Where(f => !string.IsNullOrWhiteSpace(f.Path))
            .Select(f => new GeneratedFile(f.Path, f.Content ?? string.Empty))
            .Take(Math.Clamp(request.MaxFiles, 1, 30))
            .ToList();

        return new CodeGenerationResponse(
            result.Summary ?? "Arquivos gerados com sucesso.",
            files,
            result.NextSteps ?? Array.Empty<string>());
    }

    private async Task<string> CallResponsesApiAsync(object payload, string apiKey, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erro na chamada da API OpenAI. Status: {StatusCode}. Body: {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Falha ao chamar a OpenAI. Verifique chave e parâmetros.");
        }

        return body;
    }

    private static string? ExtractText(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (root.TryGetProperty("output", out var outputArray) && outputArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in outputArray.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var contentArray) || contentArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var content in contentArray.EnumerateArray())
                {
                    if (content.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    {
                        return text.GetString();
                    }
                }
            }
        }

        return null;
    }

    private static string BuildBlueprintPrompt(AppBlueprintRequest request)
    {
        var language = string.IsNullOrWhiteSpace(request.OutputLanguage) ? "pt-BR" : request.OutputLanguage;

        return $$"""
        Você é um arquiteto sênior .NET.
        Gere um plano prático para construir a aplicação solicitada.

        Objetivo: {{request.Goal}}
        Domínio de negócio: {{request.Domain ?? "Não informado"}}
        Stack preferida: {{request.Stack ?? "ASP.NET Core + React"}}
        Restrições: {{request.Constraints ?? "Sem restrições"}}

        Retorne em {{language}} com:
        1) Arquitetura sugerida
        2) Estrutura de pastas
        3) Principais endpoints/fluxos
        4) Modelo de dados inicial
        5) Passo a passo de implementação em sprints
        6) Riscos + mitigação
        """;
    }

    private static string BuildCodePrompt(CodeGenerationRequest request)
    {
        var language = string.IsNullOrWhiteSpace(request.OutputLanguage) ? "pt-BR" : request.OutputLanguage;
        var maxFiles = Math.Clamp(request.MaxFiles, 1, 30);

        return $$"""
        Você é um especialista em scaffolding de aplicações .NET.
        Gere arquivos iniciais coerentes para o objetivo abaixo.

        Objetivo: {{request.Goal}}
        Domínio: {{request.Domain ?? "Não informado"}}
        Stack: {{request.Stack ?? "ASP.NET Core Web API + React"}}
        Restrições: {{request.Constraints ?? "Sem restrições"}}

        Regras:
        - Máximo de {{maxFiles}} arquivos.
        - Inclua caminhos relativos realistas (ex: src/MinhaApi/Program.cs).
        - Conteúdos devem ser válidos e úteis para iniciar o projeto.
        - Explique em {{language}} no campo summary e nextSteps.
        """;
    }

    private sealed record CodeGenerationResponseDto(
        string? Summary,
        IReadOnlyList<GeneratedFileDto> Files,
        IReadOnlyList<string>? NextSteps);

    private sealed record GeneratedFileDto(string Path, string? Content);
}
