import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ApiService } from '../api.service';
import { AuthService } from '../auth.service';
import { QuoteItem, QuoteStatus } from '../models';
import { messagesFrom, prettyKey } from '../utils';

type UnderwritingTab = 'Approve' | 'Issue' | 'Issued';

@Component({
  selector: 'app-underwriting',
  imports: [CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './underwriting.html',
  styleUrl: './underwriting.css',
})
export class Underwriting implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  tabs: { key: UnderwritingTab; label: string }[] = [
    { key: 'Approve', label: 'To approve' },
    { key: 'Issue', label: 'To issue' },
    { key: 'Issued', label: 'Issued' },
  ];

  quotes = signal<QuoteItem[]>([]);
  loading = signal(true);
  loadError = signal('');

  tab = signal<UnderwritingTab>('Approve');
  selectedId = signal<number | null>(null);

  busy = signal(false);
  confirmingDiscard = signal(false);
  actionError = signal('');
  notice = signal('');

  // Only the Underwriter can act. The SuperUser can look.
  canAct = computed(() => this.auth.role() === 'Underwriter');

  // The quotes in the tab we are looking at
  visible = computed(() => {
    const list = this.quotes().filter((q) => this.tabOf(q.status) === this.tab());
    if (this.tab() === 'Issued') {
      return list; // newest first
    }
    // Work queues: the longest wait is at the top
    return [...list].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  });

  selected = computed(() => this.quotes().find((q) => q.id === this.selectedId()) ?? null);

  answerList = computed(() =>
    Object.entries(this.selected()?.answers ?? {}).map(([key, value]) => ({
      label: prettyKey(key),
      value,
    })),
  );

  ngOnInit() {
    this.load();
  }

  // Which tab a quote belongs to (null = not for the underwriter)
  private tabOf(status: QuoteStatus): UnderwritingTab | null {
    switch (status) {
      case 'Accepted':
        return 'Approve';
      case 'Bound':
        return 'Issue';
      case 'Issued':
        return 'Issued';
      default:
        return null;
    }
  }

  countFor(tab: UnderwritingTab): number {
    return this.quotes().filter((q) => this.tabOf(q.status) === tab).length;
  }

  statusHelp(status: QuoteStatus): string {
    switch (status) {
      case 'Accepted':
        return 'The customer said yes. Review the quote, then approve it.';
      case 'Bound':
        return 'Approved. Issue the policy to make it active.';
      case 'Issued':
        return 'The policy has been issued.';
      default:
        return '';
    }
  }

  load() {
    this.loading.set(true);
    this.loadError.set('');

    this.api.getQuotes().subscribe({
      next: (list) => {
        this.quotes.set(list);
        this.loading.set(false);
        this.pickFirst();
      },
      error: (err) => {
        this.loading.set(false);
        // A 401 is handled for us: the app logs the person out
        this.loadError.set(messagesFrom(err));
      },
    });
  }

  setTab(tab: UnderwritingTab) {
    this.tab.set(tab);
    this.notice.set('');
    this.pickFirst();
  }

  select(id: number) {
    this.selectedId.set(id);
    this.confirmingDiscard.set(false);
    this.actionError.set('');
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

  private replace(updated: QuoteItem) {
    this.quotes.update((list) => list.map((q) => (q.id === updated.id ? updated : q)));
  }

  // Accepted -> Bound
  approve() {
    const quote = this.selected();
    if (!quote || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionError.set('');

    this.api.bindQuote(quote.id).subscribe({
      next: (updated) => {
        this.busy.set(false);
        this.replace(updated);
        this.notice.set(
          `Quote #${updated.id} for ${updated.customerName} was approved. It is ready to issue.`,
        );
        this.tab.set('Issue');
        this.select(updated.id);
      },
      error: (err) => this.failed(err),
    });
  }

  // Bound -> Issued (the policy is created)
  issue() {
    const quote = this.selected();
    if (!quote || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionError.set('');

    this.api.issueQuote(quote.id).subscribe({
      next: (updated) => {
        this.busy.set(false);
        this.replace(updated);
        this.notice.set(
          `The policy for ${updated.customerName} was issued (quote #${updated.id}).`,
        );
        this.tab.set('Issued');
        this.select(updated.id);
      },
      error: (err) => this.failed(err),
    });
  }

  startDiscard() {
    this.confirmingDiscard.set(true);
    this.actionError.set('');
  }

  cancelDiscard() {
    this.confirmingDiscard.set(false);
  }

  discard() {
    const quote = this.selected();
    if (!quote || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionError.set('');

    this.api.discardQuote(quote.id).subscribe({
      next: (updated) => {
        this.busy.set(false);
        this.replace(updated);
        this.notice.set(`Quote #${updated.id} for ${updated.customerName} was discarded.`);
        this.pickFirst();
      },
      error: (err) => this.failed(err),
    });
  }

  private failed(err: any) {
    this.busy.set(false);
    this.actionError.set(messagesFrom(err));
  }
}