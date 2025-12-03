import { Component, OnInit, OnDestroy, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

/**
 * Navigation destination interface
 */
export interface NavigationDestination {
  icon: string;
  label: string;
  route: string;
  badge?: number;
  disabled?: boolean;
}

/**
 * Global Navigation Rail Component for myIoTGrid
 * Material Design 3 compliant navigation rail
 *
 * Features:
 * - Main navigation destinations (Dashboard, Sensoren, Hubs, Warnungen, Einstellungen)
 * - User profile menu with logout
 * - Badge support for alerts
 * - Responsive design (hidden on mobile <768px)
 * - Collapsible/Expandable
 *
 * Dimensions (MD3 Spec):
 * - Collapsed Width: 72px (icons only)
 * - Expanded Width: 256px (icons + labels)
 * - Destination Height: 56px (collapsed), 48px (expanded)
 */
@Component({
  selector: 'myiotgrid-navigation-rail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatBadgeModule,
    MatTooltipModule,
    MatDividerModule
  ],
  templateUrl: './navigation-rail.component.html',
  styleUrl: './navigation-rail.component.scss'
})
export class NavigationRailComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private routerSubscription?: Subscription;

  /**
   * Current route signal
   */
  private currentRoute = signal<string>('');

  /**
   * Collapsed state for navigation rail
   * - Collapsed: 72px width, icons only (default)
   * - Expanded: 256px width, icons + labels horizontally
   */
  public isCollapsed = signal<boolean>(true);

  /**
   * User display name (hardcoded for now, can be connected to AuthService)
   */
  public readonly userName = signal<string>('Hub Admin');

  /**
   * User initials for avatar
   */
  public readonly userInitials = computed(() => {
    const name = this.userName();
    if (!name) return 'HA';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  });

  /**
   * Main navigation destinations for myIoTGrid Hub
   * Updated for v3.0 Two-Tier Model (Matter-konform):
   * - Nodes = ESP32/LoRa32 Hardware-Geräte
   * - Sensors = Physische Sensor-Instanzen mit Hardware-Konfiguration
   */
  private mainDestinations: NavigationDestination[] = [
    { icon: 'dashboard', label: 'Dashboard', route: '/dashboard', disabled: false },
    { icon: 'router', label: 'Nodes', route: '/nodes', disabled: false },
    { icon: 'memory', label: 'Sensoren', route: '/sensors', disabled: false },
    { icon: 'warning', label: 'Warnungen', route: '/alerts', disabled: false }
  ];

  /**
   * Settings destination (shown at bottom, above profile)
   * Routes to /hubs which contains Hub settings and system info
   */
  private settingsDestination: NavigationDestination[] = [
    { icon: 'settings', label: 'Einstellungen', route: '/hubs', disabled: false }
  ];

  /**
   * Computed main destinations
   */
  public readonly destinations = computed(() => this.mainDestinations);

  /**
   * Computed settings destinations
   */
  public readonly globalDestinations = computed(() => this.settingsDestination);

  /**
   * Alert badge count (can be connected to AlertService)
   */
  public readonly alertBadge = signal<number>(0);

  ngOnInit(): void {
    // Set initial route
    this.currentRoute.set(this.router.url);

    // Subscribe to route changes
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.currentRoute.set(this.router.url);
      });

    // Set initial rail width CSS variable
    this.updateRailWidthVariable();
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }

  /**
   * Get user initials for avatar
   */
  public getInitialsAvatar(): string {
    return this.userInitials();
  }

  /**
   * Open user profile / settings
   */
  public openProfile(): void {
    this.router.navigate(['/hubs']);
  }

  /**
   * Logout user (placeholder - can be connected to AuthService)
   */
  public async logout(): Promise<void> {
    // For Hub Frontend, logout could redirect to login or just show a message
    console.log('Logout clicked');
  }

  /**
   * Toggle collapsed state of navigation rail
   */
  public toggleCollapsed(): void {
    this.isCollapsed.update(value => !value);
    this.updateRailWidthVariable();
  }

  /**
   * Update CSS custom property for rail width on document root
   */
  private updateRailWidthVariable(): void {
    const width = this.isCollapsed() ? '72px' : '256px';
    document.documentElement.style.setProperty('--global-rail-width', width);
  }

  /**
   * Get badge for a destination (for alerts)
   */
  public getBadgeForDestination(route: string): number | null {
    if (route === '/alerts') {
      const count = this.alertBadge();
      return count > 0 ? count : null;
    }
    return null;
  }
}
