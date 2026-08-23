import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Asset detail — STUB (built fully by a follow-up agent).
 *
 * Route: /assets/:tag. The `tag` route param is already bound below thanks to
 * `withComponentInputBinding()` in app.config.ts.
 *
 * Suggested implementation notes:
 * - Fetch via `ApiService.getAssetByTag(tag())` → AssetDetailDto
 *   (compose with `toSignal`, mirror the retry pattern in dashboard.ts).
 * - Render detail card (name, tag mono, manufacturer/model, serial number,
 *   status via <app-status-badge [status]="…">, condition, purchase info) plus
 *   the assignment history table (assignments[], newest first).
 * - Label printing: `ApiService.getAssetLabel(tag())` → AssetLabel with
 *   barcodeSvg / qrSvg (self-contained SVGs; render with DomSanitizer.bypassSecurityTrustHtml).
 * - Lifecycle actions (assign/return/transfer/maintenance/retire/dispose) are
 *   backend commands — add POST methods to ApiService when wiring them up.
 */
@Component({
  selector: 'app-asset-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="card mt-6 p-10 text-center">
      <p class="text-xs font-medium tracking-widest text-slate-400 uppercase">Asset</p>
      <h1 class="mt-2 font-mono text-2xl font-semibold text-slate-900">{{ tag() }}</h1>
      <p class="mx-auto mt-3 max-w-md text-sm text-slate-500">
        The detailed asset view (specs, assignment history, lifecycle actions and label printing)
        is under construction.
      </p>
      <span class="badge badge-assigned mx-auto mt-4">Coming soon</span>
    </section>
  `,
})
export class AssetDetail {
  /** Canonical asset tag from the route (withComponentInputBinding). */
  readonly tag = input.required<string>();
}
