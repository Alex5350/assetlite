# ADR 0002: Aspire orchestrates the whole app: API and Angular dev server

**Status:** Accepted (full adoption, reversing the earlier partial-adoption decision on new evidence)

## Context

AssetLite is a full-stack app: an ASP.NET Core Web API and an Angular 21 SPA. Developers need one
obvious way to run both locally, and the portfolio should demonstrate current .NET orchestration
tooling. .NET Aspire 13.5.2 (AppHost + ServiceDefaults) was evaluated for orchestrating *both*
processes.

## First attempt and the wrong conclusion

`builder.AddJavaScriptApp("frontend", "../../frontend").WithRunScript("start")` failed on
macOS with nvm-managed Node 22, three ways: the default npm lifecycle, `npm run start`, and
launching the Angular CLI through `node` directly. All three failed identically:

1. The child process exited ~1.5 s after start (`Monitor process exited, shutting down`),
   although the identical command succeeded standalone.
2. The endpoint annotation was rejected: `Could not create Endpoint object(s) … information
   about the port to expose the service is missing; service-producer annotation is invalid`.

Because the failure reproduced with a plain node process, we concluded it was an upstream
defect in Aspire's JS resources on macOS/nvm and shipped partial adoption (API under Aspire,
SPA via npm). **That conclusion was wrong.**

## Reopening the case

Two hypotheses were never tested:

- **H1: the endpoint was never declared.** The first wiring called no `WithHttpEndpoint`, yet
  the error text is literally about missing port information for the service annotation.
- **H2: the port contract was never honored.** Aspire's JavaScript resources inject the port
  through the `PORT` environment variable (the convention Vite/Next-style dev servers read).
  `ng serve` does **not** read `PORT`: only `--port` or `angular.json`. The old `start` script
  hardcoded `--port 5070`, so the server bound a different port than the one Aspire declared
  and monitored. The endpoint never became healthy; the monitor shut the resource down. The
  "defect" was a port handshake miss.

An isolated repro (AppHost hosting only the JS resource, same machine, same Aspire 13.5.2,
same Node 22.15.0) tested both at once:

```csharp
builder.AddJavaScriptApp("frontend", ".../frontend")
    .WithRunScript("aspire")                    // "ng serve --port ${PORT:-5070}"
    .WithHttpEndpoint(port: 5071, env: "PORT");
```

Result: the Angular dev server came up under the process monitor and stayed up; HTTP 200 for
the entire observation window, no monitor exits, no annotation errors.

## Decision

- The **API** stays a first-class Aspire resource: `AddProject` with a fixed 5060 endpoint,
  external HTTP endpoints, OpenTelemetry/service discovery/resilience via ServiceDefaults.
- The **SPA is now an Aspire JavaScript-app resource** wired exactly like the repro:
  `WithRunScript("aspire")`, `WithHttpEndpoint(port: 5070, env: "PORT")`, `WithReference(api)`
  and `WaitFor(api)`. The `aspire` npm script is `ng serve --port ${PORT:-5070}`; it honors
  the injected `$PORT` (the part `ng serve` will not do itself) and falls back to 5070 so the
  same script works standalone.
- One command (`dotnet run --project src/AssetLite.AppHost`) brings up API, SPA and the
  Aspire dashboard. The standalone flows (`dotnet run --project src/AssetLite.Api`,
  `npm start`) remain as alternatives.

## Consequences

- The failure mode is now understood precisely: a JS resource without a declared endpoint
  produces the annotation error, and a dev server that ignores `PORT` produces the monitor
  exit. Neither is an Aspire bug on macOS; the nvm red herring wasted the first investigation.
- Verified end to end: under the AppHost the SPA serves on 5070, proxies `/api` to 5060, and
  the dashboard tracks both resources.
- Process lesson, kept deliberately in this ADR: the original rejection cited evidence, but
  evidence that a configuration fails is not evidence about *whose* defect it is. An isolated
  minimal repro should precede any "upstream bug" claim.
