// AssetLite orchestration entry point. Run with: dotnet run --project src/AssetLite.AppHost
// (the Aspire dashboard URL is printed to the console on startup).
var builder = DistributedApplication.CreateBuilder(args);

// The AssetLite Web API (controllers). Fixed http port 5060 so the SPA dev proxy, CORS setup and
// the verification scripts can rely on a stable URL.
var api = builder.AddProject<Projects.AssetLite_Api>("api")
    .WithHttpEndpoint(port: 5060)
    .WithExternalHttpEndpoints();

// The Angular 21 SPA as an Aspire JavaScript-app resource. Two things make it work (both were
// missing in the first attempt, which failed with "Monitor process exited" and an invalid
// service-producer annotation — see docs/adr/0002-aspire-orchestration.md):
//   1. the endpoint must be declared on the resource, with `env: "PORT"` so Aspire injects the
//      port into the npm script's environment;
//   2. the `aspire` npm script must actually honor $PORT (ng serve does not read PORT on its
//      own) — it falls back to 5070 so the same script works standalone.
builder.AddJavaScriptApp("frontend", "../../frontend")
    .WithRunScript("aspire")
    .WithHttpEndpoint(port: 5070, env: "PORT")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
