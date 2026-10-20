import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../api.service';
import { Category } from '../models';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private api = inject(ApiService);

  categories = signal<Category[]>([]);
  loading = signal(true);
  error = signal('');
  query = signal('');

  // The categories left after the search. Empty search shows everything.
  filtered = computed(() => {
    const text = this.query().trim().toLowerCase();
    const all = this.categories();
    if (text === '') {
      return all;
    }
    return all
      .map((category) => ({
        ...category,
        products: category.products.filter(
          (product) =>
            product.name.toLowerCase().includes(text) ||
            product.description.toLowerCase().includes(text),
        ),
      }))
      .filter((category) => category.products.length > 0);
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.api.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('We could not load the products. Please check that the API is running.');
        this.loading.set(false);
      },
    });
  }

  read(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  // Picks an icon and a short line for each category, based on its name.
  kindOf(name: string): string {
    const text = name.toLowerCase();
    if (text.includes('health')) return 'health';
    if (text.includes('life')) return 'life';
    if (text.includes('property') || text.includes('casualty')) return 'property';
    if (text.includes('special') || text.includes('niche')) return 'niche';
    return 'other';
  }

  blurbFor(name: string): string {
    switch (this.kindOf(name)) {
      case 'health':
        return 'Cover for Medical bills, Dental, Vision and Loss of Income.';
      case 'life':
        return 'Protect the People who depend on you.';
      case 'property':
        return 'Protect your Car, Home and Belongings.';
      case 'niche':
        return 'Cover for Pets, Travel, and your business.';
      default:
        return '';
    }
  }
}