import { ChangeDetectionStrategy, Component, WritableSignal, computed, inject, input, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Observable, Subject, catchError, merge, of, switchMap, tap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AssetStatus, flattenOfficeTree } from '../../models';
import { StatusBadge } from '../../shared/status-badge';
import { EmptyState } from '../../shared/empty-state';
import { LoadingSkeleton } from '../../shared/loading-skeleton';
import { ApiProblem, parseProblem } from '../../shared/api-error';
import { dash, formatDate, formatDateTime, formatMoney } from '../../shared/format';

/** Inline sub-form currently expanded in the action toolbar. */
type ToolbarPanel = 'assign' | 'transfer' | null;

/** Command awaiting an explicit confirm click. */
type ConfirmedAction = 'return' | 'maintenance' | 'resume' | 'retire' | 'dispose';

/** Lifecycle actions available for each status (mirrors the domain transitions). */
const ACTIONS_BY_STATUS: Record<AssetStatus, readonly (ConfirmedAction | 'assign' | 'transfer')[]> = {
  InStock: ['assign', 'maintenance', 'transfer', 'retire'],
  Assigned: ['assign', 'return', 'maintenance', 'transfer', 'retire'],
  Maintenance: ['resume', 'retire'],
  Retired: ['dispose'],
  Disposed: [],
};

/** Copy for the confirm strip of each confirmable action. */
const CONFIRM_TEXT: Record<ConfirmedAction, string> = {
  return: 'Return this asset to stock?',
  maintenance: 'Send this asset to maintenance?',
  resume: 'Return this asset to stock from maintenance?',
  retire: 'Retire this asset? It leaves active service.',
  dispose: 'Dispose of this asset permanently? This cannot be undone.',
};

/**
 * Asset detail (route /assets/:tag): header card with specs, a status-driven
 * action toolbar (assign/return/maintenance/retire/dispose/transfer), the
 * assignment history and the printable label (barcode + QR SVGs).
 */
@Component({
  selector: 'app-asset-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, StatusBadge, EmptyState, LoadingSkeleton],
  templateUrl: './asset-detail.html',
  styleUrl: './asset-detail.css',
})
export class AssetDetail {
  /** Canonical asset tag from the route (withComponentInputBinding). */
  readonly tag = input.required<string>();

  private readonly api = inject(ApiService);
  private readonly sanitizer = inject(DomSanitizer);

