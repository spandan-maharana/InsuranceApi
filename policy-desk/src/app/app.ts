import { Component, computed, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  auth = inject(AuthService);
  private api = inject(ApiService);
  private router = inject(Router);

  // The address of the page we are on now. It updates after every page change.
  private url = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event) => (event as NavigationEnd).urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  onLoginPage = computed(() => this.url().startsWith('/login'));

  // Only agents and the SuperUser have an inbox
  canUseInbox = computed(() => this.auth.role() === 'Agent' || this.auth.role() === 'SuperUser');

  // Only the Underwriter and the SuperUser have the underwriting page
  canUseUnderwriting = computed(
    () => this.auth.role() === 'Underwriter' || this.auth.role() === 'SuperUser',
  );

  // The number on the mail icon (shared, so the inbox can refresh it too)
  newCount = this.api.newRequestCount.asReadonly();

  constructor() {
    // After every page change, ask the server how many requests are waiting
    effect(() => {
      this.url();
      if (this.canUseInbox()) {
        this.api.refreshNewRequestCount();
      } else {
        this.api.newRequestCount.set(0);
      }
    });
  }
}