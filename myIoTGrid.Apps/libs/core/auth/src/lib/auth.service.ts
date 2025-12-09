import { Injectable, signal, computed } from '@angular/core';

export interface User {
  id: string;
  email: string;
  displayName: string;
  tenantId: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _user = signal<User | null>(null);
  private readonly _isLoading = signal(false);

  readonly user = this._user.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly userDisplayName = computed(() => this._user()?.displayName ?? 'Benutzer');
  readonly userInitials = computed(() => {
    const name = this._user()?.displayName;
    if (!name) return 'U';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  });

  constructor() {
    // Auto-login with default user for development
    this.setDefaultUser();
  }

  private setDefaultUser(): void {
    this._user.set({
      id: '00000000-0000-0000-0000-000000000001',
      email: 'admin@myiotgrid.local',
      displayName: 'Hub Administrator',
      tenantId: '00000000-0000-0000-0000-000000000001'
    });
  }

  async login(email: string, password: string): Promise<boolean> {
    this._isLoading.set(true);
    try {
      // TODO: Implement actual login logic
      this.setDefaultUser();
      return true;
    } finally {
      this._isLoading.set(false);
    }
  }

  async logout(): Promise<void> {
    this._user.set(null);
  }

  async validateSession(): Promise<boolean> {
    // For now, always return true
    return true;
  }

  async refreshAccessToken(): Promise<boolean> {
    // For now, always return true
    return true;
  }
}
