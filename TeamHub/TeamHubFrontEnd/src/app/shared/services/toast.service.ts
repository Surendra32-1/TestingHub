import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type ToastKind = 'success' | 'error' | 'info' | 'confirm';

export interface ToastMessage {
  id: number;
  kind: ToastKind;
  message: string;
  duration: number;
  actionLabel?: string;
  onAction?: () => void;
}

export const TOAST_AUTO_DISMISS_MS = 3000;
export const TOAST_CONFIRM_DISMISS_MS = 7000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly subject = new Subject<ToastMessage>();
  private nextId = 0;
  readonly messages$ = this.subject.asObservable();

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string): void {
    this.show(message, 'error');
  }

  info(message: string): void {
    this.show(message, 'info');
  }

  confirm(message: string, onConfirm: () => void, actionLabel = 'Delete'): void {
    this.subject.next({
      id: ++this.nextId,
      kind: 'confirm',
      message,
      duration: TOAST_CONFIRM_DISMISS_MS,
      actionLabel,
      onAction: onConfirm
    });
  }

  private show(message: string, kind: ToastKind, duration = TOAST_AUTO_DISMISS_MS): void {
    this.subject.next({ id: ++this.nextId, kind, message, duration });
  }
}
