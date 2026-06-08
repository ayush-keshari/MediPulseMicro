import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap, timeout } from 'rxjs/operators';
import {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
  UpdateRoleRequest,
  UpdateUserRequest,
  UserDto,
} from './auth.models';
import { getRoleDashboardRoute } from '../../shared/extensions/app.extensions';

const API_BASE           = '/api';
const TOKEN_KEY          = 'medipulse_token';
const USER_KEY           = 'medipulse_user';
const REQUEST_TIMEOUT_MS = 8_000;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(
    this.loadStoredUser()
  );

  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  get currentUser(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  get isAuthenticated(): boolean {
    return !!this.getToken() && !!this.currentUserSubject.value;
  }

  get isAdmin(): boolean {
    return this.currentUserSubject.value?.role === 'Admin';
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE}/auth/login`, request).pipe(
      timeout(REQUEST_TIMEOUT_MS),
      tap((response) => {
        localStorage.setItem(TOKEN_KEY, response.token);
        const user: CurrentUser = {
          name:  response.name,
          email: response.email,
          role:  response.role,
        };
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        this.currentUserSubject.next(user);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  navigateAfterLogin(role: string): void {
    if (role === 'Unassigned') {
      this.router.navigate(['/pending-approval']);
      return;
    }
    this.router.navigate([getRoleDashboardRoute(role)]);
  }

  // ── Admin user management ──

  getUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${API_BASE}/users`).pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  getUserById(id: number): Observable<UserDto> {
    return this.http.get<UserDto>(`${API_BASE}/users/${id}`).pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  updateUserRole(id: number, request: UpdateRoleRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE}/users/${id}/role`, request).pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  // Admin full-profile edit — PUT /api/users/{id} returns the updated UserDto
  // so the caller can refresh its row without re-fetching the whole list.
  updateUser(id: number, request: UpdateUserRequest): Observable<UserDto> {
    return this.http.put<UserDto>(`${API_BASE}/users/${id}`, request).pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/users/${id}`).pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  register(request: RegisterRequest): Observable<UserDto> {
    return this.http.post<UserDto>(`${API_BASE}/auth/register`, request).pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  private loadStoredUser(): CurrentUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as CurrentUser) : null;
    } catch {
      return null;
    }
  }
}
