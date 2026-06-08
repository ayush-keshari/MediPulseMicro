export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  name: string;
  email: string;
  role: string;
  expiresAt: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  phone?: string;
}

export interface UserDto {
  userId: number;
  name: string;
  email: string;
  role: string;
  phone?: string;
}

export interface UpdateRoleRequest {
  role: string;
}

// Admin "Edit Profile" — every field editable. password is optional;
// omit (or send empty) to keep the existing hash on the backend.
export interface UpdateUserRequest {
  name: string;
  email: string;
  role: string;
  phone?: string;
  password?: string;
}

export interface CurrentUser {
  name: string;
  email: string;
  role: string;
}

export interface ApiError {
  message: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class RequestTimeoutError extends Error {
  constructor() {
    super('Request timed out');
    this.name = 'RequestTimeoutError';
  }
}
