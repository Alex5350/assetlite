import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Offices — STUB (built fully by a follow-up agent).
 *
 * Suggested implementation notes:
 * - Tree: `ApiService.getOfficeTree()` → OfficeTreeNodeDto[] (children ordered
 *   by name); render recursively or with `flattenOfficeTree()` from models.ts
 *   (depth-annotated rows, useful for tables too).
 * - Flat list: `ApiService.getOffices()` → OfficeDto[] (id, name, code, parentOfficeId).
 * - Shared styles for tables/cards/badges live in src/styles.css
 *   (.card, .table, .badge-*, .btn, .input, .select).
 */
@Component({
  selector: 'app-offices',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div>
      <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Offices</h1>
      <p class="mt-1 text-sm text-slate-500">Office hierarchy and locations</p>
    </div>
    <section class="card mt-6 p-10 text-center">
      <p class="mx-auto max-w-md text-sm text-slate-500">
        The office tree, office detail views and office management are under construction.
      </p>
      <span class="badge badge-assigned mx-auto mt-4">Coming soon</span>
    </section>
  `,
})
export class Offices {}
