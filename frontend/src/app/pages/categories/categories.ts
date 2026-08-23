import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Categories — STUB (built fully by a follow-up agent).
 *
 * Suggested implementation notes:
 * - `ApiService.getCategories()` → CategoryDto[]
 *   (id, name, description, expectedLifespanMonths).
 * - A simple .card + .table list with create/edit forms using .btn/.input/.select
 *   classes from src/styles.css is enough; keep signals-first patterns.
 */
@Component({
  selector: 'app-categories',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div>
      <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Categories</h1>
      <p class="mt-1 text-sm text-slate-500">Asset classification and expected lifespans</p>
    </div>
    <section class="card mt-6 p-10 text-center">
      <p class="mx-auto max-w-md text-sm text-slate-500">
        The category list and management forms are under construction.
      </p>
      <span class="badge badge-assigned mx-auto mt-4">Coming soon</span>
    </section>
  `,
})
export class Categories {}
