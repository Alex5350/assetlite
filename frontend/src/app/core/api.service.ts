import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AssignAssetRequest,
  AssetDetailDto,
  AssetLabel,
  AssetListItemDto,
  AssetSearchFilters,
  CategoryDto,
  CreateOfficeRequest,
  InventorySummaryDto,
  MoveOfficeRequest,
  OfficeDto,
  OfficeTreeNodeDto,
  PagedResult,
  RegisterAssetRequest,
  SaveCategoryRequest,
  TransferAssetRequest,
  UpdateAssetRequest,
} from '../models';

/**
 * Typed gateway to the AssetLite API.
 *
 * Read strategy: plain `HttpClient` observables composed with `toSignal()` in
 * components. Mutations return observables subscribed explicitly; mutating
 * endpoints answer 204/201 or ProblemDetails (400/404/409) — parse those with
 * `parseProblemDetails` from shared/api-error.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl.replace(/\/+$/, '');

  /** Inventory summary for the dashboard (GET /api/reports/summary). */
  getInventorySummary(): Observable<InventorySummaryDto> {
    return this.http.get<InventorySummaryDto>(this.url('/api/reports/summary'));
  }

  /** Paged asset search (GET /api/assets). Server-driven paging + filtering. */
  searchAssets(filters: AssetSearchFilters = {}): Observable<PagedResult<AssetListItemDto>> {
    let params = new HttpParams({ fromObject: { page: String(filters.page ?? 1), pageSize: String(filters.pageSize ?? 20) } });
    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }
    if (filters.officeId) {
      params = params.set('officeId', filters.officeId);
    }
    if (filters.officeId && filters.includeDescendants) {
      params = params.set('includeDescendants', 'true');
    }
    if (filters.categoryId) {
      params = params.set('categoryId', filters.categoryId);
    }
    if (filters.status) {
      params = params.set('status', filters.status);
    }
    return this.http.get<PagedResult<AssetListItemDto>>(this.url('/api/assets'), { params });
  }

  /** Full asset detail incl. assignment history (GET /api/assets/{tag}). */
  getAssetByTag(tag: string): Observable<AssetDetailDto> {
    return this.http.get<AssetDetailDto>(this.url(`/api/assets/${encodeURIComponent(tag)}`));
  }

  /** Printable label artwork for an asset tag (GET /api/assets/{tag}/label). */
  getAssetLabel(tag: string): Observable<AssetLabel> {
    return this.http.get<AssetLabel>(this.url(`/api/assets/${encodeURIComponent(tag)}/label`));
  }

  /** Registers a new asset; the API allocates the sequential tag (POST /api/assets → 201). */
  registerAsset(request: RegisterAssetRequest): Observable<AssetDetailDto> {
    return this.http.post<AssetDetailDto>(this.url('/api/assets'), request);
  }

  /** Replaces an asset's descriptive details (PUT /api/assets/{tag} → 200). */
  updateAsset(tag: string, request: UpdateAssetRequest): Observable<AssetDetailDto> {
    return this.http.put<AssetDetailDto>(this.url(`/api/assets/${encodeURIComponent(tag)}`), request);
  }

  /** Assigns (or reassigns) an asset to a person (POST /api/assets/{tag}/assign → 204). */
  assignAsset(tag: string, request: AssignAssetRequest): Observable<void> {
    return this.http.post<void>(this.url(`/api/assets/${encodeURIComponent(tag)}/assign`), request);
  }

  /** Returns an assigned asset to stock (POST /api/assets/{tag}/return → 204). */
  returnAsset(tag: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/assets/${encodeURIComponent(tag)}/return`), null);
  }

  /** Sends an in-stock or assigned asset to maintenance (POST /api/assets/{tag}/maintenance → 204). */
  startMaintenance(tag: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/assets/${encodeURIComponent(tag)}/maintenance`), null);
  }

  /** Returns a maintenance asset to stock (POST /api/assets/{tag}/maintenance/resume → 204). */
  resumeMaintenance(tag: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/assets/${encodeURIComponent(tag)}/maintenance/resume`), null);
  }

  /** Retires an active asset (POST /api/assets/{tag}/retire → 204). */
  retireAsset(tag: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/assets/${encodeURIComponent(tag)}/retire`), null);
  }

  /** Disposes a retired asset — permanent (POST /api/assets/{tag}/dispose → 204). */
  disposeAsset(tag: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/assets/${encodeURIComponent(tag)}/dispose`), null);
  }

  /** Transfers an asset to another office (POST /api/assets/{tag}/transfer → 204). */
  transferAsset(tag: string, request: TransferAssetRequest): Observable<void> {
    return this.http.post<void>(this.url(`/api/assets/${encodeURIComponent(tag)}/transfer`), request);
  }

  /** Flat office list (GET /api/offices). */
  getOffices(): Observable<OfficeDto[]> {
    return this.http.get<OfficeDto[]>(this.url('/api/offices'));
  }

  /** Office hierarchy tree, rooted at the single HQ office (GET /api/offices/tree). */
  getOfficeTree(): Observable<OfficeTreeNodeDto> {
    return this.http.get<OfficeTreeNodeDto>(this.url('/api/offices/tree'));
  }

  /** Creates an office (POST /api/offices → 201). */
  createOffice(request: CreateOfficeRequest): Observable<OfficeDto> {
    return this.http.post<OfficeDto>(this.url('/api/offices'), request);
  }

  /** Re-parents an office (POST /api/offices/{id}/move → 204). */
  moveOffice(id: string, request: MoveOfficeRequest): Observable<void> {
    return this.http.post<void>(this.url(`/api/offices/${encodeURIComponent(id)}/move`), request);
  }

  /** All asset categories (GET /api/categories). */
  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(this.url('/api/categories'));
  }

  /** Creates a category (POST /api/categories → 201). */
  createCategory(request: SaveCategoryRequest): Observable<CategoryDto> {
    return this.http.post<CategoryDto>(this.url('/api/categories'), request);
  }

  /** Updates a category's editable fields (PUT /api/categories/{id} → 200). */
  updateCategory(id: string, request: SaveCategoryRequest): Observable<CategoryDto> {
    return this.http.put<CategoryDto>(this.url(`/api/categories/${encodeURIComponent(id)}`), request);
  }

  /** Absolute URL of the Excel asset-register export (direct download link target). */
  registerExcelUrl(): string {
    return this.url('/api/reports/register/excel');
  }

  /** Absolute URL of the PDF asset-register export (direct download link target). */
  registerPdfUrl(): string {
    return this.url('/api/reports/register/pdf');
  }

  private url(path: string): string {
    return `${this.baseUrl}${path}`;
  }
}
