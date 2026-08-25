import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

describe('AuthService (Authentication-related behavior)', () => {
  let service: AuthService;
  let httpClient: jasmine.SpyObj<HttpClient>;
  let router: jasmine.SpyObj<Router>;

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
    const httpSpy = jasmine.createSpyObj('HttpClient', ['post', 'get', 'put', 'delete']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: HttpClient, useValue: httpSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpClient = TestBed.inject(HttpClient) as jasmine.SpyObj<HttpClient>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;

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
      expect(service.isAuthenticated).toBeFalse();
    });

    it('should return false when token exists but no user', () => {
      localStorage.setItem('medipulse_token', 'fake-token');
      expect(service.isAuthenticated).toBeFalse();
    });

    it('should return false when no token but user exists', () => {
      localStorage.setItem('medipulse_user', JSON.stringify(mockCurrentUser));
      expect(service.isAuthenticated).toBeFalse();
    });

    it('should return true when both token and user exist', () => {
      localStorage.setItem('medipulse_token', 'fake-token');
      localStorage.setItem('medipulse_user', JSON.stringify(mockCurrentUser));
      expect(service.isAuthenticated).toBeTrue();
    });
  });

  describe('isAdmin getter', () => {
    it('should return false when no current user', () => {
      expect(service.isAdmin).toBeFalse();
    });

    it('should return false when current user is not admin', () => {
      const nonAdminUser = { ...mockCurrentUser, role: 'Staff' };
      localStorage.setItem('medipulse_token', 'fake-token');
      localStorage.setItem('medipulse_user', JSON.stringify(nonAdminUser));
      expect(service.isAdmin).toBeFalse();
    });

    it('should return true when current user is admin', () => {
      localStorage.setItem('medipulse_token', 'fake-token');
      localStorage.setItem('medipulse_user', JSON.stringify(mockCurrentUser));
      expect(service.isAdmin).toBeTrue();
    });
  });

  describe('login() method', () => {
    const loginRequest = {
      email: 'test@example.com',
      password: 'password123'
    };

    it('should make HTTP POST request to login endpoint', () => {
      // Arrange
      httpClient.post.and.returnValue(of(mockLoginResponse));

      // Act
      service.login(loginRequest).subscribe();

      // Assert
      expect(httpClient.post).toHaveBeenCalledOnceWith(
        '/api/auth/login',
        loginRequest,
        jasmine.objectContaining({ timeout: 8000 })
      );
    });

    it('should store token and user in localStorage on successful login', (done) => {
      // Arrange
      httpClient.post.and.returnValue(of(mockLoginResponse));

      // Act
      service.login(loginRequest).subscribe({
        next: () => {
          // Assert
          expect(localStorage.getItem('medipulse_token')).toBe('fake-jwt-token');
          const storedUser = localStorage.getItem('medipulse_user');
          expect(storedUser).toBeTruthy();
          const parsedUser = JSON.parse(storedUser!);
          expect(parsedUser).toEqual(mockCurrentUser);
          done();
        }
      });
    });

    it('should update currentUserSubject on successful login', (done) => {
      // Arrange
      httpClient.post.and.returnValue(of(mockLoginResponse));

      // Act
      service.login(loginRequest).subscribe({
        next: () => {
          // Assert
          expect(service.currentUser).toEqual(mockCurrentUser);
          done();
        }
      });
    });

    it('should call timeout operator with correct duration', (done) => {
      // Arrange
      httpClient.post.and.returnValue(of(mockLoginResponse));

      // Act
      service.login(loginRequest).subscribe({
        complete: () => {
          // Assert
          expect(httpClient.post).toHaveBeenCalledWith(
            '/api/auth/login',
            loginRequest,
            jasmine.objectContaining({ timeout: 8000 })
          );
          done();
        }
      });
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
      expect(router.navigate).toHaveBeenCalledOnceWith(['/login']);
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
      expect(router.navigate).toHaveBeenCalledOnceWith(['/pending-approval']);
    });

    it('should navigate to dashboard for Admin role via extension', () => {
      // Arrange
      spyOn<any>(service, 'getRoleDashboardRoute').and.returnValue('/admin/dashboard');

      // Act
      service.navigateAfterLogin('Admin');

      // Assert
      expect(router.navigate).toHaveBeenCalledOnceWith(['/admin/dashboard']);
    });

    it('should navigate to dashboard for Staff role via extension', () => {
      // Arrange
      spyOn<any>(service, 'getRoleDashboardRoute').and.returnValue('/staff/dashboard');

      // Act
      service.navigateAfterLogin('Staff');

      // Assert
      expect(router.navigate).toHaveBeenCalledOnceWith(['/staff/dashboard']);
    });
  });

  // Helper import for of operator
  const { of } = jasmine;
});

// Note: For brevity, HTTP methods like getUsers(), register(), etc. are not tested here
// but would follow similar patterns in a complete test suite