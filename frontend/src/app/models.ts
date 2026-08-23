/**
 * TypeScript mirrors of the backend API DTOs
 * (see the Dtos.cs files under src/AssetLite.Application).
 *
 * The API emits camelCase JSON. Strongly-typed ids (AssetId, OfficeId, …) are
 * Guids serialized as plain strings. Enums are serialized as their names
 * (JsonStringEnumConverter). DateOnly → "YYYY-MM-DD", DateTimeOffset → ISO-8601.
 */

/** Lifecycle status of an asset (AssetStatus enum). */
export type AssetStatus = 'InStock' | 'Assigned' | 'Maintenance' | 'Retired' | 'Disposed';

/** All asset lifecycle statuses, in canonical order. */
export const ASSET_STATUSES: readonly AssetStatus[] = [
  'InStock',
  'Assigned',
  'Maintenance',
  'Retired',
  'Disposed',
] as const;

/** Physical condition of an asset (AssetCondition enum). */
export type AssetCondition = 'New' | 'Good' | 'Fair' | 'Poor';

/** Compact asset row for lists and search results (AssetListItemDto). */
export interface AssetListItemDto {
  id: string;
  tag: string;
  name: string;
  status: AssetStatus;
  condition: AssetCondition;
  officeId: string;
  officeName: string | null;
  categoryId: string;
  categoryName: string | null;
  currentAssigneeName: string | null;
  /**
   * Frontend-friendly optional extensions: the C# AssetListItemDto currently
   * omits purchase info; when the API adds these fields the list table shows
   * them automatically (until then "—" is rendered).
   */
  purchaseDate?: string | null;
  purchaseCostAmount?: number | null;
}

/** A single assignment history entry (AssignmentDto). */
export interface AssignmentDto {
  id: string;
  assigneeName: string;
  assigneeEmail: string;
  assignedAtUtc: string;
  returnedAtUtc: string | null;
}

/** Detailed view of an asset (AssetDetailDto). */
export interface AssetDetailDto {
  id: string;
  tag: string;
  name: string;
  manufacturer: string | null;
  model: string | null;
  serialNumber: string | null;
  status: AssetStatus;
  condition: AssetCondition;
  purchaseDate: string | null;
  purchaseCostAmount: number | null;
  purchaseCostCurrency: string | null;
  notes: string | null;
  officeId: string;
  officeName: string | null;
  categoryId: string;
  categoryName: string | null;
  createdAtUtc: string;
  currentAssigneeName: string | null;
  currentAssigneeEmail: string | null;
  /** Assignment history, newest first. */
  assignments: AssignmentDto[];
}

/** Flat office representation (OfficeDto). */
export interface OfficeDto {
  id: string;
  name: string;
  code: string;
  parentOfficeId: string | null;
}

/** Node in the office hierarchy tree; children ordered by name (OfficeTreeNodeDto). */
export interface OfficeTreeNodeDto {
  id: string;
  name: string;
  code: string;
  parentOfficeId: string | null;
  children: OfficeTreeNodeDto[];
}

/** Asset category (CategoryDto). */
export interface CategoryDto {
  id: string;
  name: string;
  description: string | null;
  expectedLifespanMonths: number;
}

/** A page of results with pagination metadata (PagedResult&lt;T&gt;). */
export interface PagedResult<T> {
  items: T[];
  total: number;
  /** 1-based page number. */
  page: number;
  pageSize: number;
}

/** Total number of pages derived from a paged result (mirrors the C# computed property). */
export function totalPages(result: Pick<PagedResult<unknown>, 'total' | 'pageSize'>): number {
  return result.pageSize > 0 ? Math.ceil(result.total / result.pageSize) : 0;
}

/** Status counts and purchase value for a single office (OfficeSummaryDto). */
export interface OfficeSummaryDto {
  officeId: string;
  officeName: string;
  officeCode: string;
  totalAssets: number;
  inStockCount: number;
  assignedCount: number;
  maintenanceCount: number;
  retiredCount: number;
  disposedCount: number;
  totalPurchaseValue: number;
}

/** Status counts and purchase value for a single category (CategorySummaryDto). */
export interface CategorySummaryDto {
  categoryId: string;
  categoryName: string;
  totalAssets: number;
  inStockCount: number;
  assignedCount: number;
  maintenanceCount: number;
  retiredCount: number;
  disposedCount: number;
  totalPurchaseValue: number;
}

/** Inventory totals grouped by office and by category (InventorySummaryDto). */
export interface InventorySummaryDto {
  generatedAtUtc: string;
  totalAssets: number;
  totalPurchaseValue: number;
  /** Ordered by office name; includes offices with no assets. */
  offices: OfficeSummaryDto[];
  /** Ordered by category name; includes categories with no assets. */
  categories: CategorySummaryDto[];
}

/** Rendered label artifacts for one asset tag (AssetLabel). */
export interface AssetLabel {
  tag: string;
  labelText: string;
  barcodeSvg: string;
  qrSvg: string;
}

/** Filter + pagination criteria for GET /api/assets (SearchAssetsQuery). */
export interface AssetSearchFilters {
  searchText?: string;
  officeId?: string;
  includeDescendantOffices?: boolean;
  categoryId?: string;
  status?: AssetStatus;
  page?: number;
  pageSize?: number;
}

/** An office tree node flattened for `<select>` display, carrying its depth. */
export interface FlatOfficeOption {
  office: OfficeTreeNodeDto;
  depth: number;
}

/** Flattens an office tree into depth-annotated options, depth-first, children by name. */
export function flattenOfficeTree(nodes: readonly OfficeTreeNodeDto[], depth = 0): FlatOfficeOption[] {
  return nodes.flatMap((node) => [
    { office: node, depth },
    ...flattenOfficeTree(node.children, depth + 1),
  ]);
}
