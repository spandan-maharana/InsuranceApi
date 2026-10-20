import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QuoteRequest } from './quote-request';

describe('QuoteRequest', () => {
  let component: QuoteRequest;
  let fixture: ComponentFixture<QuoteRequest>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuoteRequest],
    }).compileComponents();

    fixture = TestBed.createComponent(QuoteRequest);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
