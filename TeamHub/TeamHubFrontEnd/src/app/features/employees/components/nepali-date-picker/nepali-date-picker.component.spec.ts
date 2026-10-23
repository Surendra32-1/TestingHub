import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DateAdapter, MAT_DATE_LOCALE } from '@angular/material/core';
import NepaliDate from 'nepali-date-converter';
import { NepaliDatePickerComponent } from './nepali-date-picker.component';

describe('NepaliDatePickerComponent', () => {
  let component: NepaliDatePickerComponent;
  let fixture: ComponentFixture<NepaliDatePickerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NepaliDatePickerComponent],
      providers: [{ provide: MAT_DATE_LOCALE, useValue: 'en-US' }]
    }).compileComponents();

    fixture = TestBed.createComponent(NepaliDatePickerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('uses the Angular Material datepicker input and toggle', () => {
    expect(fixture.nativeElement.querySelector('input[matinput]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('mat-datepicker-toggle')).toBeTruthy();
  });

  it('reads and emits Nepali dates through the form control value accessor', () => {
    let changedValue: string | null = null;
    component.registerOnChange((value) => changedValue = value);

    component.writeValue('2083-04-12');
    expect(component.dateControl.value).toEqual(new NepaliDate('2083-04-12').toJsDate());

    const selectedDate = new NepaliDate('2083-04-13').toJsDate();
    component.onDateChange(selectedDate);
    expect(changedValue).toBe('2083-04-13');
  });

  it('exposes Nepali month and day values through its Material date adapter', () => {
    const adapter = fixture.debugElement.injector.get(DateAdapter);
    const date = adapter.createDate(2083, 3, 12);

    expect(adapter.format(date, 'input')).toBe('2083-04-12');
    expect(adapter.getYear(date)).toBe(2083);
    expect(adapter.getMonth(date)).toBe(3);
    expect(adapter.getDate(date)).toBe(12);
    expect(adapter.getNumDaysInMonth(date)).toBeGreaterThanOrEqual(29);
  });
});
