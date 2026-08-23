import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { InventorySummaryDto } from '../../models';
import { Dashboard } from './dashboard';

const SUMMARY: InventorySummaryDto = {
  generatedAtUtc: '2026-08-22T10:00:00Z',
  totalAssets: 120,
  totalPurchaseValue: 25400.5,
  offices: [
    {
      officeId: 'office-hq',
      officeName: 'Headquarters',
      officeCode: 'HQ',
      totalAssets: 100,
      inStockCount: 40,
      assignedCount: 50,
      maintenanceCount: 5,
      retiredCount: 5,
      disposedCount: 0,
      totalPurchaseValue: 20000,
    },
    {
      officeId: 'office-berlin',
      officeName: 'Berlin',
      officeCode: 'BER',
      totalAssets: 20,
      inStockCount: 10,
      assignedCount: 8,
      maintenanceCount: 2,
      retiredCount: 0,
      disposedCount: 0,
      totalPurchaseValue: 5400.5,
    },
  ],
  categories: [
    {
      categoryId: 'cat-laptops',
      categoryName: 'Laptops',
      totalAssets: 90,
      inStockCount: 30,
      assignedCount: 55,
      maintenanceCount: 5,
      retiredCount: 0,
      disposedCount: 0,
      totalPurchaseValue: 20000,
    },
  ],
};

describe('Dashboard', () => {
  /** Configures the TestBed with a fake ApiService and creates the fixture. */
  function createFixture(summary$: Observable<InventorySummaryDto>) {
    TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [{ provide: ApiService, useValue: { getInventorySummary: () => summary$ } }],
    });
    return TestBed.createComponent(Dashboard);
  }

  it('creates the component', () => {
    const fixture = createFixture(of(SUMMARY));
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders stat cards computed from the inventory summary', async () => {
    const fixture = createFixture(of(SUMMARY));
    await fixture.whenStable();
    const stats = (fixture.nativeElement as HTMLElement).querySelector('section');

    // Grand total comes from totalAssets…
    expect(stats?.textContent).toContain('120');
    // …status totals are summed across the per-office rows:
    expect(stats?.textContent).toContain('58'); // assigned 50 + 8
    expect(stats?.textContent).toContain('50'); // in stock 40 + 10
    expect(stats?.textContent).toContain('7'); // maintenance 5 + 2
    expect(stats?.textContent).toContain('5'); // retired
  });

  it('renders the per-office table with office names and codes', async () => {
    const fixture = createFixture(of(SUMMARY));
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    const rows = compiled.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(compiled.textContent).toContain('Headquarters');
    expect(compiled.textContent).toContain('Berlin');
    expect(compiled.textContent).toContain('HQ');
  });

  it('renders the per-category breakdown', async () => {
    const fixture = createFixture(of(SUMMARY));
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Laptops');
    expect(compiled.textContent).toContain('90');
  });

  it('shows the error panel with a retry button when the API fails', async () => {
    const fixture = createFixture(throwError(() => new Error('API down')));
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[role="alert"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Could not load the dashboard');
    const retry = compiled.querySelector<HTMLButtonElement>('[role="alert"] button');
    expect(retry?.textContent).toContain('Try again');
  });
});
