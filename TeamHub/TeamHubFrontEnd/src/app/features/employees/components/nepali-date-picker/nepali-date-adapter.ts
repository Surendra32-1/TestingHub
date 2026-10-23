import { Inject, Injectable } from '@angular/core';
import { DateAdapter, MAT_DATE_LOCALE } from '@angular/material/core';
import NepaliDate from 'nepali-date-converter';

@Injectable()
export class NepaliDateAdapter extends DateAdapter<Date> {
  constructor(@Inject(MAT_DATE_LOCALE) locale: string) {
    super();
    this.setLocale(locale);
  }

  getYear(date: Date): number {
    return this.nepaliDate(date).getYear();
  }

  getMonth(date: Date): number {
    return this.nepaliDate(date).getMonth();
  }

  getDate(date: Date): number {
    return this.nepaliDate(date).getDate();
  }

  getDayOfWeek(date: Date): number {
    return this.nepaliDate(date).getDay();
  }

  getMonthNames(style: 'long' | 'short' | 'narrow'): string[] {
    return Array.from({ length: 12 }, (_, month) => {
      const label = new NepaliDate(2080, month, 1).format('MMMM');
      return style === 'short' ? label.slice(0, 3) : style === 'narrow' ? label[0] : label;
    });
  }

  getDateNames(): string[] {
    return Array.from({ length: 32 }, (_, day) => String(day + 1));
  }

  getDayOfWeekNames(style: 'long' | 'short' | 'narrow'): string[] {
    const formats = { long: 'ddd', short: 'dd', narrow: 'd' } as const;
    return Array.from({ length: 7 }, (_, day) => new NepaliDate(2080, 0, 1 + day).format(formats[style]));
  }

  getYearName(date: Date): string {
    return String(this.getYear(date));
  }

  getFirstDayOfWeek(): number {
    return 0;
  }

  getNumDaysInMonth(date: Date): number {
    const nepaliDate = this.nepaliDate(date);
    const year = nepaliDate.getYear();
    const month = nepaliDate.getMonth();
    let days = 0;

    for (let day = 1; day <= 32; day += 1) {
      try {
        const candidate = new NepaliDate(year, month, day);
        if (candidate.getYear() !== year || candidate.getMonth() !== month) break;
        days = day;
      } catch {
        break;
      }
    }

    return days;
  }

  clone(date: Date): Date {
    return new Date(date.getTime());
  }

  createDate(year: number, month: number, date: number): Date {
    if (!Number.isInteger(year) || !Number.isInteger(month) || !Number.isInteger(date) ||
        month < 0 || month > 11 || date < 1 || date > 32) {
      throw new Error('Invalid Nepali calendar date.');
    }
    const nepaliDate = new NepaliDate(year, month, date);
    if (nepaliDate.getYear() !== year || nepaliDate.getMonth() !== month || nepaliDate.getDate() !== date) {
      throw new Error('Invalid Nepali calendar date.');
    }
    return nepaliDate.toJsDate();
  }

  today(): Date {
    return new Date();
  }

  parse(value: unknown): Date | null {
    if (typeof value !== 'string' || !value.trim()) return null;
    try {
      const parsed = new NepaliDate(value.trim());
      return parsed.format('YYYY-MM-DD') === value.trim() ? parsed.toJsDate() : null;
    } catch {
      return null;
    }
  }

  format(date: Date, displayFormat: string): string {
    if (!this.isValid(date)) throw new Error('Cannot format an invalid Nepali calendar date.');
    const nepaliDate = this.nepaliDate(date);
    switch (displayFormat) {
      case 'input':
        return nepaliDate.format('YYYY-MM-DD');
      case 'monthYearLabel':
        return nepaliDate.format('MMMM YYYY');
      case 'dateA11yLabel':
        return nepaliDate.format('ddd, MMMM D, YYYY');
      case 'monthYearA11yLabel':
        return nepaliDate.format('MMMM YYYY');
      default:
        return nepaliDate.format('YYYY-MM-DD');
    }
  }

  addCalendarYears(date: Date, years: number): Date {
    const nepaliDate = this.nepaliDate(date);
    const day = nepaliDate.getDate();
    nepaliDate.setYear(nepaliDate.getYear() + years);
    return this.clampDay(nepaliDate, day);
  }

  addCalendarMonths(date: Date, months: number): Date {
    const nepaliDate = this.nepaliDate(date);
    const day = nepaliDate.getDate();
    nepaliDate.setMonth(nepaliDate.getMonth() + months);
    return this.clampDay(nepaliDate, day);
  }

  addCalendarDays(date: Date, days: number): Date {
    const nepaliDate = this.nepaliDate(date);
    nepaliDate.setDate(nepaliDate.getDate() + days);
    return nepaliDate.toJsDate();
  }

  toIso8601(date: Date): string {
    return this.format(date, 'input');
  }

  isDateInstance(value: unknown): boolean {
    return value instanceof Date;
  }

  isValid(date: Date): boolean {
    if (!(date instanceof Date) || Number.isNaN(date.getTime())) return false;
    try {
      this.nepaliDate(date);
      return true;
    } catch {
      return false;
    }
  }

  invalid(): Date {
    return new Date(Number.NaN);
  }

  private nepaliDate(date: Date): NepaliDate {
    return new NepaliDate(date);
  }

  private clampDay(date: NepaliDate, day: number): Date {
    const year = date.getYear();
    const month = date.getMonth();
    for (let clampedDay = day; clampedDay > 0; clampedDay -= 1) {
      try {
        return this.createDate(year, month, clampedDay);
      } catch {
        continue;
      }
    }
    throw new Error('Invalid Nepali calendar date.');
  }
}

export const NEPALI_DATE_FORMATS = {
  parse: { dateInput: 'input' },
  display: {
    dateInput: 'input',
    monthYearLabel: 'monthYearLabel',
    dateA11yLabel: 'dateA11yLabel',
    monthYearA11yLabel: 'monthYearA11yLabel'
  }
};
