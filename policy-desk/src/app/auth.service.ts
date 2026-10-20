import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

export interface LoginResponse {
  token: string;
  role: string;
  username: string;
  displayName: string;
}

interface Session {
  username: string;
  displayName: string;
  token: string;
  role: string;
}

const STORAGE_KEY = 'policy-desk-session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private baseUrl = 'https://localhost:7073';

  private session = signal<Session | null>(this.loadSession());

  isLoggedIn = computed(() => this.session() !== null);
  username = computed(() => this.session()?.username ?? '');
  displayName = computed(() => this.session()?.displayName || this.session()?.username || '');
  role = computed(() => this.session()?.role ?? '');
  token = computed(() => this.session()?.token ?? '');

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/api/auth/login`, { username, password })
      .pipe(
        tap((response) => {
          const session: Session = {
            username,
            displayName: response.displayName || username,
            token: response.token,
            role: response.role,
          };
          this.session.set(session);
          this.saveSession(session);
        }),
      );
  }

  logout() {
    this.session.set(null);
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // storage not available - nothing to clear
    }
    this.router.navigate(['/login']);
  }

  private loadSession(): Session | null {
    try {
      const saved = sessionStorage.getItem(STORAGE_KEY);
      return saved ? (JSON.parse(saved) as Session) : null;
    } catch {
      return null;
    }
  }

  private saveSession(session: Session) {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // storage not available - the login still works until refresh
    }
  }
}