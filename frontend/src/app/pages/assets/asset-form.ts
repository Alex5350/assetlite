import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { catchError, of, tap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AssetCondition, flattenOfficeTree } from '../../models';
import { ApiProblem, parseProblem } from '../../shared/api-error';

const CONDITIONS: readonly AssetCondition[] = ['New', 'Good', 'Fair', 'Poor'];

const CURRENCIES: readonly string[] = ['USD', 'EUR', 'GBP', 'CHF', 'JPY'];

/**
 * Register a new asset (route /assets/new). The API allocates the sequential
 * tag (AST-000001…) and returns the created asset; on success we navigate to
 * its detail page. Server validation problems map onto the matching fields.
 */
@Component({
  selector: 'app-asset-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './asset-form.html',
})
export class AssetForm {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  protected readonly conditions = CONDITIONS;
  protected readonly currencies = CURRENCIES;

  // --- Reference data -------------------------------------------------------
  protected readonly officeRoot = toSignal(
    this.api.getOfficeTree().pipe(catchError(() => of(null))),
    { initialValue: null },
  );
  protected readonly flatOffices = computed(() => {
    const root = this.officeRoot();
    return root ? flattenOfficeTree([root]) : [];
  });

  protected readonly categories = toSignal(
    this.api.getCategories().pipe(catchError(() => of([]))),
    { initialValue: [] },
  );

  protected readonly referenceFailed = computed(
    () => this.flatOffices().length === 0 || this.categories().length === 0,
  );

  // --- Form state -----------------------------------------------------------
  protected readonly name = signal('');
  protected readonly categoryId = signal('');
  protected readonly officeId = signal('');
  protected readonly condition = signal<AssetCondition>('New');
  protected readonly manufacturer = signal('');
  protected readonly model = signal('');
  protected readonly serialNumber = signal('');
  protected readonly purchaseDate = signal('');
  protected readonly purchaseCost = signal('');
  protected readonly currency = signal('USD');
  protected readonly notes = signal('');

  protected readonly submitting = signal(false);
  protected readonly fieldErrors = signal<Record<string, string>>({});
  protected readonly formError = signal<ApiProblem | null>(null);

  protected readonly canSubmit = computed(
    () =>
      !this.submitting() &&
      this.name().trim().length > 0 &&
      Boolean(this.categoryId()) &&
      Boolean(this.officeId()),
  );

  protected readonly depthDashes = (depth: number): string[] => Array.from({ length: depth });

  protected errorFor(field: string): string | null {
    return this.fieldErrors()[field] ?? null;
  }

  protected dismissError(): void {
    this.formError.set(null);
  }

  protected submit(): void {
    if (this.submitting() || !this.canSubmit()) {
      return;
    }
    this.submitting.set(true);
    this.fieldErrors.set({});
    this.formError.set(null);

    const cost = this.purchaseCost().trim();
    this.api
      .registerAsset({
        name: this.name().trim(),
        categoryId: this.categoryId(),
        officeId: this.officeId(),
        condition: this.condition(),
        manufacturer: this.manufacturer().trim() || undefined,
        model: this.model().trim() || undefined,
        serialNumber: this.serialNumber().trim() || undefined,
        purchaseDate: this.purchaseDate() || undefined,
        purchaseCost: cost ? Number(cost) : undefined,
        currency: this.currency(),
        notes: this.notes().trim() || undefined,
      })
      .pipe(
        tap(() => this.submitting.set(false)),
        catchError((err) => {
          this.submitting.set(false);
          const problem = parseProblem(err);
          if (Object.keys(problem.fieldErrors).length > 0) {
            this.fieldErrors.set(problem.fieldErrors);
          }
          this.formError.set(problem);
          return of(null);
        }),
      )
      .subscribe((asset) => {
        if (asset) {
          void this.router.navigate(['/assets', asset.tag]);
        }
      });
  }
}
