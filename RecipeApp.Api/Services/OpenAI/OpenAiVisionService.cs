using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RecipeApp.Api.Data;
using RecipeApp.Api.Data.Entities;
using RecipeApp.Api.Options;

namespace RecipeApp.Api.Services.OpenAI;

public class OpenAiVisionService(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAiOptions> options,
    AppDbContext db,
    ILogger<OpenAiVisionService> logger) : IOpenAiVisionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private const string DetectionPrompt = """
        Identify visible food ingredients from these 1-2 images.
        The images may show a fridge, pantry, kitchen counter, or loose ingredients.
        Return JSON only. No markdown, no explanation.
        Do not estimate exact grams. Do not calculate macros.
        Use common ingredient names. Ignore non-food objects.
        If an item is uncertain, include a lower confidence score.
        If a package is visible but not identifiable, describe it generally (e.g. "unknown sauce bottle") with low confidence.
        Include category: protein, dairy, vegetable, fruit, grain, condiment, drink, snack, other.
        Set needsConfirmation to true for all uncertain items and for anything where quantity matters.
        Return this exact JSON structure:
        {
          "items": [
            {
              "name": "eggs",
              "normalizedName": "eggs",
              "category": "protein",
              "confidence": 0.93,
              "quantityVisible": "unknown",
              "needsConfirmation": true,
              "sourceImageIndex": 0
            }
          ],
          "warnings": [
            "Quantities are visual guesses and should be confirmed."
          ]
        }
        """;

    public async Task<OpenAiVisionResult> DetectIngredientsAsync(IFormFileCollection images)
    {
        var client = httpClientFactory.CreateClient("openai");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int? statusCode = null;
        string? errorMessage = null;

        try
        {
            var contentParts = new List<object>
            {
                new { type = "text", text = DetectionPrompt }
            };

            foreach (var image in images)
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                var mimeType = image.ContentType ?? "image/jpeg";

                contentParts.Add(new
                {
                    type = "image_url",
                    image_url = new { url = $"data:{mimeType};base64,{base64}" }
                });
            }

            var requestBody = new
            {
                model = options.Value.VisionModel,
                messages = new[]
                {
                    new { role = "user", content = contentParts }
                },
                max_tokens = options.Value.MaxTokens,
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/v1/chat/completions", content);
            statusCode = (int)response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                errorMessage = $"OpenAI returned {statusCode}: {responseBody}";
                throw new Exception(errorMessage);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? throw new Exception("Empty response from OpenAI");

            return ParseVisionResponse(messageContent);
        }
        catch (Exception ex)
        {
            errorMessage ??= ex.Message;
            logger.LogError(ex, "OpenAI vision call failed");
            throw;
        }
        finally
        {
            sw.Stop();
            db.ExternalApiCallLogs.Add(new ExternalApiCallLog
            {
                Id = Guid.NewGuid(),
                Provider = ApiProvider.OpenAI,
                Endpoint = "/v1/chat/completions",
                RequestType = "DetectIngredients",
                StatusCode = statusCode,
                Success = errorMessage == null,
                ErrorMessage = errorMessage,
                DurationMs = sw.ElapsedMilliseconds,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
    }

    private static OpenAiVisionResult ParseVisionResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var items = new List<OpenAiDetectedItem>();
        if (root.TryGetProperty("items", out var itemsEl))
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                items.Add(new OpenAiDetectedItem(
                    Name: item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    NormalizedName: item.TryGetProperty("normalizedName", out var nn) ? nn.GetString() ?? "" : "",
                    Category: item.TryGetProperty("category", out var cat) ? cat.GetString() ?? "other" : "other",
                    Confidence: item.TryGetProperty("confidence", out var conf) ? (float)conf.GetDouble() : 0.5f,
                    QuantityVisible: item.TryGetProperty("quantityVisible", out var qty) ? qty.GetString() : null,
                    NeedsConfirmation: !item.TryGetProperty("needsConfirmation", out var nc) || nc.GetBoolean(),
                    SourceImageIndex: item.TryGetProperty("sourceImageIndex", out var si) ? si.GetInt32() : null
                ));
            }
        }

        var warnings = new List<string>();
        if (root.TryGetProperty("warnings", out var warningsEl))
        {
            foreach (var w in warningsEl.EnumerateArray())
            {
                var text = w.GetString();
                if (!string.IsNullOrEmpty(text)) warnings.Add(text);
            }
        }

        if (warnings.Count == 0)
            warnings.Add("Quantities are visual estimates and should be confirmed.");

        return new OpenAiVisionResult(items, warnings);
    }
}
