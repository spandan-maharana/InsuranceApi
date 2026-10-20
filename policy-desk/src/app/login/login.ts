import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

type StaffRole = 'Agent' | 'Underwriter' | 'SuperUser';

@Component({
  selector: 'app-login',
  imports: [],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);

  // null = the person has not chosen yet
  role = signal<StaffRole | null>(null);

  // The SuperUser is shown as "Administrator" on screen
  roleTitle = computed(() => this.nameOf(this.role()));

  username = signal('');
  password = signal('');
  busy = signal(false);
  error = signal('');

  read(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  choose(role: StaffRole) {
    this.role.set(role);
    this.username.set('');
    this.password.set('');
    this.error.set('');
  }

  back() {
    this.role.set(null);
    this.username.set('');
    this.password.set('');
    this.error.set('');
  }

  onSubmit(event: Event) {
    event.preventDefault();
    this.submit();
  }

  submit() {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.error.set('');

    this.auth.login(this.username().trim(), this.password()).subscribe({
      next: (response) => {
        this.busy.set(false);

        // The details are right, but for another role: sign out and say so.
        const chosen = this.role();
        if (chosen && response.role !== chosen) {
          this.auth.logout();
          this.error.set(
            `Those details belong to ${this.withArticle(response.role)} account. ` +
              `Please go back and choose ${this.nameOf(response.role)}.`,
          );
          return;
        }

        // Each role goes to its own first page
        if (response.role === 'SuperUser') {
          this.router.navigate(['/admin']);
        } else if (response.role === 'Agent') {
          this.router.navigate(['/dashboard']);
        } else if (response.role === 'Underwriter') {
          this.router.navigate(['/underwriting']);
        } else {
          this.router.navigate(['/']);
        }
      },
      error: (err) => {
        this.busy.set(false);
        if (err.status === 0) {
          this.error.set('We could not reach the server. Please check that the API is running.');
        } else if (err.error?.error) {
          this.error.set(err.error.error);
        } else {
          this.error.set('Something went wrong. Please try again.');
        }
      },
    });
  }

  private nameOf(role: string | null): string {
    return role === 'SuperUser' ? 'Administrator' : (role ?? '');
  }

  private withArticle(role: string): string {
    if (role === 'Agent') {
      return 'an agent';
    }
    if (role === 'Underwriter') {
      return 'an underwriter';
    }
    return 'an administrator';
  }
}