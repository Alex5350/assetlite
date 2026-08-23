# ADR 0004: A zero-dependency domain with DomainResult; ErrorOr at the boundary

**Status:** Accepted

## Context

The previous portfolio backends (LedgerLite, LeaveLite) referenced the ErrorOr package from the
domain layer. This project tests a stricter stance: how pure can the domain get, and does it pay?

## Decision

- `AssetLite.Domain` has **zero NuGet references**. Expected failures are values: a
  `DomainError(Code, Message)` record and `DomainResult`/`DomainResult<T>` readonly structs.
- `AssetLite.Application` maps `DomainError` to `ErrorOr` via C# 14 extension blocks: codes
  ending in `NotFound` map to `Error.NotFound` (404), everything else to `Error.Conflict`
  (409). Controllers translate to RFC 9457 ProblemDetails carrying the code as the title.

## Consequences

- The domain compiles and tests with nothing installed; error codes are a stable, greppable
  contract shared by API responses and both test suites.
- One fewer package in the most change-averse layer; the 197 domain tests run against pure data.
- Cost: a small mapping layer at the boundary instead of using ErrorOr everywhere. Worth it:
  the boundary is exactly where transport semantics (404/409) belong.
