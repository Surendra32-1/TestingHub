import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastMessage, ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-host.component.html',
  styleUrl: './toast-host.component.css'
})
export class ToastHostComponent {
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  messages: ToastMessage[] = [];

  constructor() {
    this.toastService.messages$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((message) => {
      this.messages = [...this.messages, message];
      this.changeDetectorRef.markForCheck();
      setTimeout(() => this.dismiss(message.id), message.duration);
    });
  }

  dismiss(id: number): void {
    this.messages = this.messages.filter((message) => message.id !== id);
    this.changeDetectorRef.markForCheck();
  }

  confirm(message: ToastMessage): void {
    this.dismiss(message.id);
    message.onAction?.();
  }
}
