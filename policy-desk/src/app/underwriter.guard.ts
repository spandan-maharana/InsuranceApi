import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

// Only the Underwriter (and the SuperUser, who can look) may open the underwriting page
export const underwriterGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const allowed = auth.role() === 'Underwriter' || auth.role() === 'SuperUser';
  return allowed ? true : router.createUrlTree(['/login']);
};