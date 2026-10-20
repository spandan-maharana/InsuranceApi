import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../api.service';
import { AuthService } from '../auth.service';
import { DashboardData } from '../models';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);

  data = signal<DashboardData | null>(null);
  loading = signal(true);
  error = signal('');

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set('');

    this.api.getDashboard().subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 401) {
          // The login has expired, so sign in again
          this.auth.logout();
        } else if (err.status === 0) {
          this.error.set('We could not reach the server. Please check that the API is running.');
        } else {
          this.error.set('We could not load your dashboard. Please try again.');
        }
      },
    });
  }

  // The server sends times in UTC without a "Z", so add it before showing the date
  asUtc(time: string): string {
    return time.endsWith('Z') ? time : time + 'Z';
  }
}