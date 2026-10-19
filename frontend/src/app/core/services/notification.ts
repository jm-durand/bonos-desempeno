import { Injectable, signal } from '@angular/core';

export type NotificationType = 'error' | 'warning' | 'success';

export interface NotificationMessage {
  type: NotificationType;
  text: string;
}

/** Aviso global que se muestra en la parte superior de la aplicación. */
@Injectable({ providedIn: 'root' })
export class Notification {
  readonly message = signal<NotificationMessage | null>(null);
  private autoClose?: ReturnType<typeof setTimeout>;

  show(type: NotificationType, text: string): void {
    clearTimeout(this.autoClose);
    this.message.set({ type, text });
    // Los avisos de éxito se cierran solos; los errores quedan hasta que el usuario los cierre.
    if (type === 'success') {
      this.autoClose = setTimeout(() => this.clear(), 4000);
    }
  }

  clear(): void {
    clearTimeout(this.autoClose);
    this.message.set(null);
  }
}
