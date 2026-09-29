using Asp.Versioning;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class IaEndpoints
{
    public static RouteGroupBuilder MapIaEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var ia = api.MapGroup("/ia").WithTags("IA (DeepSeek)").RequireAuthorization();

        // ── POST /ia/mejorar-descripcion-orden ────────────────────────────────
        ia.MapPost("/mejorar-descripcion-orden", async (
            [FromBody] MejorarDescripcionOrdenRequest req,
            IConfiguration config,
            IHttpClientFactory httpFactory,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Descripcion))
                return Results.BadRequest(APIResponse.Failure("Proporciona una descripción del problema."));

            var apiKey = config["DeepSeek:ApiKey"];
            var baseUrl = config["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com/v1";
            var model = config["DeepSeek:Model"] ?? "deepseek-chat";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Fallback local: sin API Key configurada, aplicamos una mejora heurística
                // básica (capitalización, puntuación, orden) para que la demo funcione siempre.
                return Results.Ok(APIResponse<IaTextResult>.Success(new IaTextResult
                {
                    Descripcion = MejorarDescripcionLocal(req.Descripcion, req.Vehiculo)
                }));
            }

            var prompt = $"""
                Eres un asesor de servicio experto en talleres automotrices en México.
                Reescribe la siguiente descripción del problema reportado por el cliente para que quede
                clara, profesional y útil para el mecánico que hará el diagnóstico. Conserva todos los
                síntomas mencionados, no inventes información nueva, usa español y sé conciso (máximo 3 líneas).
                Responde ÚNICAMENTE con un JSON con la clave "descripcion". Sin explicaciones adicionales.

                Vehículo: {req.Vehiculo ?? "(no especificado)"}
                Descripción actual: {req.Descripcion}
                """;

            try
            {
                var client = httpFactory.CreateClient("DeepSeek");
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.Timeout = TimeSpan.FromSeconds(30);

                var body = new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.4,
                    max_tokens = 400,
                    response_format = new { type = "json_object" }
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/v1/chat/completions", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync(ct);
                    logger.LogError("[IA] DeepSeek error {Status}: {Body}", response.StatusCode, errBody);
                    // Degradar a la mejora local en vez de fallar la experiencia del usuario
                    return Results.Ok(APIResponse<IaTextResult>.Success(new IaTextResult
                    {
                        Descripcion = MejorarDescripcionLocal(req.Descripcion, req.Vehiculo)
                    }));
                }

                var respJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(respJson);
                var rawText = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "{}";

                using var resultDoc = JsonDocument.Parse(rawText);
                var descripcion = resultDoc.RootElement.TryGetProperty("descripcion", out var d) ? d.GetString() ?? "" : "";

                return Results.Ok(APIResponse<IaTextResult>.Success(new IaTextResult
                {
                    Descripcion = string.IsNullOrWhiteSpace(descripcion) ? req.Descripcion : descripcion
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[IA] Error al llamar a DeepSeek para mejorar descripción de orden");
                return Results.Ok(APIResponse<IaTextResult>.Success(new IaTextResult
                {
                    Descripcion = MejorarDescripcionLocal(req.Descripcion, req.Vehiculo)
                }));
            }
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<IaTextResult>>();

        return ia;
    }

    /// <summary>Mejora heurística simple (sin IA externa) para cuando no hay API Key configurada.</summary>
    private static string MejorarDescripcionLocal(string descripcion, string? vehiculo)
    {
        var texto = descripcion.Trim();
        if (texto.Length == 0) return texto;

        // Capitalizar primera letra
        texto = char.ToUpperInvariant(texto[0]) + texto[1..];

        // Asegurar punto final
        if (!texto.EndsWith('.') && !texto.EndsWith('!') && !texto.EndsWith('?'))
            texto += ".";

        // Normalizar espacios múltiples
        while (texto.Contains("  ")) texto = texto.Replace("  ", " ");

        var prefijo = string.IsNullOrWhiteSpace(vehiculo) ? "" : $"[{vehiculo}] ";
        return $"{prefijo}{texto}";
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record MejorarDescripcionOrdenRequest(string Descripcion, string? Vehiculo);

public class IaTextResult
{
    public string Descripcion { get; set; } = "";
}
