# ADR 0002 - Aspire orchestrates the backend; the SPA dev server runs via npm

**Status:** Accepted (partial adoption, with evidence)

## Context

AssetLite is a full-stack app: an ASP.NET Core Web API and an Angular 21 SPA. Developers need one
obvious way to run both locally, and the portfolio should demonstrate current .NET orchestration
tooling. .NET Aspire 13.5.2 (AppHost + ServiceDefaults) was evaluated for orchestrating *both*
processes.

## Decision

- The **API is a first-class Aspire resource**: `AddProject` with a fixed 5060 endpoint, external
  HTTP endpoints, OpenTelemetry/service-discovery/resilience via ServiceDefaults, and the Aspire
  dashboard (logs, traces, health) - verified working end to end.
- The **Angular dev server is NOT an Aspire JavaScript-app resource**. It runs with `npm start`
  (`ng serve --port 5070`) in `/frontend`. The README documents both steps.

## Evidence - why the JS resource was rejected

We attempted `builder.AddJavaScriptApp("frontend", "../../frontend").WithRunScript("start")`
three ways: the default npm lifecycle, `npm run start` explicitly, and bypassing npm entirely
(`node node_modules/@angular/cli/bin/ng.js serve --port 5070`). All three fail identically under
Aspire 13.5.2 on macOS with nvm-managed Node 22:

1. The child process exits ~1.5 s after start - `Monitor process exited, shutting down` -
   although the identical command succeeds standalone (verified: bundle generated in 0.87 s,
   serving 200 on 5070).
2. The endpoint annotation is rejected: `Could not create Endpoint object(s) … information about
   the port to expose the service is missing; service-producer annotation is invalid`.

Since the failure reproduces with a plain node process and not with any project-specific code,
this is an upstream integration defect, not a configuration error in this repository.

## Consequences

- One command (`dotnet run --project src/AssetLite.AppHost`) still brings up the API **plus the
  Aspire dashboard** with full telemetry; `npm start` brings up the SPA against the fixed API
  port. Two terminals, zero manual configuration.
- Swapping the fixed 5070/5060 ports for Aspire-managed dynamic ports remains future work gated
  on the upstream fix; the fixed ports also keep CORS and QR-label base URLs simple.
- Revisit when Aspire ships a working JS-app resource on macOS/nvm: re-enable the commented
  block in `AppHost.cs`, delete this ADR's rejection section, done.
