import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, catchError, map, merge, of, switchMap, tap, withLatestFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ASSET_STATUSES, AssetSearchFilters, AssetStatus, flattenOfficeTree } from '../../models';
import { StatusBadge } from '../../shared/status-badge';
import { Pagination } from '../../shared/pagination';
import { EmptyState } from '../../shared/empty-state';
import { TableSkeleton } from '../../shared/loading-skeleton';
import { dash, formatDate, formatMoney } from '../../shared/format';

const PAGE_SIZE = 20;

/** Extracts typed search filters from route query params. */
function parseFilters(params: Params): AssetSearchFilters {
  const status = params['status'];
  return {
    search: typeof params['search'] === 'string' && params['search'].trim() ? params['search'].trim() : undefined,
    officeId: typeof params['officeId'] === 'string' && params['officeId'] ? params['officeId'] : undefined,
    includeDescendants: params['includeDescendants'] === 'true',
    categoryId: typeof params['categoryId'] === 'string' && params['categoryId'] ? params['categoryId'] : undefined,
    status: ASSET_STATUSES.includes(status) ? (status as AssetStatus) : undefined,
    page: Math.max(1, Number.parseInt(params['page'] ?? '1', 10) || 1),
    pageSize: PAGE_SIZE,
  };
}

/**
 * Assets list: signal-driven filter form synced to router query params
 * (shareable URLs), server-driven paging, skeleton loading and empty state.
 */
@Component({
  selector: 'app-assets-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, StatusBadge, Pagination, EmptyState, TableSkeleton],
  templateUrl: './assets-list.html',
})
export class AssetsList {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  // --- Filter form state (signals, mirrored from/to the URL) ---------------
  protected readonly searchText = signal('');
  protected readonly officeId = signal('');
  protected readonly includeDescendants = signal(false);
  protected readonly categoryId = signal('');
  protected readonly status = signal<AssetStatus | ''>('');

  private readonly queryParams = toSignal(this.route.queryParams, { initialValue: {} as Params });

  constructor() {
    // Re-sync the form whenever the URL changes (back/forward, global search…).
    effect(() => {
      const params = this.queryParams();
      this.searchText.set(typeof params['search'] === 'string' ? params['search'] : '');
      this.officeId.set(typeof params['officeId'] === 'string' ? params['officeId'] : '');
      this.includeDescendants.set(params['includeDescendants'] === 'true');
      this.categoryId.set(typeof params['categoryId'] === 'string' ? params['categoryId'] : '');
      const status = params['status'];
      this.status.set(ASSET_STATUSES.includes(status) ? (status as AssetStatus) : '');
    });
  }

  // --- Reference data ------------------------------------------------------
  protected readonly officeRoot = toSignal(
    this.api.getOfficeTree().pipe(catchError(() => of(null))),
    { initialValue: null },
  );
  protected readonly flatOffices = computed(() => {
    const root = this.officeRoot();
    return root ? flattenOfficeTree([root]) : [];
  });
  protected readonly categoryOptions = toSignal(
    this.api.getCategories().pipe(catchError(() => of([]))),
    { initialValue: [] },
  );
  protected readonly statuses = ASSET_STATUSES;

  // --- Data stream: filters (from URL) + manual reloads --------------------
  private readonly filters$ = this.route.queryParams.pipe(map(parseFilters));
  private readonly reload$ = new Subject<void>();

  private readonly result$ = merge(
    this.filters$,
    this.reload$.pipe(withLatestFrom(this.filters$), map(([, filters]) => filters)),
  ).pipe(
    switchMap((filters) => {
      this.loading.set(true);
      this.error.set(null);
      return this.api.searchAssets(filters).pipe(
        tap(() => this.loading.set(false)),
        catchError((err) => {
          this.loading.set(false);
          this.error.set(err?.message ?? 'Failed to load assets.');
          return of(null);
        }),
      );
    }),
  );

  protected readonly result = toSignal(this.result$, { initialValue: null });

  protected readonly hasActiveFilters = computed(() => {
    const params = this.queryParams();
    return Boolean(params['search'] || params['officeId'] || params['categoryId'] || params['status']);
  });

  protected readonly money = formatMoney;
  protected readonly date = formatDate;
  protected readonly dash = dash;

  /** Applies the current form values to the URL (page resets to 1). */
  protected applyFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: this.searchText().trim() || null,
        officeId: this.officeId() || null,
        includeDescendants: this.officeId() && this.includeDescendants() ? 'true' : null,
        categoryId: this.categoryId() || null,
        status: this.status() || null,
        page: null,
      },
      queryParamsHandling: 'merge',
    });
  }

  /** Navigates to a specific page, keeping all filters. */
  protected goToPage(page: number): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: page > 1 ? String(page) : null },
      queryParamsHandling: 'merge',
    });
  }

  protected clearFilters(): void {
    this.searchText.set('');
    this.officeId.set('');
    this.includeDescendants.set(false);
    this.categoryId.set('');
    this.status.set('');
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  protected retry(): void {
    this.reload$.next();
  }
}
