import { Component, inject, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { BonusStates, BonusTypes, MasterStateCodes } from '../../core/models/catalog';
import { BonusListItem } from '../../core/models/bonus';
import { PagedResponse } from '../../core/models/paged-response';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';
import { Notification } from '../../core/services/notification';
import { ConfirmDialog } from '../../shared/confirm-dialog';
import { BonusRegisterModal } from './bonus-register-modal';

/** Pantalla de bonos: listado paginado desde el servidor y modal de registro. */
@Component({
  selector: 'app-bonuses',
  imports: [FormsModule, DecimalPipe, ConfirmDialog, BonusRegisterModal],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
})
export class Bonuses {
  private readonly catalogApi = inject(CatalogApi);
  private readonly bonusApi = inject(BonusApi);
  private readonly notification = inject(Notification);

  protected readonly departments = toSignal(this.catalogApi.getDepartments(), { initialValue: [] });
  protected readonly bonusStates = toSignal(this.catalogApi.getStates(MasterStateCodes.Bonus), { initialValue: [] });

  private readonly confirmDialog = viewChild.required(ConfirmDialog);
  private readonly registerModal = viewChild.required(BonusRegisterModal);

  protected readonly RegisteredState = BonusStates.Registered;
  protected readonly PercentageType = BonusTypes.SalaryPercentage;

  protected readonly pageSize = 10;
  protected readonly page = signal<PagedResponse<BonusListItem> | null>(null);
  protected readonly loading = signal(false);

  protected filters = this.emptyFilters();
  /** Copia aplicada al Buscar, para que el cambio de página respete lo buscado y no lo que haya en los inputs. */
  private appliedFilters = this.emptyFilters();

  constructor() {
    this.load(1);
  }

  protected search(): void {
    this.appliedFilters = { ...this.filters };
    this.load(1);
  }

  protected clearFilters(): void {
    this.filters = this.emptyFilters();
    this.search();
  }

  protected goToPage(pageIndex: number): void {
    this.load(pageIndex);
  }

  protected openRegisterModal(): void {
    this.registerModal().open();
  }

  protected onBonusSaved(): void {
    this.load(1);
  }

  protected async payBonus(bonus: BonusListItem): Promise<void> {
    const confirmed = await this.confirmDialog().open(
      `¿Pagar el bono de ${bonus.employeeFullName} del periodo ${bonus.period}? El bruto se distribuirá entre sus proyectos.`,
    );
    if (!confirmed) return;
    this.bonusApi.pay(bonus.bonusId).subscribe(() => {
      this.notification.show('success', 'Bono pagado.');
      this.reloadCurrentPage();
    });
  }

  protected async cancelBonus(bonus: BonusListItem): Promise<void> {
    const confirmed = await this.confirmDialog().open(
      `¿Anular el bono de ${bonus.employeeFullName} del periodo ${bonus.period}?`,
    );
    if (!confirmed) return;
    this.bonusApi.cancel(bonus.bonusId).subscribe(() => {
      this.notification.show('success', 'Bono anulado.');
      this.reloadCurrentPage();
    });
  }

  protected async deleteBonus(bonus: BonusListItem): Promise<void> {
    const confirmed = await this.confirmDialog().open(
      `¿Eliminar el bono de ${bonus.employeeFullName} del periodo ${bonus.period}? Dejará de aparecer en el listado.`,
    );
    if (!confirmed) return;
    this.bonusApi.remove(bonus.bonusId).subscribe(() => {
      this.notification.show('success', 'Bono eliminado.');
      this.reloadCurrentPage();
    });
  }

  private reloadCurrentPage(): void {
    this.load(this.page()?.pageNumber ?? 1);
  }

  private load(pageIndex: number): void {
    this.loading.set(true);
    this.bonusApi
      .getPaged({ ...this.appliedFilters, pageIndex, pageSize: this.pageSize })
      .subscribe({
        next: (page) => {
          // Si la página quedó vacía (p. ej. se eliminó la última fila), retrocede una.
          if (page.data.length === 0 && pageIndex > 1) {
            this.load(pageIndex - 1);
            return;
          }
          this.page.set(page);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  private emptyFilters() {
    return { departmentCode: '', employeeNumber: null as number | null, period: '', bonusStateCode: '' };
  }
}
