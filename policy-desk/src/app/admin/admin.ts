import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { AdminOverview, AdminService, StaffUser } from '../admin.service';
import { AuthService } from '../auth.service';

type Tab = 'overview' | 'users';

@Component({
  selector: 'app-admin',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class Admin {
  private api = inject(AdminService);
  auth = inject(AuthService);

  tab = signal<Tab>('overview');

  // ----- Overview -----
  overview = signal<AdminOverview | null>(null);
  overviewError = signal('');

  // The longest bar in "Quotes by status" is always full width
  private maxStatus = computed(() => {
    const list = this.overview()?.quotesByStatus ?? [];
    return Math.max(1, ...list.map((s) => s.count));
  });

  // ----- Users -----
  users = signal<StaffUser[]>([]);
  usersError = signal('');
  tableMessage = signal('');

  newName = signal('');
  newUsername = signal('');
  newRole = signal('Agent');
  newPassword = signal('');
  creating = signal(false);
  formError = signal('');
  formMessage = signal('');

  resetId = signal<number | null>(null);
  resetPassword = signal('');
  resetError = signal('');
  rowBusy = signal<number | null>(null);

  constructor() {
    this.loadOverview();
    this.loadUsers();
  }

  read(event: Event): string {
    return (event.target as HTMLInputElement | HTMLSelectElement).value;
  }

  show(tab: Tab) {
    this.tab.set(tab);
    if (tab === 'overview') {
      this.loadOverview();
    }
  }

  barWidth(count: number): number {
    return (count / this.maxStatus()) * 100;
  }

  // ----- loading -----

  loadOverview() {
    this.overviewError.set('');
    this.api.getOverview().subscribe({
      next: (data) => this.overview.set(data),
      error: (err) => this.overviewError.set(this.errorText(err)),
    });
  }

  loadUsers() {
    this.usersError.set('');
    this.api.getUsers().subscribe({
      next: (list) => this.users.set(list),
      error: (err) => this.usersError.set(this.errorText(err)),
    });
  }

  // ----- add a user -----

  createUser(event: Event) {
    event.preventDefault();
    if (this.creating()) {
      return;
    }
    this.creating.set(true);
    this.formError.set('');
    this.formMessage.set('');

    this.api
      .createUser({
        displayName: this.newName().trim(),
        username: this.newUsername().trim(),
        role: this.newRole(),
        password: this.newPassword(),
      })
      .subscribe({
        next: (user) => {
          this.creating.set(false);
          this.formMessage.set(
            `Created ${user.displayName} (${user.username}). Please share the password with them privately.`,
          );
          this.newName.set('');
          this.newUsername.set('');
          this.newPassword.set('');
          this.newRole.set('Agent');
          this.loadUsers();
        },
        error: (err) => {
          this.creating.set(false);
          this.formError.set(this.errorText(err));
        },
      });
  }

  // ----- reset a password -----

  startReset(user: StaffUser) {
    this.resetId.set(user.id);
    this.resetPassword.set('');
    this.resetError.set('');
    this.tableMessage.set('');
  }

  cancelReset() {
    this.resetId.set(null);
    this.resetPassword.set('');
    this.resetError.set('');
  }

  saveReset(user: StaffUser) {
    if (this.rowBusy() !== null) {
      return;
    }
    this.rowBusy.set(user.id);
    this.resetError.set('');

    this.api.resetPassword(user.id, this.resetPassword()).subscribe({
      next: () => {
        this.rowBusy.set(null);
        this.cancelReset();
        this.tableMessage.set(`Password changed for ${user.displayName}.`);
      },
      error: (err) => {
        this.rowBusy.set(null);
        this.resetError.set(this.errorText(err));
      },
    });
  }

  // ----- switch an account on or off -----

  setActive(user: StaffUser, isActive: boolean) {
    if (this.rowBusy() !== null) {
      return;
    }
    this.rowBusy.set(user.id);
    this.tableMessage.set('');
    this.usersError.set('');

    this.api.setActive(user.id, isActive).subscribe({
      next: () => {
        this.rowBusy.set(null);
        this.tableMessage.set(
          `${user.displayName} is now ${isActive ? 'switched on' : 'switched off'}.`,
        );
        this.loadUsers();
      },
      error: (err) => {
        this.rowBusy.set(null);
        this.usersError.set(this.errorText(err));
      },
    });
  }

  roleName(role: string): string {
    return role === 'SuperUser' ? 'Administrator' : role;
  }

  private errorText(err: { status?: number; error?: { error?: string } }): string {
    if (err.status === 0) {
      return 'We could not reach the server. Please check that the API is running.';
    }
    if (err.error?.error) {
      return err.error.error;
    }
    return 'Something went wrong. Please try again.';
  }
}