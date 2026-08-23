import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ApiService } from '../../core/api.service';
import { OfficeDto, OfficeTreeNodeDto } from '../../models';
import { Offices } from './offices';

/** HQ → East → Berlin: three levels to prove flattening + indentation. */
const TREE: OfficeTreeNodeDto = {
  id: 'office-hq',
  name: 'Headquarters',
  code: 'HQ',
  parentOfficeId: null,
  children: [
    {
      id: 'office-east',
      name: 'East Region',
      code: 'EST',
      parentOfficeId: 'office-hq',
      children: [
        {
          id: 'office-berlin',
          name: 'Berlin',
          code: 'BER',
          parentOfficeId: 'office-east',
          children: [],
        },
      ],
    },
  ],
};

/** A 409 ProblemDetails body as the API emits for duplicate office codes. */
function duplicateCodeError(): HttpErrorResponse {
  return new HttpErrorResponse({
    status: 409,
    error: {
      title: 'Office.DuplicateCode',
      status: 409,
      detail: 'An office with this code already exists.',
      errors: [
        { code: 'Office.DuplicateCode', description: 'An office with this code already exists.' },
      ],
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

describe('Offices', () => {
  /** Configures the TestBed with a fake ApiService and creates the fixture. */
  function createFixture(overrides: { createOffice$?: Observable<OfficeDto> } = {}) {
    const api = {
      getOfficeTree: vi.fn(() => of(TREE)),
      createOffice: vi.fn(
        () =>
          overrides.createOffice$ ??
          of({ id: 'office-new', name: 'Berlin', code: 'BER', parentOfficeId: 'office-hq' }),
      ),
      moveOffice: vi.fn(() => of(undefined)),
    };
    TestBed.configureTestingModule({
      imports: [Offices],
      providers: [{ provide: ApiService, useValue: api }],
    });
    const fixture = TestBed.createComponent(Offices);
    const element = fixture.nativeElement as HTMLElement;
    return { fixture, element, api };
  }

  /** Finds a button whose text contains the given label. */
  function button(element: HTMLElement, label: string): HTMLButtonElement | undefined {
    return Array.from(element.querySelectorAll('button')).find((b) =>
      (b.textContent ?? '').includes(label),
    );
  }

  it('renders the tree flattened depth-first with indented rows and code badges', async () => {
    const { fixture, element } = createFixture();
    await fixture.whenStable();

    // All three levels appear, in depth-first order.
    const codes = Array.from(element.querySelectorAll('[data-testid="office-code"]')).map((badge) =>
      badge.textContent?.trim(),
    );
    expect(codes).toEqual(['HQ', 'EST', 'BER']);
    expect(element.textContent).toContain('Headquarters');
    expect(element.textContent).toContain('3 offices');

    // Indentation grows with depth: root 1.25rem, +1.5rem per level.
    const rows = Array.from(element.querySelectorAll('ul > li > div'));
    expect(rows.map((row) => (row as HTMLElement).style.paddingLeft)).toEqual([
      '1.25rem',
      '2.75rem',
      '4.25rem',
    ]);
  });

  it('uppercases the code client-side and defaults the parent to the root on create', async () => {
    const { fixture, element, api } = createFixture();
    await fixture.whenStable();

    button(element, '+ New office')?.click();
    await fixture.whenStable();

    setInput(element.querySelector<HTMLInputElement>('#office-name')!, 'Berlin');
    setInput(element.querySelector<HTMLInputElement>('#office-code')!, 'ber');
    submitForm(element.querySelector('form')!);
    await fixture.whenStable();

    expect(api.createOffice).toHaveBeenCalledWith({
      name: 'Berlin',
      code: 'BER',
      parentOfficeId: 'office-hq',
    });
    // Success notice appears and the tree reloads.
    expect(element.querySelector('[role="status"]')?.textContent).toContain('Berlin');
    expect(api.getOfficeTree).toHaveBeenCalledTimes(2);
  });

  it('rejects invalid codes client-side without calling the API', async () => {
    const { fixture, element, api } = createFixture();
    await fixture.whenStable();

    button(element, '+ New office')?.click();
    await fixture.whenStable();

    setInput(element.querySelector<HTMLInputElement>('#office-name')!, 'Berlin');
    setInput(element.querySelector<HTMLInputElement>('#office-code')!, 'ab');
    submitForm(element.querySelector('form')!);
    await fixture.whenStable();

    expect(api.createOffice).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Code must be 3-8 uppercase alphanumeric characters.');
  });

  it('surfaces duplicate-code problem details (409) in the error panel', async () => {
    const { fixture, element } = createFixture({
      createOffice$: throwError(() => duplicateCodeError()),
    });
    await fixture.whenStable();

    button(element, '+ New office')?.click();
    await fixture.whenStable();

    setInput(element.querySelector<HTMLInputElement>('#office-name')!, 'Berlin');
    setInput(element.querySelector<HTMLInputElement>('#office-code')!, 'BER');
    submitForm(element.querySelector('form')!);
    await fixture.whenStable();

    const panel = element.querySelector('[role="alert"]');
    expect(panel).toBeTruthy();
    expect(panel?.textContent).toContain('Conflict');
    expect(panel?.textContent).toContain('An office with this code already exists.');
  });
});
