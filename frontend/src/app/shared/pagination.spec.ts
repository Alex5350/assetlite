import { TestBed } from '@angular/core/testing';
import { Pagination } from './pagination';

describe('Pagination', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Pagination] });
  });

  /** Creates a pagination fixture for the given paging inputs. */
  function setup(page: number, pageSize: number, total: number) {
    const fixture = TestBed.createComponent(Pagination);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', pageSize);
    fixture.componentRef.setInput('total', total);
    return fixture;
  }

  /** Collects the texts of the numbered page buttons (excludes prev/next). */
  function pageButtonLabels(fixture: ReturnType<typeof setup>): string[] {
    const nav = (fixture.nativeElement as HTMLElement).querySelector('nav');
    return Array.from(nav?.querySelectorAll('button') ?? [])
      .filter((button) => /^\d+$/.test((button.textContent ?? '').trim()))
      .map((button) => (button.textContent ?? '').trim());
  }

  it('computes the page count from total and page size (45 items, size 10 → 5 pages)', async () => {
    const fixture = setup(1, 10, 45);
    await fixture.whenStable();
    const nav = (fixture.nativeElement as HTMLElement).querySelector('nav');
    expect(nav?.textContent).toContain('Page 1 of 5');
    expect(nav?.textContent).toContain('45 assets');
  });

  it('shows a sliding two-page window around the current page', async () => {
    const fixture = setup(3, 10, 45);
    await fixture.whenStable();
    expect(pageButtonLabels(fixture)).toEqual(['1', '2', '3', '4', '5']);
  });

  it('clamps the window at the first and last page', async () => {
    const first = setup(1, 10, 45);
    await first.whenStable();
    expect(pageButtonLabels(first)).toEqual(['1', '2', '3']);

    const last = setup(5, 10, 45);
    await last.whenStable();
    expect(pageButtonLabels(last)).toEqual(['3', '4', '5']);
  });

  it('emits pageChange when a page button is clicked, but not for the current page', async () => {
    const fixture = setup(1, 10, 45);
    await fixture.whenStable();
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => emitted.push(page));

    const nav = (fixture.nativeElement as HTMLElement).querySelector('nav');
    const pageTwo = Array.from(nav?.querySelectorAll('button') ?? []).find(
      (button) => (button.textContent ?? '').trim() === '2',
    );
    pageTwo?.dispatchEvent(new Event('click'));
    const pageOne = Array.from(nav?.querySelectorAll('button') ?? []).find(
      (button) => (button.textContent ?? '').trim() === '1',
    );
    pageOne?.dispatchEvent(new Event('click'));

    expect(emitted).toEqual([2]);
  });

  it('disables prev on the first page and next on the last page', async () => {
    const first = setup(1, 10, 45);
    await first.whenStable();
    const firstNav = (first.nativeElement as HTMLElement).querySelector('nav');
    const prev = firstNav?.querySelector<HTMLButtonElement>('[aria-label="Previous page"]');
    const next = firstNav?.querySelector<HTMLButtonElement>('[aria-label="Next page"]');
    expect(prev?.disabled).toBe(true);
    expect(next?.disabled).toBe(false);

    const last = setup(5, 10, 45);
    await last.whenStable();
    const lastNav = (last.nativeElement as HTMLElement).querySelector('nav');
    expect(
      lastNav?.querySelector<HTMLButtonElement>('[aria-label="Previous page"]')?.disabled,
    ).toBe(false);
    expect(lastNav?.querySelector<HTMLButtonElement>('[aria-label="Next page"]')?.disabled).toBe(
      true,
    );
  });

  it('renders nothing when the page size is not positive', async () => {
    const fixture = setup(1, 0, 45);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('nav')).toBeNull();
  });
});
