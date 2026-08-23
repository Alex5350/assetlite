import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiService } from '../../core/api.service';
import { InventorySummaryDto } from '../../models';
import { Reports } from './reports';

const EXCEL_URL = 'http://localhost:5060/api/reports/register/excel';
const PDF_URL = 'http://localhost:5060/api/reports/register/pdf';

/** Two offices / two categories with distinct counts so totals are checkable. */
const SUMMARY: InventorySummaryDto = {
  generatedAtUtc: '2026-08-22T10:00:00Z',
  totalAssets: 41,
  totalPurchaseValue: 10500.5,
  offices: [
    {
      officeId: 'office-hq',
      officeName: 'Headquarters',
      officeCode: 'HQ',
      totalAssets: 26,
      inStockCount: 11,
      assignedCount: 12,
      maintenanceCount: 3,
      retiredCount: 2,
      disposedCount: 1,
      totalPurchaseValue: 9000,
    },
    {
      officeId: 'office-berlin',
      officeName: 'Berlin',
      officeCode: 'BER',
      totalAssets: 15,
      inStockCount: 6,
      assignedCount: 7,
      maintenanceCount: 4,
      retiredCount: 0,
      disposedCount: 0,
      totalPurchaseValue: 1500.5,
    },
  ],
  categories: [
    {
      categoryId: 'cat-laptops',
      categoryName: 'Laptops',
      totalAssets: 30,
      inStockCount: 12,
      assignedCount: 14,
      maintenanceCount: 2,
      retiredCount: 2,
      disposedCount: 0,
      totalPurchaseValue: 9000,
    },
    {
      categoryId: 'cat-monitors',
      categoryName: 'Monitors',
      totalAssets: 11,
      inStockCount: 5,
      assignedCount: 5,
      maintenanceCount: 1,
      retiredCount: 0,
      disposedCount: 0,
      totalPurchaseValue: 1500.5,
    },
  ],
};

describe('Reports', () => {
  /** Configures the TestBed with a fake ApiService and creates the fixture. */
  function createFixture() {
    const api = {
      getInventorySummary: vi.fn(() => of(SUMMARY)),
      registerExcelUrl: vi.fn(() => EXCEL_URL),
      registerPdfUrl: vi.fn(() => PDF_URL),
    };
    TestBed.configureTestingModule({
      imports: [Reports],
      providers: [{ provide: ApiService, useValue: api }],
    });
    const fixture = TestBed.createComponent(Reports);
    const element = fixture.nativeElement as HTMLElement;
    return { fixture, element, api };
  }

  it('renders the stat cards with totals computed from the summary', async () => {
    const { fixture, element } = createFixture();
    await fixture.whenStable();

    const cards = Array.from(element.querySelectorAll('[data-testid="summary-stats"] > div'));
    expect(cards.length).toBe(7);

    // Status totals are summed across the per-office rows.
    const cardText = (label: string) => {
      const card = cards.find((c) => c.textContent?.includes(label));
      expect(card, `missing stat card "${label}"`).toBeTruthy();
      return card!.textContent ?? '';
    };
    expect(cardText('Total assets')).toContain('41');
    expect(cardText('Assigned')).toContain('19'); // 12 + 7
    expect(cardText('In stock')).toContain('17'); // 11 + 6
    expect(cardText('Maintenance')).toContain('7'); // 3 + 4
    expect(cardText('Retired')).toContain('2');
    expect(cardText('Disposed')).toContain('1');
  });

  it('renders one row per office and per category', async () => {
    const { fixture, element } = createFixture();
    await fixture.whenStable();

    const officeRows = element.querySelectorAll('[data-testid="offices-body"] tr');
    expect(officeRows.length).toBe(2);
    expect(element.textContent).toContain('Headquarters');
    expect(element.textContent).toContain('HQ');
    expect(element.textContent).toContain('Berlin');

    const categoryRows = element.querySelectorAll('[data-testid="categories-body"] tr');
    expect(categoryRows.length).toBe(2);
    expect(element.textContent).toContain('Laptops');
    expect(element.textContent).toContain('Monitors');
  });

  it('points the export anchors at the API register URLs', async () => {
    const { fixture, element, api } = createFixture();
    await fixture.whenStable();

    const excel = element.querySelector<HTMLAnchorElement>('[data-testid="export-excel"]');
    const pdf = element.querySelector<HTMLAnchorElement>('[data-testid="export-pdf"]');
    expect(excel?.getAttribute('href')).toBe(EXCEL_URL);
    expect(pdf?.getAttribute('href')).toBe(PDF_URL);
    expect(api.registerExcelUrl).toHaveBeenCalled();
    expect(api.registerPdfUrl).toHaveBeenCalled();
  });
});
