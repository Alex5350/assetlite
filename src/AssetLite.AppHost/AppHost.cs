// AssetLite orchestration entry point. Run with: dotnet run --project src/AssetLite.AppHost
// (the Aspire dashboard URL is printed to the console on startup).
var builder = DistributedApplication.CreateBuilder(args);

// The AssetLite Web API (controllers). Fixed http port 5060 so the SPA dev proxy, CORS setup and
// the verification scripts can rely on a stable URL.
var api = builder.AddProject<Projects.AssetLite_Api>("api")
    .WithHttpEndpoint(port: 5060)
    .WithExternalHttpEndpoints();

// The Angular 21 SPA runs via `npm start` in /frontend (ng serve --port 5070) rather than as an
// Aspire JavaScript-app resource. We tried AddJavaScriptApp with npm run, and with node executing
// the Angular CLI directly: in both cases the child process exits moments after start under the
// process monitor, and the HTTP endpoint annotation is rejected ("service-producer annotation is
// invalid") — a defect in Aspire 13.5.2's JS resources on macOS with nvm-managed Node, documented
// with evidence in docs/adr/0002-aspire-orchestration.md. Aspire still orchestrates the API end to
// end (dashboard, telemetry, health); revisit the JS resource when Aspire ships a fix.

builder.Build().Run();
