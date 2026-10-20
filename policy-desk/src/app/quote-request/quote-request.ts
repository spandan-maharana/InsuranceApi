import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../api.service';
import { ProductDetail, RequestInput, RequestReceived } from '../models';

// Returns a date as text like 2026-10-18. 0 = today, 1 = tomorrow.
function dateText(daysFromToday: number): string {
  const d = new Date();
  d.setDate(d.getDate() + daysFromToday);
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${month}-${day}`;
}

// Turns "FirstName" into "First name" so messages read naturally.
function prettyNames(message: string): string {
  return message.replace(/\b[A-Z][a-z]+(?:[A-Z][a-z]+)+\b/g, (name) => {
    const words = name.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase();
    return words.charAt(0).toUpperCase() + words.slice(1);
  });
}

@Component({
  selector: 'app-quote-request',
  imports: [RouterLink, DatePipe],
  templateUrl: './quote-request.html',
  styleUrl: './quote-request.css',
})
export class QuoteRequest implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);

  product = signal<ProductDetail | null>(null);
  loading = signal(true);
  loadError = signal('');

  firstName = signal('');
  lastName = signal('');
  email = signal('');
  phoneNumber = signal('');
  effectiveDate = signal(dateText(1));
  answers = signal<Record<string, string>>({});

  // Which step we are on (0 is the first one).
  step = signal(0);
  fieldErrors = signal<Record<string, string>>({});

  submitting = signal(false);
  submitErrors = signal<string[]>([]);
  receipt = signal<RequestReceived | null>(null);

  // The questions step only exists when the product has questions.
  steps = computed(() => {
    const product = this.product();
    const hasQuestions = !!product && product.questions.length > 0;
    return hasQuestions
      ? ['About you', 'Your policy', 'Questions', 'Review']
      : ['About you', 'Your policy', 'Review'];
  });

  currentName = computed(() => this.steps()[this.step()]);

  ngOnInit() {
    const code = this.route.snapshot.paramMap.get('code') ?? '';
    this.api.getProduct(code).subscribe({
      next: (product) => {
        this.product.set(product);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('We could not load this product. Please go back and try again.');
        this.loading.set(false);
      },
    });
  }

  read(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  setAnswer(key: string, event: Event) {
    const value = this.read(event);
    this.answers.update((current) => ({ ...current, [key]: value }));
  }

  errorFor(key: string): string {
    return this.fieldErrors()[key] ?? '';
  }

  answerFor(key: string): string {
    const value = (this.answers()[key] ?? '').trim();
    return value === '' ? '—' : value;
  }

  dot(index: number): string {
    return index < this.step() ? '✓' : String(index + 1);
  }

  // Called when the customer presses Enter or the main button.
  advance() {
    if (this.currentName() === 'Review') {
      this.submit();
    } else {
      this.next();
    }
  }

  next() {
    const errors = this.validate(this.currentName());
    this.fieldErrors.set(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }
    this.moveTo(this.step() + 1);
  }

  back() {
    this.moveTo(this.step() - 1);
  }

  goTo(name: string) {
    this.moveTo(this.steps().indexOf(name));
  }

  private moveTo(index: number) {
    this.fieldErrors.set({});
    this.submitErrors.set([]);
    this.step.set(index);
    window.scrollTo({ top: 0 });
  }

  private validate(name: string): Record<string, string> {
    const product = this.product();
    const errors: Record<string, string> = {};
    if (!product) {
      return errors;
    }

    if (name === 'About you') {
      if (this.firstName().trim() === '') {
        errors['firstName'] = 'Please enter your first name.';
      }
      if (this.lastName().trim() === '') {
        errors['lastName'] = 'Please enter your last name.';
      }
      const email = this.email().trim();
      if (email === '') {
        errors['email'] = 'Please enter your email.';
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        errors['email'] = 'This email address does not look right.';
      }
      const phone = this.phoneNumber().trim();
      if (phone === '') {
        errors['phoneNumber'] = 'Please enter your phone number.';
      } else if (phone.length < 7 || phone.length > 30) {
        errors['phoneNumber'] = 'The phone number must be between 7 and 30 characters.';
      }
    }

    if (name === 'Your policy') {
      if (this.effectiveDate() === '') {
        errors['effectiveDate'] = 'Please choose a start date.';
      } else if (this.effectiveDate() < dateText(0)) {
        errors['effectiveDate'] = 'The start date cannot be in the past.';
      }
    }

    if (name === 'Questions') {
      for (const q of product.questions) {
        const key = 'q_' + q.key;
        const raw = (this.answers()[q.key] ?? '').trim();
        if (raw === '') {
          if (q.isRequired) {
            errors[key] = 'Please answer this question.';
          }
          continue;
        }
        if (q.type === 'Number') {
          const n = Number(raw);
          if (Number.isNaN(n)) {
            errors[key] = 'Please enter a number.';
          } else if (q.minValue !== null && n < q.minValue) {
            errors[key] = `The answer must be at least ${q.minValue}.`;
          } else if (q.maxValue !== null && n > q.maxValue) {
            errors[key] = `The answer must be at most ${q.maxValue}.`;
          }
        } else if (q.options && !q.options.includes(raw)) {
          errors[key] = 'Please choose one of the options.';
        }
      }
    }

    return errors;
  }

  submit() {
    const product = this.product();
    if (!product) {
      return;
    }

    const answers: Record<string, string | number> = {};
    for (const question of product.questions) {
      const raw = (this.answers()[question.key] ?? '').trim();
      if (raw === '') {
        continue;
      }
      answers[question.key] = question.type === 'Number' ? Number(raw) : raw;
    }

    const input: RequestInput = {
      firstName: this.firstName().trim(),
      lastName: this.lastName().trim(),
      email: this.email().trim(),
      phoneNumber: this.phoneNumber().trim(),
      productCode: product.code,
      requestedEffectiveDate: this.effectiveDate(),
      answers,
    };

    this.submitting.set(true);
    this.submitErrors.set([]);

    this.api.submitRequest(input).subscribe({
      next: (receipt) => {
        this.receipt.set(receipt);
        this.submitting.set(false);
        window.scrollTo({ top: 0 });
      },
      error: (err) => {
        this.submitErrors.set(this.messagesFrom(err));
        this.submitting.set(false);
      },
    });
  }

  private messagesFrom(err: any): string[] {
    if (err.status === 0) {
      return ['We could not reach the server. Please check that the API is running and try again.'];
    }
    const body = err.error;
    if (body?.error) {
      return [prettyNames(body.error)];
    }
    if (body?.errors) {
      return (Object.values(body.errors) as string[][]).flat().map(prettyNames);
    }
    return ['Something went wrong. Please try again.'];
  }
}