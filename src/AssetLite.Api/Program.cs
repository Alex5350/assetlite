using System.Text.Json.Serialization;
using AssetLite.Api.Dispatching;
using AssetLite.Api.Serialization;
using AssetLite.Application;
using AssetLite.Infrastructure;
using AssetLite.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry (OTLP when orchestrated), service discovery, resilience
// handlers and the default health checks that back the /health and /alive endpoints.
builder.AddServiceDefaults();

// Serilog reads its configuration from the "Serilog" section of appsettings.json.
builder.Services.AddSerilog((sp, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(sp.GetRequiredService<IConfiguration>()));

// Layer composition.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<RequestDispatcher>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Strongly typed ids serialize as raw GUID strings; enums accept names and numbers.
        options.JsonSerializerOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// OpenAPI document at /openapi/v1.json (no bearer security scheme — the API is anonymous).
builder.Services.AddOpenApi();

// CORS for the Angular dev server (the /frontend project, served on port 5070).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5070"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// RFC 9457 problem details for unhandled exceptions (see UseExceptionHandler below).
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    // Scalar API reference UI (reads the document served by MapOpenApi).
    app.MapScalarApiReference("/scalar/v1");
}

app.MapControllers();

// OpenAPI document is served in every environment; only the interactive UI is Development-only.
app.MapOpenApi();

// /health (readiness) and /alive (liveness) from the Aspire service defaults.
app.MapDefaultEndpoints();

// Apply migrations before accepting traffic; in Development also seed the demo catalog
// (offices, categories, 45 assets) so the API and dashboard start with data.
await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    if (app.Environment.IsDevelopment())
    {
        await initializer.InitializeDevelopmentAsync();
    }
    else
    {
        await initializer.InitializeAsync();
    }
}

app.Run();

/// <summary>Exposes the implicitly generated <c>Program</c> class for WebApplicationFactory-based tests.</summary>
public partial class Program;
