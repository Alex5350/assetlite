import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Reports — STUB (built fully by a follow-up agent).
 *
 * Suggested implementation notes:
 * - Inventory summary: `ApiService.getInventorySummary()` (see dashboard.ts for
 *   a working toSignal + retry pattern).
 * - Exportable register: backend exposes an asset register query
 *   (AssetRegisterRowDto) — add an ApiService method for it, e.g.
 *   GET /api/reports/register (verify the exact route with the API agent) and
 *   offer CSV/Excel/PDF download links.
 */
@Component({
  selector: 'app-reports',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div>
      <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Reports</h1>
      <p class="mt-1 text-sm text-slate-500">Inventory summaries and exports</p>
    </div>
    <section class="card mt-6 p-10 text-center">
      <p class="mx-auto max-w-md text-sm text-slate-500">
        The reports workspace (inventory summary views and asset register exports) is under
        construction.
      </p>
      <span class="badge badge-assigned mx-auto mt-4">Coming soon</span>
    </section>
  `,
})
export class Reports {}
