// ── Transfer Orders ───────────────────────────────────────────────────────
export interface TransferOrderItemDto {
  transferOrderItemId: number;
  itemId: number;
  itemName: string;
  quantity: number;
  toStorageZoneId: number;
}

export interface TransferOrderDto {
  transferOrderId: number;
  fromFacilityId: number;
  fromFacilityName: string;
  toFacilityId: number;
  toFacilityName: string;
  requestedBy: string;
  requestedDate: string;
  status: string;
  items: TransferOrderItemDto[];
}

export interface TransferOrderItemRequest {
  itemId: number;
  itemName: string;
  quantity: number;
  toStorageZoneId: number;
}

export interface CreateTransferOrderRequest {
  fromFacilityId: number;
  fromFacilityName: string;
  toFacilityId: number;
  toFacilityName: string;
  requestedBy: string;
  items: TransferOrderItemRequest[];
}

export interface UpdateTransferStatusRequest {
  status: string;
}

// ── Consumption Records ───────────────────────────────────────────────────
export interface ConsumptionRecordDto {
  consumptionId: number;
  facilityId: number;
  wardId?: number;
  itemId: number;
  itemName: string;
  quantityConsumed: number;
  consumedDate: string;
  consumedBy: string;
}

export interface CreateConsumptionRequest {
  facilityId: number;
  wardId?: number;
  itemId: number;
  itemName: string;
  quantityConsumed: number;
  consumedDate: string;
  consumedBy: string;
}

export interface UpdateConsumptionRequest {
  quantityConsumed: number;
  consumedDate: string;
  consumedBy: string;
}
