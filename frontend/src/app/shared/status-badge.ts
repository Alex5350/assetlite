import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { AssetStatus } from '../models';

/** Status → badge CSS class (full literal class names so Tailwind keeps them). */
const STATUS_CLASSES: Record<AssetStatus, string> = {
  InStock: 'badge-in-stock',
  Assigned: 'badge-assigned',
  Maintenance: 'badge-maintenance',
  Retired: 'badge-retired',
  Disposed: 'badge-disposed',
};

/** Friendly display labels for asset statuses. */
export const STATUS_LABELS: Record<AssetStatus, string> = {
  InStock: 'In stock',
  Assigned: 'Assigned',
  Maintenance: 'Maintenance',
  Retired: 'Retired',
  Disposed: 'Disposed',
};

/**
 * Colored lifecycle badge: emerald = InStock, sky = Assigned,
 * amber = Maintenance, slate = Retired, rose = Disposed.
 */
@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="badge" [class]="badgeClass()">
      <span class="badge-dot" aria-hidden="true"></span>
      {{ label() }}
    </span>
  `,
})
export class StatusBadge {
  /** Lifecycle status to render; unknown values fall back to a neutral badge. */
  readonly status = input.required<string>();

  readonly badgeClass = computed(() => STATUS_CLASSES[this.status() as AssetStatus] ?? 'badge-neutral');
  readonly label = computed(() => STATUS_LABELS[this.status() as AssetStatus] ?? this.status());
}
