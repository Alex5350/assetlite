import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

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
 * horizontal top bar below `lg` via CSS only — no JS hamburger) and the router
 * outlet in a max-w-7xl container. Page headers own their actions (search,
 * registration, exports); the shell is navigation-only so nothing duplicates.
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly navItems = NAV_ITEMS;
}
