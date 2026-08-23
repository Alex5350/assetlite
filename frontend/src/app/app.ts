import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';

interface NavItem {
  readonly path: string;
  readonly label: string;
  readonly icon: string;
}

/** Primary navigation (route paths double as routerLink targets). */
const NAV_ITEMS: readonly NavItem[] = [
  { path: '', label: 'Dashboard', icon: '▦' },
  { path: 'assets', label: 'Assets', icon: '▤' },
  { path: 'offices', label: 'Offices', icon: '⌂' },
  { path: 'categories', label: 'Categories', icon: '☰' },
  { path: 'reports', label: 'Reports', icon: '◎' },
];

/**
 * Application shell: fixed left sidebar on large screens (collapses to a
 * horizontal top bar below `lg` via CSS only — no JS hamburger), a topbar with
 * global asset search, and the router outlet in a max-w-7xl container.
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly router = inject(Router);

  protected readonly navItems = NAV_ITEMS;
  protected readonly searchQuery = signal('');

  /** Global search: navigates to the assets list with the search query param. */
  protected search(): void {
    const query = this.searchQuery().trim();
    this.router.navigate(['/assets'], {
      queryParams: { search: query || null, page: null },
      queryParamsHandling: 'merge',
    });
  }
}
