// AssetLite orchestration entry point. Run with: dotnet run --project src/AssetLite.AppHost
// (the Aspire dashboard URL is printed to the console on startup).
var builder = DistributedApplication.CreateBuilder(args);

// The AssetLite Web API (controllers). Fixed http port 5060 so the SPA dev proxy, CORS setup and
// the verification scripts can rely on a stable URL.
var api = builder.AddProject<Projects.AssetLite_Api>("api")
    .WithHttpEndpoint(port: 5060)
    .WithExternalHttpEndpoints();

// TODO(frontend): the Angular SPA is scaffolded by a parallel work stream under /frontend and is
// NOT referenced yet. When the folder exists, wire it up as follows:
//
//   1) Add the Aspire.Hosting.JavaScript package to this project (dotnet add package Aspire.Hosting.JavaScript).
//      (Aspire 13 renamed Aspire.Hosting.NodeJs and replaced AddNpmApp with AddJavaScriptApp.)
//   2) Ensure /frontend/package.json has a "start" script serving on port 5070, e.g. "ng serve --port 5070".
//   3) Uncomment:
//
//   var frontend = builder.AddJavaScriptApp("frontend", "../frontend")
//       .WithRunScript("start")
//       .WithHttpEndpoint(port: 5070)
//       .WithExternalHttpEndpoints()
//       .WithReference(api);
//
// Port 5070 matches the CORS origin configured in AssetLite.Api (http://localhost:5070). The API
// already allows that origin, so no API-side change is needed when this lands.

builder.Build().Run();
