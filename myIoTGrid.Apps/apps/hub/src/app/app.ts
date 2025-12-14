import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { NavigationRailComponent, BottomNavComponent, MobileSidebarComponent } from '@myiotgrid/core/navigation';
import { SignalRService } from '@myiotgrid/shared/data-access';
import { AuthService } from '@myiotgrid/core/auth';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, NavigationRailComponent, BottomNavComponent, MobileSidebarComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly signalRService = inject(SignalRService);
  private readonly authService = inject(AuthService);

  readonly isInitializing = signal(true);
  private readonly currentUrl = signal('/');

  /** Sidebar open state (mobile only) */
  readonly isSidebarOpen = signal(false);

  /**
   * True if device is a mobile/touch device.
   * Detection based on:
   * - pointer: coarse (touch input, not precise mouse)
   * - hover: none (no hover capability)
   * - maxTouchPoints > 0 (has touch screen)
   *
   * This is more reliable than screen width, as it detects actual
   * device capabilities rather than viewport size.
   */
  readonly isMobile = signal(false);

  readonly showNavigation = computed(() => {
    const url = this.currentUrl();
    return !this.isAuthRoute(url);
  });

  async ngOnInit(): Promise<void> {
    // Detect mobile device based on capabilities (not screen size)
    this.detectMobileDevice();

    // Subscribe to route changes
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.currentUrl.set(this.router.url);
      });

    this.currentUrl.set(this.router.url || '/');

    // Initialize SignalR connection
    try {
      await this.signalRService.startConnection();
    } catch (error) {
      console.error('Failed to start SignalR connection:', error);
    }

    this.isInitializing.set(false);
  }

  ngOnDestroy(): void {
    // Cleanup if needed
  }

  /**
   * Detects if the device is a PHONE (not tablet, not desktop).
   *
   * Logic:
   * - Phone = Touch device + small screen (< 768px)
   * - Tablet = Touch device + larger screen (>= 768px) → Desktop UI
   * - Desktop = No touch or larger screen → Desktop UI
   *
   * This ensures:
   * - iPhone, Android phones → Bottom Navigation
   * - iPad, Android tablets → Navigation Rail (Desktop)
   * - Desktop/Laptop → Navigation Rail (Desktop)
   */
  private detectMobileDevice(): void {
    const hasCoarsePointer = window.matchMedia('(pointer: coarse)').matches;
    const hasTouchScreen = navigator.maxTouchPoints > 0;
    const isSmallScreen = window.innerWidth < 768;

    // Phone = Touch device WITH small screen
    // Tablets have touch but larger screens → should get Desktop UI
    const isPhone = (hasCoarsePointer || hasTouchScreen) && isSmallScreen;

    this.isMobile.set(isPhone);

    console.debug('Device detection:', {
      hasCoarsePointer,
      hasTouchScreen,
      screenWidth: window.innerWidth,
      isSmallScreen,
      isPhone
    });
  }

  private isAuthRoute(url: string): boolean {
    const basePath = url.split('?')[0];
    return basePath.startsWith('/auth') ||
           basePath.startsWith('/login');
  }

  /**
   * Open the mobile sidebar
   */
  openSidebar(): void {
    this.isSidebarOpen.set(true);
  }

  /**
   * Close the mobile sidebar
   */
  closeSidebar(): void {
    this.isSidebarOpen.set(false);
  }
}
