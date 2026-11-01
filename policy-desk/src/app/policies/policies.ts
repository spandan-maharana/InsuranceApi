import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ApiService } from '../api.service';
import { AuthService } from '../auth.service';
import { PolicyEvent, PolicyItem } from '../models';

type PolicyTab = 'Issued' | 'Cancelled';

// The API sends the status as text. This also copes with a number, just in case.
function tidy(policy: PolicyItem): PolicyItem {
  const raw = policy.status as unknown;
  return typeof raw === 'number'
    ? { ...policy, status: raw === 0 ? 'Issued' : 'Cancelled' }
    : policy;
}

// Pulls a readable message out of an API error
function messageFrom(err: any): string {
  if (err.status === 0) {
    return 'We could not reach the server. Please check that the API is running.';
  }
  if (err.error?.error) {
    return err.error.error;
  }
  return 'Something went wrong. Please try again.';
}

@Component({
  selector: 'app-policies',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './policies.html',
  styleUrl: './policies.css',
})
export class Policies implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  tabs: PolicyTab[] = ['Issued', 'Cancelled'];

  policies = signal<PolicyItem[]>([]);
  loading = signal(true);
  loadError = signal('');

  tab = signal<PolicyTab>('Issued');
  selectedId = signal<number | null>(null);

  confirmingCancel = signal(false);
  busy = signal(false);
  actionError = signal('');
  notice = signal('');
  history = signal<PolicyEvent[]>([]);

  // Only agents can renew or cancel (the backend enforces this too)
  canEdit = computed(() => this.auth.role() === 'Agent');

  // The policies in the tab we are looking at: newest policy first, latest term first
  visible = computed(() =>
    this.policies()
      .filter((p) => p.status === this.tab())
      .sort((a, b) => b.segment - a.segment || b.term - a.term),
  );

  selected = computed(() => this.policies().find((p) => p.id === this.selectedId()) ?? null);

  ngOnInit() {
    this.load();
  }

  countFor(status: PolicyTab): number {
    return this.policies().filter((p) => p.status === status).length;
  }

  // Cancel works on any policy that is still Issued
  canCancel(p: PolicyItem): boolean {
    return p.status === 'Issued';
  }

  // Renew works on an Issued policy, but only if the next term does not exist yet
  canRenew(p: PolicyItem): boolean {
    if (p.status !== 'Issued') {
      return false;
    }
    return !this.policies().some((x) => x.segment === p.segment && x.term === p.term + 1);
  }

  load() {
    this.loading.set(true);
    this.loadError.set('');

    this.api.getPolicies().subscribe({
      next: (list) => {
        this.policies.set(list.map(tidy));
        this.loading.set(false);
        this.pickFirst();
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 401) {
          this.auth.logout();
        } else {
          this.loadError.set(messageFrom(err));
        }
      },
    });
  }

  setTab(status: PolicyTab) {
    this.tab.set(status);
    this.notice.set('');
    this.pickFirst();
  }

  select(id: number) {
    this.selectedId.set(id);
    this.confirmingCancel.set(false);
    this.actionError.set('');

    this.history.set([]);
    this.api.getPolicyHistory(id).subscribe({
      next: (rows) => this.history.set(rows),
      error: () => this.history.set([]),
    });
  }
  

  private pickFirst() {
    const first = this.visible()[0];
    if (first) {
      this.select(first.id);
    } else {
      this.selectedId.set(null);
    }
  }

  // The server sends times in UTC without a "Z", so add it before showing them
  asUtc(time: string): string {
    return time.endsWith('Z') ? time : time + 'Z';
  }

  renew() {
    const policy = this.selected();
    if (!policy || !this.canRenew(policy)) {
      return;
    }

    this.busy.set(true);
    this.actionError.set('');

    this.api.renewPolicy(policy.id).subscribe({
      next: (renewal) => {
        this.busy.set(false);
        this.notice.set(`${policy.policyNumber} was renewed. The new policy is ${renewal.policyNumber}.`);
        this.reload(renewal.id);
      },
      error: (err) => this.failed(err),
    });
  }

  startCancel() {
    this.confirmingCancel.set(true);
    this.actionError.set('');
  }

  keepPolicy() {
    this.confirmingCancel.set(false);
    this.actionError.set('');
  }

  cancel() {
    const policy = this.selected();
    if (!policy || !this.canCancel(policy)) {
      return;
    }

    this.busy.set(true);
    this.actionError.set('');

    this.api.cancelPolicy(policy.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.notice.set(`${policy.policyNumber} was cancelled.`);
        this.reload(null);
      },
      error: (err) => this.failed(err),
    });
  }

  private failed(err: any) {
    this.busy.set(false);
    if (err.status === 401) {
      this.auth.logout();
    } else {
      this.actionError.set(messageFrom(err));
    }
  }

  // Reload the list. After a renewal, show the new policy; after a cancel, go to the next one.
  private reload(selectId: number | null) {
    this.api.getPolicies().subscribe({
      next: (list) => {
        this.policies.set(list.map(tidy));
        this.confirmingCancel.set(false);
        if (selectId !== null) {
          this.tab.set('Issued');
          this.select(selectId);
        } else {
          this.pickFirst();
        }
      },
    });
  }
}