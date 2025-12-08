import { Component, OnInit, OnDestroy, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatRippleModule } from '@angular/material/core';
import { MatBadgeModule } from '@angular/material/badge';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { NavigationDestination } from '../navigation-rail/navigation-rail.component';

/**
 * Bottom Navigation Component for Mobile Devices
 *
 * Material Design 3 compliant bottom navigation bar.
 * Only visible on mobile devices (< 768px).
 *
 * Features:
 * - 4-5 navigation destinations with icons and labels
 * - Touch-friendly targets (min 48x48px)
 * - Active state with primary color
 * - Badge support for alerts
 * - Safe area insets for iPhone notch/home indicator
 *
 * @see https://m3.material.io/components/navigation-bar/overview
 */
@Component({
  selector: 'myiotgrid-bottom-nav',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatRippleModule,
    MatBadgeModule
  ],
  templateUrl: './bottom-nav.component.html',
  styleUrl: './bottom-nav.component.scss'
})
export class BottomNavComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private routerSubscription?: Subscription;

  /**
   * Current route signal
   */
  private currentRoute = signal<string>('');

  /**
   * Navigation destinations for mobile
   * Limited to 4-5 items for optimal mobile UX
   */
  readonly navItems: NavigationDestination[] = [
    { icon: 'dashboard', label: 'Dashboard', route: '/dashboard' },
    { icon: 'router', label: 'Nodes', route: '/nodes' },
    { icon: 'memory', label: 'Sensoren', route: '/sensors' },
    { icon: 'warning', label: 'Warnungen', route: '/alerts' },
    { icon: 'settings', label: 'Settings', route: '/hubs' }
  ];

  /**
   * Alert badge count (can be connected to AlertService)
   */
  readonly alertBadge = signal<number>(0);

  ngOnInit(): void {
    // Set initial route
    this.currentRoute.set(this.router.url);

    // Subscribe to route changes
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.currentRoute.set(this.router.url);
      });
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }

  /**
   * Check if a route is currently active
   */
  isActive(route: string): boolean {
    const current = this.currentRoute();
    return current === route || current.startsWith(route + '/');
  }

  /**
   * Get badge for a destination
   */
  getBadgeForDestination(route: string): number | null {
    if (route === '/alerts') {
      const count = this.alertBadge();
      return count > 0 ? count : null;
    }
    return null;
  }

  /**
   * Navigate to a route
   */
  navigateTo(route: string): void {
    this.router.navigate([route]);
  }
}
