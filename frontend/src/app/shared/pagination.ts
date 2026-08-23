import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Page-window size on each side of the current page. */
const WINDOW = 2;

/**
 * Server-driven pagination controls: prev/next, a sliding window of numbered
 * page links and a "Page x of y" summary. Emits the chosen 1-based page.
 */
@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (pageCount() > 0) {
      <nav class="flex items-center justify-between gap-4 px-4 py-3" aria-label="Pagination">
        <p class="text-sm text-slate-500 tnums">
          Page <span class="font-medium text-slate-700">{{ page() }}</span> of
          <span class="font-medium text-slate-700">{{ pageCount() }}</span>
          <span class="hidden sm:inline">· {{ total().toLocaleString() }} assets</span>
        </p>
        <div class="flex items-center gap-1">
          <button
            type="button"
            class="btn btn-ghost px-2.5"
            [disabled]="page() <= 1"
            (click)="goTo(page() - 1)"
            aria-label="Previous page"
          >
            ‹ Prev
          </button>
          @for (p of pages(); track p) {
            <button
              type="button"
              class="btn px-2.5 tnums"
              [class.btn-primary]="p === page()"
              [class.btn-ghost]="p !== page()"
              (click)="goTo(p)"
              [attr.aria-current]="p === page() ? 'page' : null"
            >
              {{ p }}
            </button>
          }
          <button
            type="button"
            class="btn btn-ghost px-2.5"
            [disabled]="page() >= pageCount()"
            (click)="goTo(page() + 1)"
            aria-label="Next page"
          >
            Next ›
          </button>
        </div>
      </nav>
    }
  `,
})
export class Pagination {
  /** Current 1-based page number. */
  readonly page = input.required<number>();
  /** Page size used to compute the page count. */
  readonly pageSize = input.required<number>();
  /** Total number of matching items across all pages. */
  readonly total = input.required<number>();
  /** Emits the selected 1-based page number. */
  readonly pageChange = output<number>();

  readonly pageCount = computed(() =>
    this.pageSize() > 0 ? Math.ceil(this.total() / this.pageSize()) : 0,
  );

  /** Sliding window of page numbers around the current page, clamped to bounds. */
  readonly pages = computed<number[]>(() => {
    const count = this.pageCount();
    const current = this.page();
    if (count <= 0) {
      return [];
    }
    const first = Math.max(1, current - WINDOW);
    const last = Math.min(count, current + WINDOW);
    const pages: number[] = [];
    for (let p = first; p <= last; p++) {
      pages.push(p);
    }
    return pages;
  });

  goTo(page: number): void {
    if (page >= 1 && page <= this.pageCount() && page !== this.page()) {
      this.pageChange.emit(page);
    }
  }
}
