import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';
import {
  ItemResponse, CreateItemRequest, UpdateItemRequest,
  FacilityStockDto,
  PositionResponse, CreatePositionRequest, UpdatePositionRequest,
  ExceptionEventDto, CreateExceptionRequest, UpdateExceptionStatusRequest, DetectExceptionsResult,
  RecallActionDto, CreateRecallActionRequest, UpdateRecallActionRequest,
  ForecastDto, ReplenishmentPlanDto, UpdatePlanStatusRequest, GenerateReplenishmentResult,
} from './inventory.models';

const BASE = '/api';
const T = 10_000;

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private http: HttpClient) {}

  // ── Items ────────────────────────────────────────────────────────────────
  getItems(): Observable<ItemResponse[]> { return this.http.get<ItemResponse[]>(`${BASE}/items`).pipe(timeout(T)); }
  getItem(id: number): Observable<ItemResponse> { return this.http.get<ItemResponse>(`${BASE}/items/${id}`).pipe(timeout(T)); }
  createItem(req: CreateItemRequest): Observable<ItemResponse> { return this.http.post<ItemResponse>(`${BASE}/items`, req).pipe(timeout(T)); }
  updateItem(id: number, req: UpdateItemRequest): Observable<ItemResponse> { return this.http.put<ItemResponse>(`${BASE}/items/${id}`, req).pipe(timeout(T)); }
  deleteItem(id: number): Observable<void> { return this.http.delete<void>(`${BASE}/items/${id}`).pipe(timeout(T)); }

  // ── Stock Positions ──────────────────────────────────────────────────────
  getFacilitiesByItem(itemId: number): Observable<number[]> { return this.http.get<number[]>(`${BASE}/inventory/item/${itemId}/facilities`).pipe(timeout(T)); }
  getItemsByFacility(facilityId: number): Observable<number[]> { return this.http.get<number[]>(`${BASE}/inventory/facility/${facilityId}/items`).pipe(timeout(T)); }
  getFacilityStock(facilityId: number): Observable<FacilityStockDto[]> { return this.http.get<FacilityStockDto[]>(`${BASE}/inventory/facility/${facilityId}/stock`).pipe(timeout(T)); }

  getPositions(facilityId?: number, itemId?: number): Observable<PositionResponse[]> {
    if (facilityId) return this.http.get<PositionResponse[]>(`${BASE}/inventory`).pipe(timeout(T));
    if (itemId)     return this.http.get<PositionResponse[]>(`${BASE}/inventory/item/${itemId}`).pipe(timeout(T));
    return this.http.get<PositionResponse[]>(`${BASE}/inventory`).pipe(timeout(T));
  }
  getPosition(id: number): Observable<PositionResponse> { return this.http.get<PositionResponse>(`${BASE}/inventory/${id}`).pipe(timeout(T)); }
  createPosition(req: CreatePositionRequest): Observable<PositionResponse> { return this.http.post<PositionResponse>(`${BASE}/inventory`, req).pipe(timeout(T)); }
  updatePosition(id: number, req: UpdatePositionRequest): Observable<PositionResponse> { return this.http.put<PositionResponse>(`${BASE}/inventory/${id}`, req).pipe(timeout(T)); }
  deletePosition(id: number): Observable<void> { return this.http.delete<void>(`${BASE}/inventory/${id}`).pipe(timeout(T)); }

  // ── Exceptions ───────────────────────────────────────────────────────────
  getExceptions(): Observable<ExceptionEventDto[]> { return this.http.get<ExceptionEventDto[]>(`${BASE}/exceptions`).pipe(timeout(T)); }
  getException(id: number): Observable<ExceptionEventDto> { return this.http.get<ExceptionEventDto>(`${BASE}/exceptions/${id}`).pipe(timeout(T)); }
  createException(req: CreateExceptionRequest): Observable<ExceptionEventDto> { return this.http.post<ExceptionEventDto>(`${BASE}/exceptions`, req).pipe(timeout(T)); }
  updateExceptionStatus(id: number, req: UpdateExceptionStatusRequest): Observable<ExceptionEventDto> { return this.http.patch<ExceptionEventDto>(`${BASE}/exceptions/${id}/status`, req).pipe(timeout(T)); }
  deleteException(id: number): Observable<void> { return this.http.delete<void>(`${BASE}/exceptions/${id}`).pipe(timeout(T)); }
  detectExceptions(facilityId: number, expiryThresholdDays = 30): Observable<DetectExceptionsResult> {
    const params = new HttpParams().set('facilityId', facilityId).set('expiryThresholdDays', expiryThresholdDays);
    return this.http.post<DetectExceptionsResult>(`${BASE}/exceptions/detect`, null, { params }).pipe(timeout(T));
  }

  // ── Recall Actions ───────────────────────────────────────────────────────
  getRecallActions(exceptionId?: number): Observable<RecallActionDto[]> {
    const params = exceptionId ? new HttpParams().set('exceptionId', exceptionId) : undefined;
    return this.http.get<RecallActionDto[]>(`${BASE}/recallactions`, { params }).pipe(timeout(T));
  }
  createRecallAction(req: CreateRecallActionRequest): Observable<RecallActionDto> { return this.http.post<RecallActionDto>(`${BASE}/recallactions`, req).pipe(timeout(T)); }
  updateRecallAction(id: number, req: UpdateRecallActionRequest): Observable<RecallActionDto> { return this.http.put<RecallActionDto>(`${BASE}/recallactions/${id}`, req).pipe(timeout(T)); }
  deleteRecallAction(id: number): Observable<void> { return this.http.delete<void>(`${BASE}/recallactions/${id}`).pipe(timeout(T)); }

  // ── Replenishment ────────────────────────────────────────────────────────
  getForecasts(): Observable<ForecastDto[]> { return this.http.get<ForecastDto[]>(`${BASE}/replenishment/forecasts`).pipe(timeout(T)); }
  getPlans(): Observable<ReplenishmentPlanDto[]> { return this.http.get<ReplenishmentPlanDto[]>(`${BASE}/replenishment/plans`).pipe(timeout(T)); }
  updatePlanStatus(id: number, req: UpdatePlanStatusRequest): Observable<ReplenishmentPlanDto> { return this.http.patch<ReplenishmentPlanDto>(`${BASE}/replenishment/plans/${id}/status`, req).pipe(timeout(T)); }
  deletePlan(id: number): Observable<void> { return this.http.delete<void>(`${BASE}/replenishment/plans/${id}`).pipe(timeout(T)); }
  generateReplenishment(facilityId: number): Observable<GenerateReplenishmentResult> {
    const params = new HttpParams().set('facilityId', facilityId);
    return this.http.post<GenerateReplenishmentResult>(`${BASE}/replenishment/generate`, null, { params }).pipe(timeout(T));
  }
}
