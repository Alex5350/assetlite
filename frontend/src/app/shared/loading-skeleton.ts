import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Reusable CSS-shimmer skeleton block. Size it via `blockClass` (e.g. "h-4 w-24"). */
@Component({
  selector: 'app-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="skeleton" [class]="blockClass()" [style.width.%]="widthPct()" aria-hidden="true"></div>`,
  host: { class: 'block' },
})
export class LoadingSkeleton {
  /** Tailwind layout classes for the shimmer block (height/width). */
  readonly blockClass = input<string>('h-4 w-full');
  /** Optional width percentage override (ignored when null). */
  readonly widthPct = input<number | null>(null);
}

/** A skeleton table body matching the standard `.table` row shape. */
@Component({
  selector: 'app-table-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LoadingSkeleton],
  template: `
    @for (row of rowIndices(); track row; let i = $index) {
      <tr>
        @for (col of colIndices(); track col; let j = $index) {
          <td><app-skeleton blockClass="h-4" [widthPct]="widths[(i + j) % widths.length]" /></td>
        }
      </tr>
    }
  `,
  host: { class: 'contents' },
})
export class TableSkeleton {
  /** Number of skeleton rows to render. */
  readonly rows = input<number>(6);
  /** Number of columns per row. */
  readonly cols = input<number>(5);

  protected readonly widths = [85, 60, 92, 45, 73];

  protected readonly rowIndices = computed(() => Array.from({ length: this.rows() }, (_, i) => i));
  protected readonly colIndices = computed(() => Array.from({ length: this.cols() }, (_, i) => i));
}
