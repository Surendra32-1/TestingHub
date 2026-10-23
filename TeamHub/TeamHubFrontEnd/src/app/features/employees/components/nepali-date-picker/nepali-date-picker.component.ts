import { CommonModule } from '@angular/common';
import { Component, forwardRef } from '@angular/core';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { NepaliDateAdapter, NEPALI_DATE_FORMATS } from './nepali-date-adapter';

@Component({
  selector: 'app-nepali-date-picker',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDatepickerModule, MatFormFieldModule, MatInputModule],
  template: `
    <mat-form-field appearance="outline" class="nepali-date-picker">
      <input matInput [matDatepicker]="picker" [formControl]="dateControl" [placeholder]="placeholder"
        (dateChange)="onDateChange($event.value)" (blur)="markTouched()">
      <mat-datepicker-toggle matSuffix [for]="picker" aria-label="Open Nepali calendar"></mat-datepicker-toggle>
      <mat-datepicker #picker></mat-datepicker>
    </mat-form-field>
  `,
  styles: [`
    :host { display: block; width: 100%; }
    .nepali-date-picker { width: 100%; }
  `],
  providers: [
    { provide: DateAdapter, useClass: NepaliDateAdapter },
    { provide: MAT_DATE_FORMATS, useValue: NEPALI_DATE_FORMATS },
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => NepaliDatePickerComponent),
      multi: true
    }
  ]
})
export class NepaliDatePickerComponent implements ControlValueAccessor {
  readonly dateControl = new FormControl<Date | null>(null);
  placeholder = 'YYYY-MM-DD';
  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    if (!value) {
      this.dateControl.setValue(null, { emitEvent: false });
      return;
    }
    this.dateControl.setValue(this.adapter.parse(value, 'input'), { emitEvent: false });
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    if (isDisabled) this.dateControl.disable({ emitEvent: false });
    else this.dateControl.enable({ emitEvent: false });
  }

  onDateChange(date: Date | null): void {
    this.dateControl.setValue(date, { emitEvent: false });
    this.onChange(date ? this.adapter.format(date, 'input') : null);
  }

  markTouched(): void {
    this.onTouched();
  }

  constructor(private readonly adapter: DateAdapter<Date>) {}
}