  // --- Detail load (initial + retry + tag changes) --------------------------
  private readonly reload$ = new Subject<void>();

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);

  private readonly load$ = merge(toObservable(this.tag), this.reload$).pipe(
    tap(() => {
      this.loading.set(true);
      this.error.set(null);
      this.notFound.set(false);
      this.resetActionState();
    }),
    switchMap(() =>
      this.api.getAssetByTag(this.tag()).pipe(
        tap(() => this.loading.set(false)),
        catchError((err) => {
          this.loading.set(false);
          this.notFound.set(err?.status === 404);
          this.error.set(parseProblem(err).messages[0] ?? 'Failed to load the asset.');
          return of(null);
        }),
      ),
    ),
  );

  protected readonly asset = toSignal(this.load$, { initialValue: null });

  // --- Label ----------------------------------------------------------------
  // Trusted same-origin artwork: the API generates both SVGs from the asset
  // tag alone, so they are safe to embed after bypassing Angular's HTML
  // sanitizer (which would strip the <svg> markup).
  private readonly label$ = toObservable(this.tag).pipe(
    switchMap(() => this.api.getAssetLabel(this.tag()).pipe(catchError(() => of(null)))),
  );
  protected readonly label = toSignal(this.label$, { initialValue: null });

  protected readonly barcodeHtml = computed<SafeHtml | null>(() => {
    const svg = this.label()?.barcodeSvg;
    return svg ? this.sanitizer.bypassSecurityTrustHtml(svg) : null;
  });

  protected readonly qrHtml = computed<SafeHtml | null>(() => {
    const svg = this.label()?.qrSvg;
    return svg ? this.sanitizer.bypassSecurityTrustHtml(svg) : null;
  });

  // --- Reference data for transfer ------------------------------------------
  protected readonly officeRoot = toSignal(
    this.api.getOfficeTree().pipe(catchError(() => of(null))),
    { initialValue: null },
  );
  protected readonly transferTargets = computed(() => {
    const root = this.officeRoot();
    const current = this.asset()?.officeId;
    const flat = root ? flattenOfficeTree([root]) : [];
    return flat.filter((option) => option.office.id !== current);
  });

  // --- Action state -----------------------------------------------------------
  protected readonly activePanel = signal<ToolbarPanel>(null);
  protected readonly confirmAction = signal<ConfirmedAction | null>(null);
  protected readonly actionBusy = signal(false);
  protected readonly actionError = signal<ApiProblem | null>(null);
  protected readonly notice = signal<string | null>(null);

  // Assign form
  protected readonly assigneeName = signal('');
  protected readonly assigneeEmail = signal('');
  protected readonly assignFieldErrors = signal<Record<string, string>>({});

  // Transfer form
  protected readonly targetOfficeId = signal('');
  protected readonly transferFieldErrors = signal<Record<string, string>>({});

  protected readonly availableActions = computed(() => {
    const asset = this.asset();
    return asset ? (ACTIONS_BY_STATUS[asset.status] ?? []) : [];
  });

  protected readonly confirmText = computed(() => {
    const action = this.confirmAction();
    return action ? CONFIRM_TEXT[action] : '';
  });

  protected readonly currentAssignee = computed(() => {
    const asset = this.asset();
    return asset?.currentAssigneeName
      ? { name: asset.currentAssigneeName, email: asset.currentAssigneeEmail }
      : null;
  });

  /** Depth dashes for indented office options ("– HQ child" → "– – Child"). */
  protected readonly depthDashes = (depth: number): string[] => Array.from({ length: depth });

  protected readonly money = formatMoney;
  protected readonly date = formatDate;
  protected readonly dateTime = formatDateTime;
  protected readonly dash = dash;

  protected retry(): void {
    this.reload$.next();
  }

  protected openPanel(panel: Exclude<ToolbarPanel, null>): void {
    this.confirmAction.set(null);
    this.actionError.set(null);
    this.activePanel.set(this.activePanel() === panel ? null : panel);
  }

  protected askConfirm(action: ConfirmedAction): void {
    this.activePanel.set(null);
    this.actionError.set(null);
    this.confirmAction.set(this.confirmAction() === action ? null : action);
  }

  protected cancelConfirm(): void {
    this.confirmAction.set(null);
  }

  protected dismissError(): void {
    this.actionError.set(null);
  }

  protected dismissNotice(): void {
    this.notice.set(null);
  }

  protected submitAssign(): void {
    const name = this.assigneeName().trim();
    const email = this.assigneeEmail().trim();
    const fieldErrors: Record<string, string> = {};
    if (!name) {
      fieldErrors['AssigneeName'] = 'Assignee name is required.';
    }
    if (!email) {
      fieldErrors['AssigneeEmail'] = 'Assignee email is required.';
    }
    this.assignFieldErrors.set(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) {
      return;
    }
    this.runAction(this.api.assignAsset(this.tag(), { assigneeName: name, assigneeEmail: email }), {
      success: `Assigned to ${name}.`,
      fieldErrors: this.assignFieldErrors,
      onDone: () => {
        this.assigneeName.set('');
        this.assigneeEmail.set('');
      },
    });
  }

  protected submitTransfer(): void {
    if (!this.targetOfficeId()) {
      this.transferFieldErrors.set({ TargetOfficeId: 'Pick a destination office.' });
      return;
    }
    this.runAction(this.api.transferAsset(this.tag(), { targetOfficeId: this.targetOfficeId() }), {
      success: 'Asset transferred.',
      fieldErrors: this.transferFieldErrors,
      onDone: () => this.targetOfficeId.set(''),
    });
  }

  protected executeConfirmed(): void {
    const action = this.confirmAction();
    if (!action) {
      return;
    }
    const tag = this.tag();
    const calls: Record<ConfirmedAction, () => Observable<void>> = {
      return: () => this.api.returnAsset(tag),
      maintenance: () => this.api.startMaintenance(tag),
      resume: () => this.api.resumeMaintenance(tag),
      retire: () => this.api.retireAsset(tag),
      dispose: () => this.api.disposeAsset(tag),
    };
    const success: Record<ConfirmedAction, string> = {
      return: 'Returned to stock.',
      maintenance: 'Sent to maintenance.',
      resume: 'Back in stock from maintenance.',
      retire: 'Asset retired.',
      dispose: 'Asset disposed.',
    };
    this.runAction(calls[action](), { success: success[action] });
  }

  protected printLabel(): void {
    window.print();
  }

  /** Wraps a mutating call: busy flag, problem panel on failure, refetch + notice on success. */
  private runAction(
    call: Observable<unknown>,
    options: { success: string; fieldErrors?: WritableSignal<Record<string, string>>; onDone?: () => void },
  ): void {
    if (this.actionBusy()) {
      return;
    }
    this.actionBusy.set(true);
    this.actionError.set(null);
    this.notice.set(null);
    call
      .pipe(
        tap(() => {
          this.actionBusy.set(false);
          this.activePanel.set(null);
          this.confirmAction.set(null);
          options.fieldErrors?.set({});
          options.onDone?.();
          this.notice.set(options.success);
          this.reload$.next();
        }),
        catchError((err) => {
          this.actionBusy.set(false);
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

  private resetActionState(): void {
    this.activePanel.set(null);
    this.confirmAction.set(null);
    this.actionError.set(null);
    this.notice.set(null);
    this.assignFieldErrors.set({});
    this.transferFieldErrors.set({});
  }
}
