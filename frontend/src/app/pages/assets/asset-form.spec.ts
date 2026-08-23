import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ApiService } from '../../core/api.service';
import { AssetDetailDto, CategoryDto, OfficeTreeNodeDto } from '../../models';
import { AssetForm } from './asset-form';

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

const CREATED: AssetDetailDto = {
  id: 'asset-42',
  tag: 'AST-000042',
  name: 'MacBook Pro 16',
  manufacturer: 'Apple',
  model: null,
  serialNumber: null,
  status: 'InStock',
  condition: 'New',
  purchaseDate: null,
  purchaseCostAmount: 2499.5,
  purchaseCostCurrency: 'USD',
  notes: null,
  officeId: 'office-hq',
  officeName: 'Headquarters',
  categoryId: 'cat-laptops',
  categoryName: 'Laptops',
  createdAtUtc: '2026-08-22T10:00:00Z',
  currentAssigneeName: null,
  currentAssigneeEmail: null,
  assignments: [],
};

/** A 400 ValidationProblemDetails body as the API emits for invalid fields. */
function validationError(): HttpErrorResponse {
  return new HttpErrorResponse({
    status: 400,
    error: {
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { Name: ['Name is required.'] },
    },
  });
}

/** Types a value into an input the way a user would (event → signal → CD). */
function setInput(input: HTMLInputElement, value: string): void {
  input.value = value;
  input.dispatchEvent(new Event('input', { bubbles: true }));
}

/** Picks a select option the way a user would. */
function setSelect(select: HTMLSelectElement, value: string): void {
  select.value = value;
  select.dispatchEvent(new Event('change'));
}

/**
 * Fires the form's ngSubmit binding. The components bind (ngSubmit) without
 * FormsModule, so Angular listens for a DOM event literally named "ngSubmit" —
 * dispatching that event is what actually reaches the handler.
 */
function submitForm(form: HTMLFormElement): void {
  form.dispatchEvent(new Event('ngSubmit', { bubbles: true }));
}

/** Fills the three client-required fields (name, category, office). */
function fillRequiredFields(element: HTMLElement, name = 'MacBook Pro 16'): void {
  setInput(element.querySelector<HTMLInputElement>('#new-name')!, name);
  setSelect(element.querySelector<HTMLSelectElement>('#new-category')!, 'cat-laptops');
  setSelect(element.querySelector<HTMLSelectElement>('#new-office')!, 'office-hq');
}

describe('AssetForm', () => {
  /** Configures the TestBed with a fake ApiService and a spied Router. */
  function createFixture(overrides: { registerAsset$?: unknown } = {}) {
    const api = {
      getOfficeTree: vi.fn(() => of(TREE)),
      getCategories: vi.fn(() => of(CATEGORIES)),
      registerAsset: vi.fn(() => overrides.registerAsset$ ?? of(CREATED)),
    };
    TestBed.configureTestingModule({
      imports: [AssetForm],
      providers: [provideRouter([]), { provide: ApiService, useValue: api }],
    });
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate');
    const fixture = TestBed.createComponent(AssetForm);
    const element = fixture.nativeElement as HTMLElement;
    return { fixture, element, api, navigateSpy };
  }

  it('blocks submission while required fields are missing', async () => {
    const { fixture, element, api } = createFixture();
    await fixture.whenStable();

    const submitButton = element.querySelector<HTMLButtonElement>('button[type="submit"]');
    expect(submitButton?.disabled).toBe(true);

    // Even a manual submit event cannot bypass the guard.
    submitForm(element.querySelector('form')!);
    await fixture.whenStable();
    expect(api.registerAsset).not.toHaveBeenCalled();

    // The name alone is not enough — category and office are also required.
    setInput(element.querySelector<HTMLInputElement>('#new-name')!, 'MacBook Pro 16');
    await fixture.whenStable();
    expect(element.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(true);
  });

  it('registers the asset and navigates to the new detail page', async () => {
    const { fixture, element, api, navigateSpy } = createFixture();
    await fixture.whenStable();

    fillRequiredFields(element, '  MacBook Pro 16  ');
    setInput(element.querySelector<HTMLInputElement>('#new-manufacturer')!, 'Apple');
    setInput(element.querySelector<HTMLInputElement>('#new-cost')!, '2499.50');
    await fixture.whenStable();
    expect(element.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(false);

    submitForm(element.querySelector('form')!);
    await fixture.whenStable();

    expect(api.registerAsset).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'MacBook Pro 16', // trimmed client-side
        categoryId: 'cat-laptops',
        officeId: 'office-hq',
        condition: 'New',
        manufacturer: 'Apple',
        purchaseCost: 2499.5,
        currency: 'USD',
      }),
    );
    expect(navigateSpy).toHaveBeenCalledWith(['/assets', 'AST-000042']);
  });

  it('maps server validation problems onto the matching field error', async () => {
    const { fixture, element, api } = createFixture({
      registerAsset$: throwError(() => validationError()),
    });
    await fixture.whenStable();

    fillRequiredFields(element);
    await fixture.whenStable();
    submitForm(element.querySelector('form')!);
    await fixture.whenStable();

    const nameInput = element.querySelector<HTMLInputElement>('#new-name');
    expect(nameInput?.getAttribute('aria-invalid')).toBe('true');
    expect(element.textContent).toContain('Name is required.');
    expect(element.querySelector('section[role="alert"]')).toBeTruthy();
  });
});
