import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Params, Router, provideRouter } from '@angular/router';
import { Observable, Subject, of } from 'rxjs';
import { vi } from 'vitest';
import { ApiService } from '../../core/api.service';
import { AssetListItemDto, CategoryDto, OfficeTreeNodeDto, PagedResult } from '../../models';
import { AssetsList } from './assets-list';

const TREE: OfficeTreeNodeDto = {
  id: 'office-hq',
  name: 'Headquarters',
  code: 'HQ',
  parentOfficeId: null,
  children: [],
};

const CATEGORIES: CategoryDto[] = [
  { id: 'cat-laptops', name: 'Laptops', description: null, expectedLifespanMonths: 36 },
];

const PAGE: PagedResult<AssetListItemDto> = {
  items: [
    {
      id: 'asset-1',
      tag: 'AST-000001',
      name: 'MacBook Pro 14',
      status: 'Assigned',
      condition: 'Good',
      officeId: 'office-hq',
      officeName: 'Headquarters',
      categoryId: 'cat-laptops',
      categoryName: 'Laptops',
      currentAssigneeName: 'Jordan Reyes',
      purchaseDate: '2026-01-15',
      purchaseCostAmount: 2399,
    },
    {
      id: 'asset-2',
      tag: 'AST-000002',
      name: 'Dell UltraSharp 27',
      status: 'InStock',
      condition: 'New',
      officeId: 'office-hq',
      officeName: 'Headquarters',
      categoryId: 'cat-laptops',
      categoryName: 'Laptops',
      currentAssigneeName: null,
      purchaseDate: null,
      purchaseCostAmount: null,
    },
  ],
  total: 21,
  page: 1,
  pageSize: 20,
};

/** Types a value into an input the way a user would (event → signal → CD). */
function setInput(input: HTMLInputElement, value: string): void {
  input.value = value;
  input.dispatchEvent(new Event('input', { bubbles: true }));
}

/**
 * Fires the form's ngSubmit binding. The components bind (ngSubmit) without
 * FormsModule, so Angular listens for a DOM event literally named "ngSubmit" —
 * dispatching that event is what actually reaches the handler.
 */
function submitForm(form: HTMLFormElement): void {
  form.dispatchEvent(new Event('ngSubmit', { bubbles: true }));
}

describe('AssetsList', () => {
  /** Configures the TestBed with a fake ApiService, a controllable query-param
   * stream and a spied Router; creates the fixture and loads the first page. */
  function createFixture(
    overrides: { searchResult$?: Observable<PagedResult<AssetListItemDto>> } = {},
  ) {
    const queryParams$ = new Subject<Params>();
    const api = {
      searchAssets: vi.fn(() => overrides.searchResult$ ?? of(PAGE)),
      getOfficeTree: vi.fn(() => of(TREE)),
      getCategories: vi.fn(() => of(CATEGORIES)),
    };
    TestBed.configureTestingModule({
      imports: [AssetsList],
      providers: [
        provideRouter([]),
        { provide: ApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { queryParams: queryParams$.asObservable() } },
      ],
    });
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');
    const fixture = TestBed.createComponent(AssetsList);
    const element = fixture.nativeElement as HTMLElement;
    return { fixture, element, api, navigateSpy, queryParams$ };
  }

  it('renders table rows from the paged result', async () => {
    const { fixture, element, queryParams$ } = createFixture();
    queryParams$.next({});
    await fixture.whenStable();

    const rows = element.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('AST-000001');
    expect(rows[0].textContent).toContain('MacBook Pro 14');
    expect(rows[0].textContent).toContain('Jordan Reyes');
    expect(rows[1].textContent).toContain('AST-000002');
    // Missing assignee renders an em dash rather than a blank cell.
    expect(rows[1].textContent).toContain('—');
  });

  it('passes parsed query params (search, includeDescendants, status, page) to the API', async () => {
    const { fixture, api, queryParams$ } = createFixture();
    queryParams$.next({
      search: 'thinkpad',
      officeId: 'office-hq',
      includeDescendants: 'true',
      status: 'Maintenance',
      page: '3',
    });
    await fixture.whenStable();

    expect(api.searchAssets).toHaveBeenCalledWith({
      search: 'thinkpad',
      officeId: 'office-hq',
      includeDescendants: true,
      status: 'Maintenance',
      page: 3,
      pageSize: 20,
    });
  });

  it('applies filter form values to the URL via the router', async () => {
    const { fixture, element, navigateSpy, queryParams$ } = createFixture();
    queryParams$.next({});
    await fixture.whenStable();

    setInput(element.querySelector<HTMLInputElement>('#asset-search')!, 'macbook');
    const status = element.querySelector<HTMLSelectElement>('#asset-status')!;
    status.value = 'Assigned';
    status.dispatchEvent(new Event('change'));

    // Selecting a status applies immediately; the search field rides along.
    expect(navigateSpy).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ search: 'macbook', status: 'Assigned', page: null }),
        queryParamsHandling: 'merge',
      }),
    );

    // The Apply button submits the whole form the same way.
    submitForm(element.querySelector('form[role="search"]')!);
    expect(navigateSpy).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ search: 'macbook', status: 'Assigned' }),
      }),
    );
  });

  it('flips the page from the pagination controls and reloads with the new page', async () => {
    const { fixture, element, api, navigateSpy, queryParams$ } = createFixture();
    queryParams$.next({});
    await fixture.whenStable();

    const nav = element.querySelector('nav[aria-label="Pagination"]');
    expect(nav?.textContent).toContain('Page 1 of 2'); // 21 items, page size 20

    const pageTwo = Array.from(nav?.querySelectorAll('button') ?? []).find(
      (button) => (button.textContent ?? '').trim() === '2',
    );
    pageTwo?.dispatchEvent(new Event('click'));

    // The click navigates to ?page=2 …
    expect(navigateSpy).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({ queryParams: expect.objectContaining({ page: '2' }) }),
    );
    // … and once the URL updates, the API is re-called with the new page.
    queryParams$.next({ page: '2' });
    await fixture.whenStable();
    expect(api.searchAssets).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 2, pageSize: 20 }),
    );
    expect(api.searchAssets).toHaveBeenCalledTimes(2);
  });

  it('renders the empty state when the page has no items', async () => {
    const { fixture, element, queryParams$ } = createFixture({
      searchResult$: of({ items: [], total: 0, page: 1, pageSize: 20 }),
    });
    queryParams$.next({});
    await fixture.whenStable();

    expect(element.querySelector('app-empty-state')).toBeTruthy();
    expect(element.textContent).toContain('No assets found');
  });

  it('shows the table skeleton while the first page is loading', async () => {
    const { fixture, element, queryParams$ } = createFixture({ searchResult$: new Subject() });
    queryParams$.next({});
    await fixture.whenStable();

    expect(element.querySelector('[aria-busy="true"]')).toBeTruthy();
    expect(element.querySelector('app-table-skeleton')).toBeTruthy();
    expect(element.querySelector('app-empty-state')).toBeNull();
  });
});
