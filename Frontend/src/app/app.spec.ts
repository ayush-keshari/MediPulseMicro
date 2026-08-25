import { TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { App } from './app';
import { AuthService } from './services/auth/auth.service';

class MockAuthService {
  isAuthenticated = false;
}

describe('App', () => {
  let fixture: any;
  let authService: MockAuthService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        App,
        RouterModule.forRoot([])
      ],
      providers: [
        { provide: AuthService, useClass: MockAuthService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    authService = TestBed.inject(AuthService);
  });

  it('should create the app', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should have showLayout false when not authenticated', () => {
    // Arrange & Act
    const app = fixture.componentInstance;

    // Assert
    expect(app.showLayout).toBeFalsy();
  });

  it('should have showLayout false when on login page even if authenticated', () => {
    // Arrange
    authService.isAuthenticated = true;
    // Simulate being on login page
    fixture.componentInstance.updateLayout('/login');
    fixture.detectChanges();

    // Assert
    expect(fixture.componentInstance.showLayout).toBeFalsy();
  });
});
