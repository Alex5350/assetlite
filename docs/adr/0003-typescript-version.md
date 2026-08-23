# ADR 0003: TypeScript 5.9 today; TypeScript 7 evaluated and deferred

**Status:** Accepted

## Context

TypeScript 7 (the Go-native compiler, "tsgo") went GA on 2026-07-08 with ~10x faster type
checking. The frontend should use current, vetted tooling, but only if the framework supports it.

## Decision

Use the TypeScript version Angular 21 installs and supports: **TypeScript 5.9** (`~5.9.2`).
Do not adopt TypeScript 7 yet.

## Evidence

- Angular's own roadmap states it has "perhaps one of the deepest integrations with the
  TypeScript compiler, which will require bigger architectural changes to support new tsgo-based
  workflows", a "bridge to TypeScript 7.0" effort is underway but not shipped.
- Angular 21's supported range is TypeScript 6.x/5.9 depending on minor; the CLI pins `~5.9.2`.
- Angular 22 exists but requires Node ≥ 22.22, above the toolchain this repository standardizes
  on (22.15), pinned to Angular 21 deliberately, another documented constraint.

## Consequences

- `ng build`/`ng test` run on fully supported compiler versions: no experimental bridge flags
  in a portfolio codebase.
- When Angular ships official tsgo support, upgrading is a `package.json` bump plus whatever the
  migration guide requires; this ADR is the record that the choice was evaluated, not missed.
