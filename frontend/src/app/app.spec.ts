import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the AssetLite brand and primary navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('AssetLite');
    for (const label of ['Dashboard', 'Assets', 'Offices', 'Categories', 'Reports']) {
      expect(compiled.textContent).toContain(label);
    }
  });

  it('keeps the shell navigation-only (page headers own search and actions)', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    // The assets page already provides search + New asset in context; a global
    // topbar duplicated both. Guards against the duplicate creeping back.
    expect(compiled.querySelector('#global-search')).toBeNull();
    expect(Array.from(compiled.querySelectorAll('a')).some((a) => a.textContent?.includes('New asset'))).toBe(false);
  });
});
