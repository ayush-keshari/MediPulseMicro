// ── Items ─────────────────────────────────────────────────────────────────
export interface ItemResponse {
  itemId: number;
  itemCode: string;
  name: string;
  category: string;
  unit: string;
  storageRequirement: string;
  safetyStock: number;
  totalStock: number;
}

export interface CreateItemRequest {
  itemCode: string;
  name: string;
  category: string;
  unit: string;
  storageRequirement: string;
  safetyStock: number;
}

export interface UpdateItemRequest {
  name?: string;
  category?: string;
  unit?: string;
  storageRequirement?: string;
  safetyStock?: number;
}

// ── Facility stock summary (itemId → available qty) ──────────────────────
export interface FacilityStockDto {
  itemId: number;
  availableQty: number;
}

// ── Stock Positions ───────────────────────────────────────────────────────
export interface PositionResponse {
  positionId: number;
  itemId: number;
  itemName: string;
  itemCode: string;
  lotId: string;
  expiryDate: string;
  quantity: number;
  facilityId: number;
  storageZoneId: number;
  safetyStock: number;
  isExpired: boolean;
  isExpiringSoon: boolean;
  isBelowSafetyStock: boolean;
}

export interface CreatePositionRequest {
  itemId: number;
  lotId: string;
  expiryDate: string;
  quantity: number;
  facilityId: number;
  storageZoneId: number;
  safetyStock: number;
}

export interface UpdatePositionRequest {
  quantity?: number;
  facilityId?: number;
  storageZoneId?: number;
  safetyStock?: number;
  expiryDate?: string;
}

// ── Exception Events ──────────────────────────────────────────────────────
export interface ExceptionEventDto {
  exceptionId: number;
  type: string;
  referenceType: string;
  referenceId: number;
  itemId?: number;
  itemName?: string;
  facilityId?: number;
  lotId?: string;
  severity: string;
  status: string;
  detectedDate: string;
  actions: RecallActionDto[];
}

export interface CreateExceptionRequest {
  type: string;
  referenceType: string;
  referenceId: number;
  itemId?: number;
  itemName?: string;
  facilityId?: number;
  lotId?: string;
  severity: string;
}

export interface UpdateExceptionStatusRequest {
  status: string;
}

export interface DetectExceptionsResult {
  stockoutCount: number;
  expiryCount: number;
  totalCreated: number;
}

// ── Recall Actions ────────────────────────────────────────────────────────
export interface RecallActionDto {
  recallActionId: number;
  exceptionId: number;
  ownerId: string;
  actionDescription: string;
  dueDate: string;
  status: string;
}

export interface CreateRecallActionRequest {
  exceptionId: number;
  ownerId: string;
  actionDescription: string;
  dueDate: string;
}

export interface UpdateRecallActionRequest {
  actionDescription?: string;
  dueDate?: string;
  status?: string;
}

// ── Replenishment ─────────────────────────────────────────────────────────
export interface ForecastDto {
  forecastId: number;
  itemId: number;
  facilityId: number;
  period: string;
  forecastQuantity: number;
  generatedDate: string;
}

export interface ReplenishmentPlanDto {
  planId: number;
  itemId: number;
  facilityId: number;
  suggestedOrderQty: number;
  priority: string;
  status: string;
  generatedDate: string;
}

export interface UpdatePlanStatusRequest {
  status: string;
}

export interface GenerateReplenishmentResult {
  plansCreated: number;
  forecastsCreated: number;
  facilityId: number;
}
