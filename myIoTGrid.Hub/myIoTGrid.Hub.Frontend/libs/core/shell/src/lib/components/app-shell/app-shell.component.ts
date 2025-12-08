import { Component, inject, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NavigationRailComponent, BottomNavComponent } from '@myiotgrid/core/navigation';
import { LayoutService } from '../../services/layout.service';

/**
 * AppShellComponent - Main application shell with responsive navigation
 *
 * Adapts between:
 * - Desktop/Tablet: Side navigation rail
 * - Mobile: Bottom navigation bar
 *
 * Uses LayoutService for breakpoint detection.
 */
@Component({
  selector: 'myiotgrid-app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, NavigationRailComponent, BottomNavComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent {
  private readonly layout = inject(LayoutService);

  /** Whether to show navigation (can be disabled for setup wizard etc.) */
  showNavigation = input<boolean>(true);

  /** Reactive layout signals from LayoutService */
  readonly isMobile = this.layout.isMobile;
  readonly isTablet = this.layout.isTablet;
  readonly isDesktop = this.layout.isDesktop;
  readonly showSideNav = this.layout.showSideNav;
  readonly showBottomNav = this.layout.showBottomNav;
  readonly layoutMode = this.layout.layoutMode;

  /** Content padding based on device */
  readonly contentPadding = this.layout.contentPadding;

  /** Computed: should show any navigation */
  readonly shouldShowNav = computed(() => this.showNavigation());

  /** Computed: should show side navigation */
  readonly shouldShowSideNav = computed(() =>
    this.showNavigation() && this.showSideNav()
  );

  /** Computed: should show bottom navigation */
  readonly shouldShowBottomNav = computed(() =>
    this.showNavigation() && this.showBottomNav()
  );
}
