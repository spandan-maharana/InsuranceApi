import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Quotes } from './quotes';

describe('Quotes', () => {
  let component: Quotes;
  let fixture: ComponentFixture<Quotes>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Quotes],
    }).compileComponents();

    fixture = TestBed.createComponent(Quotes);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
