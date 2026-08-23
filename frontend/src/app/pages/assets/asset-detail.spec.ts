import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ApiService } from '../../core/api.service';
import { AssetDetailDto, AssetLabel, OfficeTreeNodeDto } from '../../models';
import { AssetDetail } from './asset-detail';

const TAG = 'AST-000007';

const TREE: OfficeTreeNodeDto = {
  id: 'office-hq',
  name: 'Headquarters',
  code: 'HQ',
  parentOfficeId: null,
  children: [],
};

/** Builds an asset detail with overrides (status in particular). */
function asset(overrides: Partial<AssetDetailDto> = {}): AssetDetailDto {
  return {
    id: 'asset-1',
    tag: TAG,
    name: 'MacBook Pro 14',
    manufacturer: 'Apple',
    model: 'MBP14/M4/16',
    serialNumber: 'C02XK1ABJGH5',
    status: 'Maintenance',
    condition: 'Good',
    purchaseDate: '2026-01-15',
    purchaseCostAmount: 2399,
    purchaseCostCurrency: 'USD',
    notes: null,
    officeId: 'office-hq',
    officeName: 'Headquarters',
    categoryId: 'cat-laptops',
    categoryName: 'Laptops',
    createdAtUtc: '2026-01-16T09:30:00Z',
    currentAssigneeName: null,
    currentAssigneeEmail: null,
    assignments: [
      {
        id: 'assign-2',
        assigneeName: 'Jordan Reyes',
        assigneeEmail: 'jordan@example.com',
        assignedAtUtc: '2026-03-01T10:00:00Z',
        returnedAtUtc: null,
      },
      {
        id: 'assign-1',
        assigneeName: 'Sam Smith',
        assigneeEmail: 'sam@example.com',
        assignedAtUtc: '2026-01-20T10:00:00Z',
        returnedAtUtc: '2026-02-28T16:00:00Z',
      },
    ],
    ...overrides,
  };
}

const LABEL: AssetLabel = {
  tag: TAG,
  labelText: `${TAG} · MacBook Pro 14`,
  barcodeSvg: '<svg xmlns="http://www.w3.org/2000/svg"><rect width="10" height="40" /></svg>',
  qrSvg: '<svg xmlns="http://www.w3.org/2000/svg"><path d="M0 0h7v7H0z" /></svg>',
};

/**
 * Fires the form's ngSubmit binding. The components bind (ngSubmit) without
 * FormsModule, so Angular listens for a DOM event literally named "ngSubmit" —
 * dispatching that event is what actually reaches the handler.
 */
function submitForm(form: HTMLFormElement): void {
  form.dispatchEvent(new Event('ngSubmit', { bubbles: true }));
}

describe('AssetDetail', () => {
  /** Configures the TestBed with a fake ApiService and renders the given tag. */
  function createFixture(
    overrides: {
      asset$?: Observable<AssetDetailDto | null>;
      label$?: Observable<AssetLabel | null>;
    } = {},
  ) {
    const api = {
      getAssetByTag: vi.fn(() => overrides.asset$ ?? of(asset())),
      getAssetLabel: vi.fn(() => overrides.label$ ?? of(LABEL)),
      getOfficeTree: vi.fn(() => of(TREE)),
      assignAsset: vi.fn(() => of(undefined)),
      returnAsset: vi.fn(() => of(undefined)),
      startMaintenance: vi.fn(() => of(undefined)),
      resumeMaintenance: vi.fn(() => of(undefined)),
      retireAsset: vi.fn(() => of(undefined)),
      disposeAsset: vi.fn(() => of(undefined)),
      transferAsset: vi.fn(() => of(undefined)),
    };
    TestBed.configureTestingModule({
      imports: [AssetDetail],
      providers: [provideRouter([]), { provide: ApiService, useValue: api }],
    });
    const fixture = TestBed.createComponent(AssetDetail);
    fixture.componentRef.setInput('tag', TAG);
    const element = fixture.nativeElement as HTMLElement;
    return { fixture, element, api };
  }

  it('renders the name, tag and status badge', async () => {
    const { fixture, element } = createFixture();
    await fixture.whenStable();

    expect(element.querySelector('h1')?.textContent?.trim()).toBe('MacBook Pro 14');
    expect(element.textContent).toContain(TAG);
    const badge = element.querySelector('app-status-badge .badge');
    expect(badge?.classList).toContain('badge-maintenance');
    expect(badge?.textContent?.trim()).toBe('Maintenance');
  });

  it('renders the assignment history with open and closed entries', async () => {
    const { fixture, element } = createFixture();
    await fixture.whenStable();

    const rows = element.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(element.textContent).toContain('Jordan Reyes');
    expect(element.textContent).toContain('Open');
    expect(element.textContent).toContain('Closed');
  });

  it('shows only the legal toolbar actions for a Maintenance asset', async () => {
    const { fixture, element } = createFixture();
    await fixture.whenStable();

    const toolbar = element.querySelector('[aria-label="Asset actions"]');
    const labels = Array.from(toolbar?.querySelectorAll('button') ?? [])
      .map((button) => (button.textContent ?? '').trim())
      .filter((text) => text.length > 0);
    expect(labels).toEqual(['Resume', 'Retire']);
  });

  it('assigns the asset with the form values and refetches the detail', async () => {
    const { fixture, element, api } = createFixture({ asset$: of(asset({ status: 'InStock' })) });
    await fixture.whenStable();

    const toolbar = element.querySelector('[aria-label="Asset actions"]');
    Array.from(toolbar?.querySelectorAll('button') ?? [])
      .find((button) => (button.textContent ?? '').trim() === 'Assign')
      ?.dispatchEvent(new Event('click'));
    await fixture.whenStable();

    const nameInput = element.querySelector<HTMLInputElement>(`#assignee-name-asset-1`)!;
    const emailInput = element.querySelector<HTMLInputElement>(`#assignee-email-asset-1`)!;
    nameInput.value = 'Jordan Reyes';
    nameInput.dispatchEvent(new Event('input', { bubbles: true }));
    emailInput.value = 'jordan@example.com';
    emailInput.dispatchEvent(new Event('input', { bubbles: true }));

    submitForm(element.querySelector('form')!);
    await fixture.whenStable();

    expect(api.assignAsset).toHaveBeenCalledWith(TAG, {
      assigneeName: 'Jordan Reyes',
      assigneeEmail: 'jordan@example.com',
    });
    // A successful action triggers a refetch of the detail.
    expect(api.getAssetByTag).toHaveBeenCalledTimes(2);
  });

  it('injects the barcode and QR label SVGs', async () => {
    const { fixture, element } = createFixture();
    await fixture.whenStable();

    const barcode = element.querySelector('[data-testid="label-barcode"]');
    const qr = element.querySelector('[data-testid="label-qr"]');
    expect(barcode?.innerHTML).toContain('<svg');
    expect(qr?.innerHTML).toContain('<svg');
    expect(element.textContent).toContain(LABEL.labelText);
  });

  it('renders the not-found state for an unknown tag', async () => {
    const { fixture, element } = createFixture({
      asset$: throwError(
        () =>
          new HttpErrorResponse({
            status: 404,
            error: { title: 'Not Found', status: 404, detail: 'Asset not found.' },
          }),
      ),
      label$: of(null),
    });
    await fixture.whenStable();

    expect(element.textContent).toContain('Asset not found');
    expect(element.textContent).toContain(`No asset with tag ${TAG} exists`);
    expect(element.querySelector('h1')).toBeNull();
  });
});
