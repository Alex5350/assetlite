import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ApiService } from '../../core/api.service';
import { CategoryDto } from '../../models';
import { Categories } from './categories';

const CATEGORIES: CategoryDto[] = [
  {
    id: 'cat-laptops',
    name: 'Laptops',
    description: 'Portable computers',
    expectedLifespanMonths: 36,
  },
  { id: 'cat-monitors', name: 'Monitors', description: null, expectedLifespanMonths: 60 },
];

/** A 400 ValidationProblemDetails body as the API emits for duplicate names. */
function duplicateNameError(): HttpErrorResponse {
  return new HttpErrorResponse({
    status: 400,
    error: {
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { Name: ['A category with this name already exists.'] },
    },
  });
}

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

describe('Categories', () => {
  /** Configures the TestBed with a fake ApiService and creates the fixture. */
  function createFixture(overrides: { createCategory$?: unknown } = {}) {
    const api = {
      getCategories: vi.fn(() => of(CATEGORIES)),
      createCategory: vi.fn(() => overrides.createCategory$ ?? of(CATEGORIES[0])),
      updateCategory: vi.fn(() => of(CATEGORIES[0])),
    };
    TestBed.configureTestingModule({
      imports: [Categories],
      providers: [{ provide: ApiService, useValue: api }],
    });
    const fixture = TestBed.createComponent(Categories);
    const element = fixture.nativeElement as HTMLElement;
    return { fixture, element, api };
  }

  /** Finds a button whose text contains the given label. */
  function button(element: HTMLElement, label: string): HTMLButtonElement | undefined {
    return Array.from(element.querySelectorAll('button')).find((b) =>
      (b.textContent ?? '').includes(label),
    );
  }

  it('renders one row per category with an em dash for missing descriptions', async () => {
    const { fixture, element } = createFixture();
    await fixture.whenStable();

    const rows = element.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Laptops');
    expect(rows[0].textContent).toContain('Portable computers');
    expect(rows[0].textContent).toContain('36');
    expect(rows[1].textContent).toContain('Monitors');
    expect(rows[1].textContent).toContain('—');
  });

  it('updates a category via PUT from the inline editor and refreshes the list', async () => {
    const { fixture, element, api } = createFixture();
    await fixture.whenStable();

    // Enter edit mode on the first row; inputs are prefilled from the row.
    button(element, 'Edit')?.click();
    await fixture.whenStable();
    const nameInput = element.querySelector<HTMLInputElement>('input[aria-label="Category name"]')!;
    expect(nameInput.value).toBe('Laptops');

    setInput(nameInput, 'Laptops Pro');
    button(element, 'Save')?.click();
    await fixture.whenStable();

    expect(api.updateCategory).toHaveBeenCalledWith('cat-laptops', {
      name: 'Laptops Pro',
      description: 'Portable computers',
      expectedLifespanMonths: 36,
    });
    // The success notice shows and the list is re-fetched.
    expect(element.querySelector('[role="status"]')?.textContent).toContain('Laptops Pro');
    expect(api.getCategories).toHaveBeenCalledTimes(2);
  });

  it('surfaces server duplicate-name validation errors on the name field', async () => {
    const { fixture, element, api } = createFixture({
      createCategory$: throwError(() => duplicateNameError()),
    });
    await fixture.whenStable();

    button(element, '+ New category')?.click();
    await fixture.whenStable();
    setInput(element.querySelector<HTMLInputElement>('#category-name')!, 'Laptops');
    submitForm(element.querySelector('form')!);
    await fixture.whenStable();

    expect(api.createCategory).toHaveBeenCalledWith({
      name: 'Laptops',
      description: null,
      expectedLifespanMonths: 60,
    });
    expect(element.textContent).toContain('A category with this name already exists.');
    expect(element.querySelector('[role="alert"]')).toBeTruthy();
  });

  it('requires a name client-side before calling the API', async () => {
    const { fixture, element, api } = createFixture();
    await fixture.whenStable();

    button(element, '+ New category')?.click();
    await fixture.whenStable();
    submitForm(element.querySelector('form')!);
    await fixture.whenStable();

    expect(api.createCategory).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Category name is required.');
  });
});
