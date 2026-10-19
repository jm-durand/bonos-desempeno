import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Notification } from './core/services/notification';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly notification = inject(Notification);
  private readonly notificationBox = viewChild<ElementRef<HTMLElement>>('notificationBox');

  constructor() {
    // La notificación se muestra como popover: entra al top layer del navegador,
    // que es la única forma de quedar por encima de un <dialog> modal abierto.
    effect(() => {
      const box = this.notificationBox()?.nativeElement;
      const message = this.notification.message();
      if (!box) return;
      if (box.matches(':popover-open')) box.hidePopover();
      if (message) box.showPopover();
    });
  }
}
