using AiDotnetBuilder.Api.Models;
using AiDotnetBuilder.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<OpenAiBlueprintService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/blueprint", async (
    AppBlueprintRequest request,
    OpenAiBlueprintService service,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Goal))
    {
        return Results.BadRequest(new { error = "O campo 'goal' é obrigatório." });
    }

    var apiKey = ResolveApiKey(configuration);

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.BadRequest(new { error = "Configure OpenAI:ApiKey ou OPENAI_API_KEY." });
    }

    try
    {
        var result = await service.GenerateBlueprintAsync(request, apiKey, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "Erro ao gerar blueprint", detail: ex.Message);
    }
});

app.MapPost("/api/generate-files", async (
    CodeGenerationRequest request,
    OpenAiBlueprintService service,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Goal))
    {
        return Results.BadRequest(new { error = "O campo 'goal' é obrigatório." });
    }

    var apiKey = ResolveApiKey(configuration);

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.BadRequest(new { error = "Configure OpenAI:ApiKey ou OPENAI_API_KEY." });
    }

    try
    {
        var result = await service.GenerateFilesAsync(request, apiKey, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "Erro ao gerar arquivos", detail: ex.Message);
    }
});

app.Run();

static string? ResolveApiKey(IConfiguration configuration)
{
    return configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
}
