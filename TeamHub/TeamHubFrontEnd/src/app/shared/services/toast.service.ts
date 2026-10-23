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

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly subject = new Subject<ToastMessage>();
  private nextId = 0;
  readonly messages$ = this.subject.asObservable();

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string): void {
    this.show(message, 'error', 6000);
  }

  info(message: string): void {
    this.show(message, 'info');
  }

  confirm(message: string, onConfirm: () => void, actionLabel = 'Delete'): void {
    this.subject.next({
      id: ++this.nextId,
      kind: 'confirm',
      message,
      duration: 7000,
      actionLabel,
      onAction: onConfirm
    });
  }

  private show(message: string, kind: ToastKind, duration = 4000): void {
    this.subject.next({ id: ++this.nextId, kind, message, duration });
  }
}
