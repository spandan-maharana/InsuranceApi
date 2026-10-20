import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Underwriting } from './underwriting';

describe('Underwriting', () => {
  let component: Underwriting;
  let fixture: ComponentFixture<Underwriting>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Underwriting],
    }).compileComponents();

    fixture = TestBed.createComponent(Underwriting);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
