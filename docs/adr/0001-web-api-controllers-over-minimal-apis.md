# ADR-0001: ASP.NET Core controllers over minimal APIs for the Web API host

- Status: Accepted
- Date: 2026-08-22
- Scope: `src/AssetLite.Api`

## Context

AssetLite needs an HTTP host exposing the Application layer's commands and queries
(`ICommandHandler` / `IQueryHandler`). .NET 10 offers minimal APIs and controllers; both are
first-class. The API surface is CRUD-plus-lifecycle over a fixed resource model (offices,
categories, assets, reports), addressed by a stable route contract consumed by the upcoming
Angular SPA (`/frontend`, port 5070).

## Decision

Use **controllers** (`AddControllers`, `[ApiController]`, attribute routing under `api/`).

## Rationale

- The route contract is the project's public surface; centralizing it in four controllers
  (Offices, Categories, Assets, Reports) makes the route table reviewable at a glance, and the
  Application layer stays HTTP-agnostic either way.
- `ProducesResponseType` metadata plus a shared `ApiControllerBase` gives one uniform
  ErrorOr → RFC 9457 problem-details mapping (400/404/409) reused by every action, instead of
  per-endpoint `.AddEndpointFilter` wiring.
- ControllerBase helpers (`CreatedAtAction`, `ValidationProblem`, `File`) map directly onto the
  required 201/400/file-download behaviors.
- Versioning, convention-based `Produces`, and test tooling (WebApplicationFactory with
  `partial Program`) are mature on controllers for this API shape.

Minimal APIs would work equally well for this size; this is a deliberate preference for a
contract-first, metadata-rich host rather than a performance-driven choice.

## Consequences

- `Program.cs` calls `AddControllers` + `MapControllers`; OpenAPI (`AddOpenApi`/`MapOpenApi`,
  Microsoft.AspNetCore.OpenApi 10) is generated from controller metadata and browsable via
  Scalar at `/scalar/v1` in Development.
- All enum query binding keeps MVC defaults (numbers or names); JSON bodies accept both via
  `JsonStringEnumConverter`, and strongly typed ids serialize as raw GUID strings via a custom
  converter factory.
