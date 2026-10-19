import { Component, ElementRef, signal, viewChild } from '@angular/core';

/**
 * Confirmación sobre el <dialog> nativo: showModal() da foco atrapado,
 * cierre con Esc y backdrop sin JavaScript adicional.
 * Uso: const ok = await confirmDialog.open('¿Seguro?');
 */
@Component({
  selector: 'app-confirm-dialog',
  template: `
    <dialog #dialog class="confirm" (close)="onClose()">
      <p class="confirm-message">{{ message() }}</p>
      <div class="confirm-actions">
        <button type="button" class="btn" (click)="dialog.close('')">Cancelar</button>
        <button type="button" class="btn btn-primary" (click)="dialog.close('ok')">Confirmar</button>
      </div>
    </dialog>
  `,
  styles: `
    .confirm {
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 20px;
      max-width: 420px;
    }
    .confirm::backdrop {
      background: rgb(0 0 0 / 0.35);
    }
    .confirm-message {
      margin: 0 0 16px;
    }
    .confirm-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
    }
  `,
})
export class ConfirmDialog {
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  protected readonly message = signal('');
  private resolve?: (confirmed: boolean) => void;

  open(message: string): Promise<boolean> {
    this.message.set(message);
    this.dialog().nativeElement.showModal();
    return new Promise((resolve) => (this.resolve = resolve));
  }

  protected onClose(): void {
    this.resolve?.(this.dialog().nativeElement.returnValue === 'ok');
    this.resolve = undefined;
  }
}
