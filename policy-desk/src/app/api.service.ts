import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from './api-config';
import {
  Category,
  DashboardData,
  NewRequestCount,
  PolicyItem,
  ProductDetail,
  QuoteItem,
  QuoteRequestItem,
  QuoteStatus,
  RequestInput,
  RequestReceived,
} from './models';

const QUOTE_STATUSES: QuoteStatus[] = [
  'Draft',
  'Quoted',
  'Accepted',
  'Bound',
  'Issued',
  'Expired',
  'Discarded',
];

// The API sends the status as text. This also copes with a number, just in case.
function tidy(quote: QuoteItem): QuoteItem {
  const raw = quote.status as unknown;
  return typeof raw === 'number' ? { ...quote, status: QUOTE_STATUSES[raw] } : quote;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private baseUrl = API_BASE_URL;

  // The number on the mail icon in the header. Any page can refresh it.
  newRequestCount = signal(0);

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/api/categories`);
  }

  getProduct(code: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.baseUrl}/api/products/${code}`);
  }

  submitRequest(input: RequestInput): Observable<RequestReceived> {
    return this.http.post<RequestReceived>(`${this.baseUrl}/api/requests`, input);
  }

  getDashboard(): Observable<DashboardData> {
    return this.http.get<DashboardData>(`${this.baseUrl}/api/dashboard`);
  }

  getNewRequestCount(): Observable<NewRequestCount> {
    return this.http.get<NewRequestCount>(`${this.baseUrl}/api/dashboard/new-requests`);
  }

  // Asks the server for the number and updates the mail icon
  refreshNewRequestCount() {
    this.getNewRequestCount().subscribe({
      next: (result) => this.newRequestCount.set(result.count),
      error: () => this.newRequestCount.set(0),
    });
  }

  // ----- Agent inbox -----

  getRequests(): Observable<QuoteRequestItem[]> {
    return this.http.get<QuoteRequestItem[]>(`${this.baseUrl}/api/inbox`);
  }

  // Leave coverageAmount out to use the recommended amount
  acceptRequest(id: number, coverageAmount?: number): Observable<QuoteRequestItem> {
    const body = coverageAmount === undefined ? {} : { coverageAmount };
    return this.http.post<QuoteRequestItem>(`${this.baseUrl}/api/inbox/${id}/accept`, body);
  }

  declineRequest(id: number, reason: string): Observable<QuoteRequestItem> {
    return this.http.post<QuoteRequestItem>(`${this.baseUrl}/api/inbox/${id}/decline`, {
      reason,
    });
  }

  // ----- Quotes -----

  getQuotes(): Observable<QuoteItem[]> {
    return this.http
      .get<QuoteItem[]>(`${this.baseUrl}/api/quotes`)
      .pipe(map((list) => list.map(tidy)));
  }

  // Works out the premium (Draft -> Quoted)
  priceQuote(id: number): Observable<QuoteItem> {
    return this.http
      .post<QuoteItem>(`${this.baseUrl}/api/quotes/${id}/quote`, {})
      .pipe(map(tidy));
  }

  // The customer said yes (Quoted -> Accepted)
  acceptQuote(id: number): Observable<QuoteItem> {
    return this.http
      .post<QuoteItem>(`${this.baseUrl}/api/quotes/${id}/accept`, {})
      .pipe(map(tidy));
  }

  discardQuote(id: number): Observable<QuoteItem> {
    return this.http
      .post<QuoteItem>(`${this.baseUrl}/api/quotes/${id}/discard`, {})
      .pipe(map(tidy));
  }

  // ----- Underwriting -----

  // The underwriter approves the quote (Accepted -> Bound)
  bindQuote(id: number): Observable<QuoteItem> {
    return this.http
      .post<QuoteItem>(`${this.baseUrl}/api/quotes/${id}/bind`, {})
      .pipe(map(tidy));
  }

  // The underwriter issues the policy (Bound -> Issued). The policy is created.
  issueQuote(id: number): Observable<QuoteItem> {
    return this.http
      .post<QuoteItem>(`${this.baseUrl}/api/quotes/${id}/issue`, {})
      .pipe(map(tidy));
  }

  // ----- Policies -----
getPolicies(): Observable<PolicyItem[]> {
    return this.http.get<PolicyItem[]>(`${this.baseUrl}/api/policies`);
  }

  // Creates the next term (Term + 1) of the same policy
  renewPolicy(id: number): Observable<PolicyItem> {
    return this.http.post<PolicyItem>(`${this.baseUrl}/api/policies/${id}/renew`, {});
  }

  // Soft cancel: the row stays, its status changes
  cancelPolicy(id: number): Observable<PolicyItem> {
    return this.http.post<PolicyItem>(`${this.baseUrl}/api/policies/${id}/cancel`, {});
  }
}

