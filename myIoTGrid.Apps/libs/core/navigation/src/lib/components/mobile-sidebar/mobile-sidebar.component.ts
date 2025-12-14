import { Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatRippleModule } from '@angular/material/core';

/**
 * Mobile Sidebar Component
 *
 * Slide-in sidebar from the right for mobile navigation.
 * Contains: Sensoren, Warnungen, Einstellungen
 *
 * Features:
 * - Slides in from right with animation
 * - Overlay backdrop for closing
 * - Close button (X)
 * - Auto-closes on navigation
 */
@Component({
  selector: 'myiotgrid-mobile-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatRippleModule
  ],
  templateUrl: './mobile-sidebar.component.html',
  styleUrl: './mobile-sidebar.component.scss'
})
export class MobileSidebarComponent {
  private readonly router = inject(Router);

  /**
   * Whether the sidebar is open
   */
  readonly isOpen = input<boolean>(false);

  /**
   * Emits when the sidebar should close
   */
  readonly closeRequest = output<void>();

  /**
   * Sidebar menu items
   */
  readonly menuItems = [
    { icon: 'memory', label: 'Sensoren', route: '/sensors' },
    { icon: 'warning', label: 'Warnungen', route: '/alerts' },
    { icon: 'settings', label: 'Einstellungen', route: '/settings' }
  ];

  /**
   * Close the sidebar
   */
  close(): void {
    this.closeRequest.emit();
  }

  /**
   * Navigate to a route and close sidebar
   */
  navigateTo(route: string): void {
    this.router.navigate([route]);
    this.close();
  }

  /**
   * Handle overlay click
   */
  onOverlayClick(): void {
    this.close();
  }

  /**
   * Check if a route is active
   */
  isActive(route: string): boolean {
    return this.router.url === route || this.router.url.startsWith(route + '/');
  }
}
