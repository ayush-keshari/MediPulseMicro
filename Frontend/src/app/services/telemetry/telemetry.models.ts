export interface SensorDeviceDto {
  sensorId: number;
  deviceName: string;
  deviceType: string;
  assignedTo: string;
  assignedEntityId?: number;
  status: string;
}
export interface CreateSensorDeviceRequest { deviceName: string; deviceType: string; assignedTo: string; assignedEntityId?: number; status?: string; }
export interface UpdateSensorDeviceRequest { deviceName: string; deviceType: string; assignedTo: string; assignedEntityId?: number; status: string; }

export interface TelemetryRecordDto {
  telemetryId: number;
  sensorId: number;
  deviceType: string;
  timestamp: string;
  temperature?: number;
  humidity?: number;
  location?: string;
  isExcursion: boolean;
}
export interface CreateTelemetryRecordRequest { sensorId: number; timestamp?: string; temperature?: number; humidity?: number; location?: string; }
export interface UpdateTelemetryRecordRequest { timestamp: string; temperature?: number; humidity?: number; location?: string; }
