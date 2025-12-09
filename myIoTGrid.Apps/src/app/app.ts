import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { NavigationRailComponent } from '@myiotgrid/core/navigation';
import { SignalRService } from '@myiotgrid/shared/data-access';
import { AuthService } from '@myiotgrid/core/auth';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, NavigationRailComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly router = inject(Router);
  private readonly signalRService = inject(SignalRService);
  private readonly authService = inject(AuthService);

  readonly isInitializing = signal(true);
  private readonly currentUrl = signal('/');

  readonly showNavigation = computed(() => {
    const url = this.currentUrl();
    return !this.isAuthRoute(url);
  });

  async ngOnInit(): Promise<void> {
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

  private isAuthRoute(url: string): boolean {
    const basePath = url.split('?')[0];
    return basePath.startsWith('/auth') ||
           basePath.startsWith('/login');
  }
}
