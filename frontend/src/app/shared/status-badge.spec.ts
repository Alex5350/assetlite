import { TestBed } from '@angular/core/testing';
import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [StatusBadge] });
  });

  /** Creates a badge fixture rendering the given status value. */
  function createFixture(status: string) {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', status);
    return fixture;
  }

  it('renders InStock with the in-stock class and friendly label', async () => {
    const fixture = createFixture('InStock');
    await fixture.whenStable();
    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge?.classList).toContain('badge-in-stock');
    expect(badge?.textContent?.trim()).toBe('In stock');
  });

  it('renders Assigned with the assigned class', async () => {
    const fixture = createFixture('Assigned');
    await fixture.whenStable();
    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge?.classList).toContain('badge-assigned');
    expect(badge?.textContent?.trim()).toBe('Assigned');
  });

  it('renders Maintenance with the maintenance class', async () => {
    const fixture = createFixture('Maintenance');
    await fixture.whenStable();
    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge?.classList).toContain('badge-maintenance');
    expect(badge?.textContent?.trim()).toBe('Maintenance');
  });

  it('renders Retired with the retired class', async () => {
    const fixture = createFixture('Retired');
    await fixture.whenStable();
    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge?.classList).toContain('badge-retired');
    expect(badge?.textContent?.trim()).toBe('Retired');
  });

  it('renders Disposed with the disposed class', async () => {
    const fixture = createFixture('Disposed');
    await fixture.whenStable();
    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge?.classList).toContain('badge-disposed');
    expect(badge?.textContent?.trim()).toBe('Disposed');
  });

  it('falls back to a neutral badge showing the raw value for unknown statuses', async () => {
    const fixture = createFixture('Lost');
    await fixture.whenStable();
    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge?.classList).toContain('badge-neutral');
    expect(badge?.textContent?.trim()).toBe('Lost');
  });
});
