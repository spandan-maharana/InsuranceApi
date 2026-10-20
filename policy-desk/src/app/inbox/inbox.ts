import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ApiService } from '../api.service';
import { AuthService } from '../auth.service';
import { QuoteRequestItem, RequestStatus } from '../models';

function prettyKey(key: string): string {
  const words = key
    .replace(/([a-z])([A-Z0-9])/g, '$1 $2')
    .replace(/([0-9])([A-Za-z])/g, '$1 $2')
    .toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

// Pulls readable messages out of an API error
function messagesFrom(err: any): string {
  if (err.status === 0) {
    return 'We could not reach the server. Please check that the API is running.';
  }
  const body = err.error;
  if (body?.error) {
    return body.error;
  }
  if (body?.errors) {
    return (Object.values(body.errors) as string[][]).flat().join(' ');
  }
  return 'Something went wrong. Please try again.';
}

@Component({
  selector: 'app-inbox',
  imports: [CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './inbox.html',
  styleUrl: './inbox.css',
})
export class Inbox implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  tabs: RequestStatus[] = ['New', 'Accepted', 'Declined'];

  requests = signal<QuoteRequestItem[]>([]);
  loading = signal(true);
  loadError = signal('');

  tab = signal<RequestStatus>('New');
  selectedId = signal<number | null>(null);

  // The decision form
  coverageInput = signal('');
  declining = signal(false);
  reason = signal('');
  busy = signal(false);
  actionError = signal('');
  notice = signal('');

  // The requests in the tab we are looking at
  visible = computed(() => {
    const status = this.tab();
    const list = this.requests().filter((r) => r.status === status);
    if (status === 'New') {
      return list; // oldest first, so the longest wait is at the top
    }
    // Finished ones: the most recent decision first
    return [...list].sort((a, b) => (b.reviewedAt ?? '').localeCompare(a.reviewedAt ?? ''));
  });

  selected = computed(() => this.requests().find((r) => r.id === this.selectedId()) ?? null);

  answerList = computed(() =>
    Object.entries(this.selected()?.answers ?? {}).map(([key, value]) => ({
      label: prettyKey(key),
      value,
    })),
  );

  ngOnInit() {
    this.load();
  }

  countFor(status: RequestStatus): number {
    return this.requests().filter((r) => r.status === status).length;
  }

  load() {
    this.loading.set(true);
    this.loadError.set('');

    this.api.getRequests().subscribe({
      next: (list) => {
        this.requests.set(list);
        this.loading.set(false);
        this.pickFirst();
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 401) {
          // The login has expired, so sign in again
          this.auth.logout();
        } else {
          this.loadError.set(messagesFrom(err));
        }
      },
    });
  }

  setTab(status: RequestStatus) {
    this.tab.set(status);
    this.notice.set('');
    this.pickFirst();
  }

  select(id: number) {
    const request = this.requests().find((r) => r.id === id);
    this.selectedId.set(id);
    this.coverageInput.set(request ? String(request.coverageAmount) : '');
    this.declining.set(false);
    this.reason.set('');
    this.actionError.set('');
  }

  // Selects the first request in the current tab, or nothing if the tab is empty
  private pickFirst() {
    const first = this.visible()[0];
    if (first) {
      this.select(first.id);
    } else {
      this.selectedId.set(null);
    }
  }

  read(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  // The server sends times in UTC without a "Z", so add it before showing them
  asUtc(time: string): string {
    return time.endsWith('Z') ? time : time + 'Z';
  }

  accept() {
    const request = this.selected();
    if (!request || request.status !== 'New') {
      return;
    }

    const raw = this.coverageInput().trim();
    const amount = Number(raw);
    if (raw === '' || Number.isNaN(amount)) {
      this.actionError.set('Please enter a coverage amount.');
      return;
    }
    if (amount < request.minCoverage || amount > request.maxCoverage) {
      this.actionError.set(
        `The amount must be between ${request.minCoverage.toLocaleString()} and ${request.maxCoverage.toLocaleString()}.`,
      );
      return;
    }

    this.busy.set(true);
    this.actionError.set('');

    // Only send an amount when the agent changed it
    const changed = amount !== request.coverageAmount;
    this.api.acceptRequest(request.id, changed ? amount : undefined).subscribe({
      next: (result) => {
        this.busy.set(false);
        this.notice.set(
          `Request #${request.id} for ${request.firstName} ${request.lastName} was accepted. ` +
            `Draft quote #${result.quoteId} has been created.`,
        );
        this.afterDecision();
      },
      error: (err) => this.failed(err),
    });
  }

  startDecline() {
    this.declining.set(true);
    this.actionError.set('');
  }

  cancelDecline() {
    this.declining.set(false);
    this.reason.set('');
    this.actionError.set('');
  }

  decline() {
    const request = this.selected();
    if (!request || request.status !== 'New') {
      return;
    }

    const reason = this.reason().trim();
    if (reason.length < 3) {
      this.actionError.set('Please give a reason (at least 3 characters).');
      return;
    }

    this.busy.set(true);
    this.actionError.set('');

    this.api.declineRequest(request.id, reason).subscribe({
      next: () => {
        this.busy.set(false);
        this.notice.set(`Request #${request.id} for ${request.firstName} ${request.lastName} was declined.`);
        this.afterDecision();
      },
      error: (err) => this.failed(err),
    });
  }

  private failed(err: any) {
    this.busy.set(false);
    if (err.status === 401) {
      this.auth.logout();
    } else {
      this.actionError.set(messagesFrom(err));
    }
  }

  // Reload the list, move to the next request, and update the number in the header
  private afterDecision() {
    this.api.getRequests().subscribe({
      next: (list) => {
        this.requests.set(list);
        this.pickFirst();
      },
    });
    this.api.refreshNewRequestCount();
  }
}