using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using RecipeApp.Api.Data;
using RecipeApp.Api.Middleware;
using RecipeApp.Api.Options;
using RecipeApp.Api.Services;
using RecipeApp.Api.Services.OpenAI;
using RecipeApp.Api.Services.Spoonacular;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7));

// ── Options ───────────────────────────────────────────────────────────────────
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.Section));
builder.Services.Configure<SpoonacularOptions>(builder.Configuration.GetSection(SpoonacularOptions.Section));
builder.Services.Configure<FreeTierOptions>(builder.Configuration.GetSection(FreeTierOptions.Section));
builder.Services.Configure<ImageStorageOptions>(builder.Configuration.GetSection(ImageStorageOptions.Section));

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not configured.");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mysql => mysql.EnableRetryOnFailure(3)));

// ── HTTP Clients ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient("openai", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com");
    client.Timeout = TimeSpan.FromSeconds(60);
    var apiKey = builder.Configuration["OpenAi:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
});

builder.Services.AddHttpClient("spoonacular", client =>
{
    client.BaseAddress = new Uri("https://api.spoonacular.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── External services: real vs mock based on key presence ────────────────────
var isDev = builder.Environment.IsDevelopment();
var openAiKey = builder.Configuration["OpenAi:ApiKey"];
var spoonacularKey = builder.Configuration["Spoonacular:ApiKey"];

if (string.IsNullOrWhiteSpace(openAiKey) && isDev)
{
    builder.Services.AddScoped<IOpenAiVisionService, MockOpenAiVisionService>();
    Log.Warning("OpenAI API key not set — using MockOpenAiVisionService");
}
else
{
    builder.Services.AddScoped<IOpenAiVisionService, OpenAiVisionService>();
}

if (string.IsNullOrWhiteSpace(spoonacularKey) && isDev)
{
    builder.Services.AddScoped<ISpoonacularService, MockSpoonacularService>();
    Log.Warning("Spoonacular API key not set — using MockSpoonacularService");
}
else
{
    builder.Services.AddScoped<ISpoonacularService, SpoonacularService>();
}

// ── Business Services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<DeviceSettingsService>();
builder.Services.AddScoped<FridgeScanService>();
builder.Services.AddScoped<RecipeService>();
builder.Services.AddScoped<UsageLimitService>();
builder.Services.AddScoped<ImageStorageService>();
builder.Services.AddScoped<IngredientNormalizationService>();

// ── Admin cookie auth ─────────────────────────────────────────────────────────
builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", opt =>
    {
        opt.LoginPath = "/admin/login";
        opt.AccessDeniedPath = "/admin/login";
        opt.Cookie.Name = "RecipeAppAdmin";
        opt.Cookie.HttpOnly = true;
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// ── MVC + Razor Pages ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddRouting(opt => opt.LowercaseUrls = true);

// ── OpenAPI (native .NET 10) ──────────────────────────────────────────────────
builder.Services.AddOpenApi(opt =>
{
    opt.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "RecipeApp API";
        doc.Info.Version = "v1";
        doc.Info.Description = "AI-powered ingredient detection and recipe suggestion API.";
        return Task.CompletedTask;
    });
});

// ── CORS for local mobile dev ─────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("MobileDev", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── Pipeline ──────────────────────────────────────────────────────────────────
app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    // OpenAPI spec:   /openapi/v1.json
    // Swagger UI:     /swagger
    // Scalar UI:      /scalar/v1
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "RecipeApp API v1");
        c.RoutePrefix = "swagger";
    });
    app.MapScalarApiReference();
    app.UseCors("MobileDev");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
}));

app.MapControllers();
app.MapRazorPages();

app.MapPost("/admin/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync("AdminCookie");
    ctx.Response.Redirect("/admin/login");
});

app.Run();
