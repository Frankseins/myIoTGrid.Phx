import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { filter, Subscription } from 'rxjs';
import { AuthService } from '@myiotgrid/core/auth';
import { NavigationItem } from '../../models/navigation.model';

@Component({
  selector: 'myiotgrid-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private routerSubscription?: Subscription;

  readonly isCollapsed = signal(false);
  readonly currentRoute = signal('/');

  readonly userName = computed(() => this.authService.userDisplayName());
  readonly userInitials = computed(() => this.authService.userInitials());

  readonly mainNavItems: NavigationItem[] = [
    { icon: 'dashboard', label: 'Dashboard', route: '/dashboard' },
    { icon: 'sensors', label: 'Sensoren', route: '/sensors' },
    { icon: 'hub', label: 'Hubs', route: '/hubs' },
    { icon: 'notifications', label: 'Warnungen', route: '/alerts' }
  ];

  readonly bottomNavItems: NavigationItem[] = [
    { icon: 'settings', label: 'Einstellungen', route: '/settings' }
  ];

  ngOnInit(): void {
    this.currentRoute.set(this.router.url);

    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.currentRoute.set(this.router.url);
      });
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }

  toggleCollapsed(): void {
    this.isCollapsed.update(v => !v);
  }

  isActive(route: string): boolean {
    return this.currentRoute().startsWith(route);
  }

  async logout(): Promise<void> {
    await this.authService.logout();
    this.router.navigate(['/']);
  }

  openProfile(): void {
    this.router.navigate(['/settings/profile']);
  }
}
