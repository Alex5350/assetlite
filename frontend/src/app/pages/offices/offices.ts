import { ChangeDetectionStrategy, Component, WritableSignal, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, of, startWith, switchMap, tap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { FlatOfficeOption, OfficeTreeNodeDto, flattenOfficeTree } from '../../models';
import { EmptyState } from '../../shared/empty-state';
import { LoadingSkeleton } from '../../shared/loading-skeleton';
import { ApiProblem, parseProblem } from '../../shared/api-error';

const CODE_PATTERN = /^[A-Z0-9]{3,8}$/;

/** Collects a node plus all of its descendants' ids (for cycle-safe parent options). */
function subtreeIds(node: OfficeTreeNodeDto): Set<string> {
  const ids = new Set<string>([node.id]);
  for (const child of node.children) {
    for (const id of subtreeIds(child)) {
      ids.add(id);
    }
  }
  return ids;
}

/** Counts all descendants of a node (for the child-count badge). */
function countDescendants(node: OfficeTreeNodeDto): number {
  return node.children.reduce((acc, child) => acc + 1 + countDescendants(child), 0);
}

/**
 * Offices: the hierarchy tree with inline creation and re-parenting (move).
 * The API enforces the guard rails (unique code, single root, no cycles,
 * max depth) — failures surface in a dismissible problem panel.
 */
@Component({
  selector: 'app-offices',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, LoadingSkeleton],
  templateUrl: './offices.html',
})
export class Offices {
  private readonly api = inject(ApiService);

  // --- Tree load -------------------------------------------------------------
  private readonly reload$ = new Subject<void>();

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  private readonly tree$ = this.reload$.pipe(
    startWith(null),
    tap(() => {
      this.loading.set(true);
      this.error.set(null);
    }),
    switchMap(() =>
      this.api.getOfficeTree().pipe(
        tap(() => this.loading.set(false)),
        catchError((err) => {
          this.loading.set(false);
          this.error.set(parseProblem(err).messages[0] ?? 'Failed to load the office tree.');
          return of(null);
        }),
      ),
    ),
  );

  protected readonly root = toSignal(this.tree$, { initialValue: null });

  /** Depth-annotated rows, depth-first (children ordered by name, as delivered). */
  protected readonly rows = computed<FlatOfficeOption[]>(() => {
    const root = this.root();
    return root ? flattenOfficeTree([root]) : [];
  });

  protected readonly totalOffices = computed(() => this.rows().length);

  /** id → descendant count, for the child badges. */
  protected readonly descendantCounts = computed(() => {
    const counts = new Map<string, number>();
    for (const row of this.rows()) {
      counts.set(row.office.id, countDescendants(row.office));
    }
    return counts;
  });

  // --- Action state ------------------------------------------------------------
  protected readonly actionError = signal<ApiProblem | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly busy = signal(false);

  // Create form
  protected readonly createOpen = signal(false);
  protected readonly newName = signal('');
  protected readonly newCode = signal('');
  protected readonly newParentId = signal('');
  protected readonly createFieldErrors = signal<Record<string, string>>({});

  // Move form (one open row at a time)
  protected readonly movingId = signal<string | null>(null);
  protected readonly moveTargetId = signal('');
  protected readonly moveFieldErrors = signal<Record<string, string>>({});

  protected readonly depthDashes = (depth: number): string[] => Array.from({ length: depth });

  protected retry(): void {
    this.reload$.next();
  }

  protected openCreate(): void {
    this.createOpen.set(!this.createOpen());
    this.createFieldErrors.set({});
    const root = this.root();
    if (this.createOpen() && !this.newParentId() && root) {
      // Sensible default: hang new offices under the current root.
      this.newParentId.set(root.id);
    }
  }

  protected dismissError(): void {
    this.actionError.set(null);
  }

  protected dismissNotice(): void {
    this.notice.set(null);
  }

  protected submitCreate(): void {
    const name = this.newName().trim();
    const code = this.newCode().trim().toUpperCase();
    const parentId = this.newParentId();

    const fieldErrors: Record<string, string> = {};
    if (!name) {
      fieldErrors['Name'] = 'Office name is required.';
    }
    if (!CODE_PATTERN.test(code)) {
      fieldErrors['Code'] = 'Code must be 3-8 uppercase alphanumeric characters.';
    }
    this.createFieldErrors.set(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) {
      return;
    }

    this.run(this.api.createOffice({ name, code, parentOfficeId: parentId || null }), {
      success: `Office “${name}” created.`,
      fieldErrors: this.createFieldErrors,
      onDone: () => {
        this.newName.set('');
        this.newCode.set('');
        this.createOpen.set(false);
      },
    });
  }

  protected openMove(row: FlatOfficeOption): void {
    this.movingId.set(this.movingId() === row.office.id ? null : row.office.id);
    this.moveTargetId.set('');
    this.moveFieldErrors.set({});
  }

  /** Parent options for re-parenting a node: everything except itself and its subtree. */
  protected moveParentOptions(row: FlatOfficeOption): FlatOfficeOption[] {
    const excluded = subtreeIds(row.office);
    return this.rows().filter((option) => !excluded.has(option.office.id));
  }

  protected submitMove(row: FlatOfficeOption): void {
    if (!this.moveTargetId()) {
      this.moveFieldErrors.set({ NewParentOfficeId: 'Pick a new parent office.' });
      return;
    }
    if (this.moveTargetId() === row.office.parentOfficeId) {
      this.moveFieldErrors.set({ NewParentOfficeId: 'The office already reports to that parent.' });
      return;
    }
    this.run(this.api.moveOffice(row.office.id, { newParentOfficeId: this.moveTargetId() }), {
      success: `“${row.office.name}” moved.`,
      fieldErrors: this.moveFieldErrors,
      onDone: () => this.movingId.set(null),
    });
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
