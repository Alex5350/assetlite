import { ChangeDetectionStrategy, Component, WritableSignal, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, of, startWith, switchMap, tap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { CategoryDto } from '../../models';
import { EmptyState } from '../../shared/empty-state';
import { TableSkeleton } from '../../shared/loading-skeleton';
import { ApiProblem, parseProblem } from '../../shared/api-error';
import { dash } from '../../shared/format';

/** Draft values for the row currently being edited. */
interface CategoryDraft {
  name: string;
  description: string;
  lifespanMonths: string;
}

/**
 * Categories: table with create form and inline row editing (PUT).
 * Duplicate names are rejected by the API and surfaced as field errors.
 */
@Component({
  selector: 'app-categories',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, TableSkeleton],
  templateUrl: './categories.html',
})
export class Categories {
  private readonly api = inject(ApiService);

  // --- List load --------------------------------------------------------------
  private readonly reload$ = new Subject<void>();

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  private readonly categories$ = this.reload$.pipe(
    startWith(null),
    tap(() => {
      this.loading.set(true);
      this.error.set(null);
    }),
    switchMap(() =>
      this.api.getCategories().pipe(
        tap(() => this.loading.set(false)),
        catchError((err) => {
          this.loading.set(false);
          this.error.set(parseProblem(err).messages[0] ?? 'Failed to load categories.');
          return of([] as CategoryDto[]);
        }),
      ),
    ),
  );

  protected readonly categories = toSignal(this.categories$, { initialValue: [] as CategoryDto[] });

  // --- Action state -------------------------------------------------------------
  protected readonly actionError = signal<ApiProblem | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly busy = signal(false);

  // Create form
  protected readonly createOpen = signal(false);
  protected readonly newName = signal('');
  protected readonly newDescription = signal('');
  protected readonly newLifespan = signal('60');
  protected readonly createFieldErrors = signal<Record<string, string>>({});

  // Inline edit (one row at a time)
  protected readonly editingId = signal<string | null>(null);
  protected readonly draft = signal<CategoryDraft>({ name: '', description: '', lifespanMonths: '' });
  protected readonly editFieldErrors = signal<Record<string, string>>({});

  protected readonly dash = dash;

  protected retry(): void {
    this.reload$.next();
  }

  protected openCreate(): void {
    this.createOpen.set(!this.createOpen());
    this.createFieldErrors.set({});
  }

  protected dismissError(): void {
    this.actionError.set(null);
  }

  protected dismissNotice(): void {
    this.notice.set(null);
  }

  protected submitCreate(): void {
    const name = this.newName().trim();
    const lifespan = Number(this.newLifespan());
    const fieldErrors = validateDraft({ name, description: this.newDescription().trim(), lifespanMonths: this.newLifespan() });
    this.createFieldErrors.set(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) {
      return;
    }
    this.run(
      this.api.createCategory({
        name,
        description: this.newDescription().trim() || null,
        expectedLifespanMonths: Math.round(lifespan),
      }),
      {
        success: `Category “${name}” created.`,
        fieldErrors: this.createFieldErrors,
        onDone: () => {
          this.newName.set('');
          this.newDescription.set('');
          this.newLifespan.set('60');
          this.createOpen.set(false);
        },
      },
    );
  }

  protected startEdit(category: CategoryDto): void {
    this.editingId.set(category.id);
    this.editFieldErrors.set({});
    this.actionError.set(null);
    this.draft.set({
      name: category.name,
      description: category.description ?? '',
      lifespanMonths: String(category.expectedLifespanMonths),
    });
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
    this.editFieldErrors.set({});
  }

  protected setDraft(patch: Partial<CategoryDraft>): void {
    this.draft.update((current) => ({ ...current, ...patch }));
  }

  protected submitEdit(category: CategoryDto): void {
    const draft = this.draft();
    const name = draft.name.trim();
    const lifespan = Number(draft.lifespanMonths);
    const fieldErrors = validateDraft(draft);
    this.editFieldErrors.set(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) {
      return;
    }
    this.run(
      this.api.updateCategory(category.id, {
        name,
        description: draft.description.trim() || null,
        expectedLifespanMonths: Math.round(lifespan),
      }),
      {
        success: `Category “${name}” saved.`,
        fieldErrors: this.editFieldErrors,
        onDone: () => this.editingId.set(null),
      },
    );
  }

  /** Wraps a mutating call: busy flag, problem panel on failure, reload + notice on success. */
  private run(
    call: Observable<unknown>,
    options: { success: string; fieldErrors?: WritableSignal<Record<string, string>>; onDone?: () => void },
  ): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionError.set(null);
    this.notice.set(null);
    call
      .pipe(
        tap(() => {
          this.busy.set(false);
          options.fieldErrors?.set({});
          options.onDone?.();
          this.notice.set(options.success);
          this.reload$.next();
        }),
        catchError((err) => {
          this.busy.set(false);
          const problem = parseProblem(err);
          if (options.fieldErrors && Object.keys(problem.fieldErrors).length > 0) {
            options.fieldErrors.set(problem.fieldErrors);
          }
          this.actionError.set(problem);
          return of(null);
        }),
      )
      .subscribe();
  }
}

/** Client-side mirror of the API rules: required name, positive integer lifespan. */
function validateDraft(draft: CategoryDraft): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!draft.name.trim()) {
    errors['Name'] = 'Category name is required.';
  }
  const lifespan = Number(draft.lifespanMonths);
  if (!Number.isFinite(lifespan) || lifespan <= 0 || !Number.isInteger(lifespan)) {
    errors['ExpectedLifespanMonths'] = 'Expected lifespan must be a whole number of months (1 or more).';
  }
  return errors;
}
