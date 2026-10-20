import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';

export interface StatusCount {
  status: string;
  count: number;
}

export interface AgentSummary {
  agent: string;
  totalQuotes: number;
  openQuotes: number;
  issuedQuotes: number;
  premiumIssued: number;
}

export interface RecentPolicy {
  id: number;
  policyNumber: string;
  customerName: string;
  productName: string;
  coverageAmount: number;
  premium: number;
  issuedAt: string;
}

export interface AdminOverview {
  newRequests: number;
  acceptedRequests: number;
  declinedRequests: number;
  openQuotes: number;
  quotesByStatus: StatusCount[];
  issuedPolicies: number;
  premiumIssued: number;
  recentPolicies: RecentPolicy[];
  agents: AgentSummary[];
}

export interface StaffUser {
  id: number;
  username: string;
  displayName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface NewStaffUser {
  username: string;
  displayName: string;
  password: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private url = `${API_BASE_URL}/api/admin`;

  getOverview(): Observable<AdminOverview> {
    return this.http.get<AdminOverview>(`${this.url}/overview`);
  }

  getUsers(): Observable<StaffUser[]> {
    return this.http.get<StaffUser[]>(`${this.url}/users`);
  }

  createUser(user: NewStaffUser): Observable<StaffUser> {
    return this.http.post<StaffUser>(`${this.url}/users`, user);
  }

  resetPassword(id: number, password: string): Observable<StaffUser> {
    return this.http.post<StaffUser>(`${this.url}/users/${id}/password`, { password });
  }

  setActive(id: number, isActive: boolean): Observable<StaffUser> {
    return this.http.post<StaffUser>(`${this.url}/users/${id}/active`, { isActive });
  }
}