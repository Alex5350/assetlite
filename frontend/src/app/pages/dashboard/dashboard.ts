import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { OfficeSummaryDto } from '../../models';
import { LoadingSkeleton } from '../../shared/loading-skeleton';
import { formatDateTime, formatMoney, formatMoneyCompact } from '../../shared/format';

/** One stat card descriptor. */
interface StatCard {
  readonly label: string;
  readonly value: string;
  readonly hint: string;
  readonly tone: 'neutral' | 'in-stock' | 'assigned' | 'maintenance' | 'retired' | 'value';
}

const STAT_CARD_TONES: Record<StatCard['tone'], string> = {
  neutral: 'text-slate-900',
  'in-stock': 'text-emerald-600',
  assigned: 'text-sky-600',
  maintenance: 'text-amber-600',
  retired: 'text-slate-500',
  value: 'text-primary-700',
};

/**
 * Dashboard: inventory summary (GET /api/reports/summary) with stat cards,
 * a per-office status table and a per-category breakdown.
 */
@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LoadingSkeleton],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private readonly api = inject(ApiService);

  /** Bumped by "Try again" to re-run the summary request. */
  private readonly reloadTick = signal(0);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  private readonly summary$ = toObservable(this.reloadTick).pipe(
    tap(() => {
      this.loading.set(true);
      this.error.set(null);
    }),
    switchMap(() =>
      this.api.getInventorySummary().pipe(
        tap(() => this.loading.set(false)),
        catchError((err) => {
          this.loading.set(false);
          this.error.set(err?.message ?? 'Failed to load inventory summary.');
          return of(null);
        }),
      ),
    ),
  );

  protected readonly summary = toSignal(this.summary$, { initialValue: null });

  /** Status totals summed over the per-office rows. */
  protected readonly totals = computed(() => {
    const summary = this.summary();
    if (!summary) {
      return null;
    }
    const sum = (pick: (office: OfficeSummaryDto) => number) =>
      summary.offices.reduce((acc, office) => acc + pick(office), 0);
    return {
      assigned: sum((o) => o.assignedCount),
      inStock: sum((o) => o.inStockCount),
      maintenance: sum((o) => o.maintenanceCount),
      retired: sum((o) => o.retiredCount),
    };
  });

  protected readonly statCards = computed<StatCard[]>(() => {
    const summary = this.summary();
    const totals = this.totals();
    if (!summary || !totals) {
      return [];
    }
    return [
      { label: 'Total assets', value: summary.totalAssets.toLocaleString(), hint: 'registered lifecycle-wide', tone: 'neutral' },
      { label: 'Assigned', value: totals.assigned.toLocaleString(), hint: 'checked out to people', tone: 'assigned' },
      { label: 'In stock', value: totals.inStock.toLocaleString(), hint: 'available for assignment', tone: 'in-stock' },
      { label: 'Maintenance', value: totals.maintenance.toLocaleString(), hint: 'under repair', tone: 'maintenance' },
      { label: 'Retired', value: totals.retired.toLocaleString(), hint: 'withdrawn from use', tone: 'retired' },
      { label: 'Purchase value', value: formatMoneyCompact(summary.totalPurchaseValue), hint: 'total cost basis (USD)', tone: 'value' },
    ];
  });

  /** Largest per-category total, for relative share bars. */
  protected readonly maxCategoryTotal = computed(() => {
    const summary = this.summary();
    return summary ? Math.max(1, ...summary.categories.map((c) => c.totalAssets)) : 1;
  });

  protected readonly generatedAt = computed(() => formatDateTime(this.summary()?.generatedAtUtc));

  protected retry(): void {
    this.reloadTick.update((n) => n + 1);
  }

  protected readonly money = formatMoney;
  protected readonly statToneClass = (tone: StatCard['tone']) => STAT_CARD_TONES[tone];
}
