import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ApiService } from '../api.service';
import { AuthService } from '../auth.service';
import { QuoteItem, QuoteStatus } from '../models';
import { messagesFrom, prettyKey } from '../utils';

type QuoteTab = 'Draft' | 'Quoted' | 'Underwriter' | 'Closed';

@Component({
  selector: 'app-quotes',
  imports: [CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './quotes.html',
  styleUrl: './quotes.css',
})
export class Quotes implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  tabs: { key: QuoteTab; label: string }[] = [
    { key: 'Draft', label: 'Draft' },
    { key: 'Quoted', label: 'Quoted' },
    { key: 'Underwriter', label: 'With underwriter' },
    { key: 'Closed', label: 'Closed' },
  ];

  quotes = signal<QuoteItem[]>([]);
  loading = signal(true);
  loadError = signal('');

  tab = signal<QuoteTab>('Draft');
  selectedId = signal<number | null>(null);

  busy = signal(false);
  confirmingDiscard = signal(false);
  actionError = signal('');
  notice = signal('');

  // Only agents can change quotes. The SuperUser can look.
  canEdit = computed(() => this.auth.role() === 'Agent');

  visible = computed(() => this.quotes().filter((q) => this.tabOf(q.status) === this.tab()));

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

  // Which tab a quote belongs to
  private tabOf(status: QuoteStatus): QuoteTab {
    switch (status) {
      case 'Draft':
        return 'Draft';
      case 'Quoted':
        return 'Quoted';
      case 'Accepted':
      case 'Bound':
        return 'Underwriter';
      default:
        return 'Closed'; // Issued, Expired, Discarded
    }
  }

  countFor(tab: QuoteTab): number {
    return this.quotes().filter((q) => this.tabOf(q.status) === tab).length;
  }

  statusHelp(status: QuoteStatus): string {
    switch (status) {
      case 'Draft':
        return 'Not priced yet. Price it to get the premium.';
      case 'Quoted':
        return 'Priced. Waiting for the customer to say yes.';
      case 'Accepted':
        return 'The customer said yes. Waiting for the underwriter.';
      case 'Bound':
        return 'Approved by the underwriter. The policy will be issued next.';
      case 'Issued':
        return 'The policy has been issued.';
      case 'Expired':
        return 'This quote expired before the customer accepted.';
      default:
        return 'This quote was discarded.';
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

  setTab(tab: QuoteTab) {
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

  private money(value: number | null): string {
    return (value ?? 0).toLocaleString('en-US', { style: 'currency', currency: 'USD' });
  }

  // Put the updated quote into the list
  private replace(updated: QuoteItem) {
    this.quotes.update((list) => list.map((q) => (q.id === updated.id ? updated : q)));
  }

  // Draft -> Quoted
  price() {
    const quote = this.selected();
    if (!quote || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionError.set('');

    this.api.priceQuote(quote.id).subscribe({
      next: (updated) => {
        this.busy.set(false);
        this.replace(updated);
        this.notice.set(
          `Quote #${updated.id} for ${updated.customerName} is priced. Premium: ${this.money(updated.premium)}.`,
        );
        // Go to the Quoted tab so the agent sees the premium straight away
        this.tab.set('Quoted');
        this.select(updated.id);
      },
      error: (err) => this.failed(err),
    });
  }

  // Quoted -> Accepted (the customer said yes)
  accept() {
    const quote = this.selected();
    if (!quote || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionError.set('');

    this.api.acceptQuote(quote.id).subscribe({
      next: (updated) => {
        this.busy.set(false);
        this.replace(updated);
        this.notice.set(
          `Quote #${updated.id} for ${updated.customerName} was accepted and sent to the underwriter.`,
        );
        this.tab.set('Underwriter');
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