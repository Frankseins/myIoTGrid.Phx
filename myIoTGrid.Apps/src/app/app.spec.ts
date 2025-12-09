import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { App } from './app';
import { SignalRService } from '@myiotgrid/shared/data-access';
import { AuthService } from '@myiotgrid/core/auth';
import { signal } from '@angular/core';

// Mock SignalR Service
const mockSignalRService = {
  startConnection: jest.fn().mockResolvedValue(undefined),
  stopConnection: jest.fn().mockResolvedValue(undefined),
  connectionState: signal('disconnected'),
  lastError: signal(null),
  onNewReading: jest.fn(),
  onNodeStatusChanged: jest.fn(),
  onAlertReceived: jest.fn(),
};

// Mock Auth Service
const mockAuthService = {
  isAuthenticated: signal(false),
  currentUser: signal(null),
  login: jest.fn(),
  logout: jest.fn(),
};

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App, RouterTestingModule],
      providers: [
        { provide: SignalRService, useValue: mockSignalRService },
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should call SignalR startConnection on init', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(mockSignalRService.startConnection).toHaveBeenCalled();
  });

  it('should set isInitializing to false after init', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.isInitializing()).toBe(false);
  });

  it('should handle SignalR connection error gracefully', async () => {
    mockSignalRService.startConnection.mockRejectedValueOnce(new Error('Connection failed'));
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation();

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(consoleSpy).toHaveBeenCalledWith(
      'Failed to start SignalR connection:',
      expect.any(Error)
    );
    expect(fixture.componentInstance.isInitializing()).toBe(false);

    consoleSpy.mockRestore();
  });
});
