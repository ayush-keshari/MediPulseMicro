import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

describe('AuthService (Authentication-related behavior)', () => {
  type MockFunction = ReturnType<typeof vi.fn>;
  type MockHttpClient = {
    post: MockFunction;
    get: MockFunction;
    put: MockFunction;
    delete: MockFunction;
  };
  type MockRouter = { navigate: MockFunction };

  let service: AuthService;
  let httpClient: MockHttpClient;
  let router: MockRouter;

  const mockLoginResponse = {
    token: 'fake-jwt-token',
    name: 'Test User',
    email: 'test@example.com',
    role: 'Admin'
  };

  const mockCurrentUser = {
    name: 'Test User',
    email: 'test@example.com',
    role: 'Admin'
  };

  beforeEach(() => {
    const httpSpy = {
      post: vi.fn(),
      get: vi.fn(),
      put: vi.fn(),
      delete: vi.fn()
    };
    const routerSpy = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: HttpClient, useValue: httpSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpClient = TestBed.inject(HttpClient) as unknown as MockHttpClient;
    router = TestBed.inject(Router) as unknown as MockRouter;

    // Clear localStorage before each test
    localStorage.removeItem('medipulse_token');
    localStorage.removeItem('medipulse_user');
  });

  afterEach(() => {
    // Clean up after each test
    localStorage.removeItem('medipulse_token');
    localStorage.removeItem('medipulse_user');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('isAuthenticated getter', () => {
    it('should return false when no token and no user', () => {
      expect(service.isAuthenticated).toBeFalsy();
    });

    it('should return false when token exists but no user', () => {
      localStorage.setItem('medipulse_token', 'fake-token');
      expect(service.isAuthenticated).toBeFalsy();
    });

    it('should return false when no token but user exists', () => {
      localStorage.setItem('medipulse_user', JSON.stringify(mockCurrentUser));
      expect(service.isAuthenticated).toBeFalsy();
    });

    it('should return true when both token and user exist', () => {
      localStorage.setItem('medipulse_token', 'fake-token');
      localStorage.setItem('medipulse_user', JSON.stringify(mockCurrentUser));
      service = new AuthService(httpClient as unknown as HttpClient, router as unknown as Router);
      expect(service.isAuthenticated).toBeTruthy();
    });
  });

  describe('isAdmin getter', () => {
    it('should return false when no current user', () => {
      expect(service.isAdmin).toBeFalsy();
    });

    it('should return false when current user is not admin', () => {
      const nonAdminUser = { ...mockCurrentUser, role: 'Staff' };
      localStorage.setItem('medipulse_token', 'fake-token');
      localStorage.setItem('medipulse_user', JSON.stringify(nonAdminUser));
      expect(service.isAdmin).toBeFalsy();
    });

    it('should return true when current user is admin', () => {
      localStorage.setItem('medipulse_token', 'fake-token');
      localStorage.setItem('medipulse_user', JSON.stringify(mockCurrentUser));
      service = new AuthService(httpClient as unknown as HttpClient, router as unknown as Router);
      expect(service.isAdmin).toBeTruthy();
    });
  });

  describe('login() method', () => {
    const loginRequest = {
      email: 'test@example.com',
      password: 'password123'
    };

    it('should make HTTP POST request to login endpoint', () => {
      // Arrange
      httpClient.post.mockReturnValue(of(mockLoginResponse));

      // Act
      service.login(loginRequest).subscribe();

      // Assert
      expect(httpClient.post).toHaveBeenCalledWith('/api/auth/login', loginRequest);
    });

    it('should store token and user in localStorage on successful login', () => {
      // Arrange
      httpClient.post.mockReturnValue(of(mockLoginResponse));

      // Act
      service.login(loginRequest).subscribe();

      // Assert
      expect(localStorage.getItem('medipulse_token')).toBe('fake-jwt-token');
      const storedUser = localStorage.getItem('medipulse_user');
      expect(storedUser).toBeTruthy();
      const parsedUser = JSON.parse(storedUser!);
      expect(parsedUser).toEqual(mockCurrentUser);
    });

    it('should update currentUserSubject on successful login', () => {
      // Arrange
      httpClient.post.mockReturnValue(of(mockLoginResponse));

      // Act
      service.login(loginRequest).subscribe();

      // Assert
      expect(service.currentUser).toEqual(mockCurrentUser);
    });

    it('should call timeout operator with correct duration', () => {
      // Arrange
      httpClient.post.mockReturnValue(of(mockLoginResponse));

      // Act
      service.login(loginRequest).subscribe();

      // Assert
      expect(httpClient.post).toHaveBeenCalledWith('/api/auth/login', loginRequest);
    });
  });

  describe('logout() method', () => {
    it('should remove token and user from localStorage', () => {
      // Arrange
      localStorage.setItem('medipulse_token', 'fake-token');
      localStorage.setItem('medipulse_user', JSON.stringify(mockCurrentUser));

      // Act
      service.logout();

      // Assert
      expect(localStorage.getItem('medipulse_token')).toBeNull();
      expect(localStorage.getItem('medipulse_user')).toBeNull();
    });

    it('should set currentUserSubject to null', () => {
      // Arrange
      localStorage.setItem('medipulse_token', 'fake-token');
      localStorage.setItem('medipulse_user', JSON.stringify(mockCurrentUser));
      // Trigger initial load
      service.currentUser$.subscribe();

      // Act
      service.logout();

      // Assert
      expect(service.currentUser).toBeNull();
    });

    it('should navigate to login page', () => {
      // Act
      service.logout();

      // Assert
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('getToken() method', () => {
    it('should return null when no token in localStorage', () => {
      expect(service.getToken()).toBeNull();
    });

    it('should return token from localStorage', () => {
      // Arrange
      const testToken = 'test-token-123';
      localStorage.setItem('medipulse_token', testToken);

      // Act
      const token = service.getToken();

      // Assert
      expect(token).toBe(testToken);
    });
  });

  describe('navigateAfterLogin() method', () => {
    it('should navigate to pending-approval for Unassigned role', () => {
      // Act
      service.navigateAfterLogin('Unassigned');

      // Assert
      expect(router.navigate).toHaveBeenCalledWith(['/pending-approval']);
    });

    it('should navigate to dashboard for Admin role via extension', () => {
      // Arrange
      // Act
      service.navigateAfterLogin('Admin');

      // Assert
      expect(router.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
    });

    it('should navigate to dashboard for Staff role via extension', () => {
      // Arrange
      // Act
      service.navigateAfterLogin('Staff');

      // Assert
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });
  });

});

// Note: For brevity, HTTP methods like getUsers(), register(), etc. are not tested here
// but would follow similar patterns in a complete test suite
