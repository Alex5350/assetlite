import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AssetDetailDto,
  AssetLabel,
  AssetListItemDto,
  AssetSearchFilters,
  CategoryDto,
  InventorySummaryDto,
  OfficeDto,
  OfficeTreeNodeDto,
  PagedResult,
} from '../models';

/**
 * Typed gateway to the AssetLite API.
 *
 * Read strategy: plain `HttpClient` observables composed with `toSignal()` in
 * components — the stable Angular path (`httpResource`/`resource` are still
 * marked experimental in Angular 21, so they are intentionally not used).
 * Mutations (built by follow-up agents) can add command methods here.
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
    if (filters.searchText?.trim()) {
      params = params.set('searchText', filters.searchText.trim());
    }
    if (filters.officeId) {
      params = params.set('officeId', filters.officeId);
    }
    if (filters.officeId && filters.includeDescendantOffices) {
      params = params.set('includeDescendantOffices', 'true');
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

  /** Flat office list (GET /api/offices). */
  getOffices(): Observable<OfficeDto[]> {
    return this.http.get<OfficeDto[]>(this.url('/api/offices'));
  }

  /** Office hierarchy tree (GET /api/offices/tree). */
  getOfficeTree(): Observable<OfficeTreeNodeDto[]> {
    return this.http.get<OfficeTreeNodeDto[]>(this.url('/api/offices/tree'));
  }

  /** All asset categories (GET /api/categories). */
  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(this.url('/api/categories'));
  }

  private url(path: string): string {
    return `${this.baseUrl}${path}`;
  }
}
