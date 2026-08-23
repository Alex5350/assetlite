import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Friendly empty-state panel with an optional action projected via content. */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center gap-2 px-6 py-14 text-center">
      @if (icon()) {
        <div class="grid size-11 place-items-center rounded-full bg-slate-100 text-xl text-slate-400" aria-hidden="true">
          {{ icon() }}
        </div>
      }
      <h3 class="text-sm font-semibold text-slate-700">{{ title() }}</h3>
      @if (message()) {
        <p class="max-w-sm text-sm text-slate-500">{{ message() }}</p>
      }
      <div class="mt-2">
        <ng-content />
      </div>
    </div>
  `,
})
export class EmptyState {
  /** Short headline, e.g. "No assets found". */
  readonly title = input.required<string>();
  /** Optional explanatory message. */
  readonly message = input<string>('');
  /** Optional single-glyph icon (emoji or character). */
  readonly icon = input<string>('📭');
}
