import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // For now, allow all access since we don't have auth yet
  // In production, this would redirect to login
  // router.navigate(['/auth/login']);
  // return false;

  return true;
};
