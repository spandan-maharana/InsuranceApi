import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const agentGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const allowed = auth.role() === 'Agent' || auth.role() === 'SuperUser';
  return allowed ? true : router.createUrlTree(['/login']);
};