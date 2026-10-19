import { Component, ElementRef, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BonusTypes, Department } from '../../core/models/catalog';
import { EmployeeSummary } from '../../core/models/bonus';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';
import { Notification } from '../../core/services/notification';

/** Redondeo a 2 decimales con el medio hacia arriba, igual que el backend (solo informativo: la autoridad del cálculo es la API). */
const round2 = (value: number): number => Math.round(value * 100) / 100;

@Component({
  selector: 'app-bonus-register-modal',
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  templateUrl: './bonus-register-modal.html',
  styleUrl: './bonus-register-modal.css',
})
export class BonusRegisterModal {
  private readonly catalogApi = inject(CatalogApi);
  private readonly bonusApi = inject(BonusApi);
  private readonly notification = inject(Notification);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly departments = input.required<Department[]>();
  readonly saved = output<void>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  protected readonly bonusTypes = toSignal(this.catalogApi.getBonusTypes(), { initialValue: [] });
  protected readonly reasons = toSignal(this.catalogApi.getBonusReasons(), { initialValue: [] });

  protected readonly employee = signal<EmployeeSummary | null>(null);
  protected readonly searching = signal(false);
  protected readonly saving = signal(false);

  /** Tope del input de periodo: el mes actual. */
  protected readonly currentPeriod = `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}`;

  protected readonly searchForm = this.formBuilder.group({
    departmentCode: ['', Validators.required],
    employeeNumber: [null as number | null, Validators.required],
  });

  protected readonly form = this.formBuilder.group({
    period: ['', Validators.required],
    bonusTypeId: [BonusTypes.FixedAmount as number, Validators.required],
    bonusValue: [null as number | null, [Validators.required, this.validateBonusValue.bind(this)]],
    reasonCode: ['', Validators.required],
  });

  private readonly formValues = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  protected readonly isPercentage = computed(() => this.formValues().bonusTypeId === BonusTypes.SalaryPercentage);
  protected readonly valueLabel = computed(() => (this.isPercentage() ? 'Porcentaje (%)' : 'Monto'));

  /** Vista previa del cálculo mientras el usuario escribe. */
  protected readonly preview = computed(() => {
    const employee = this.employee();
    const { bonusTypeId, bonusValue } = this.formValues();
    if (!employee || !bonusValue || bonusValue <= 0) return null;

    const gross = round2(
      bonusTypeId === BonusTypes.SalaryPercentage ? (employee.monthlySalary * bonusValue) / 100 : bonusValue,
    );
    const tax = round2((gross * employee.taxRate) / 100);
    return { gross, tax, net: round2(gross - tax) };
  });

  constructor() {
    // El rango válido del valor depende del tipo elegido y del sueldo del empleado.
    this.form.controls.bonusTypeId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.form.controls.bonusValue.updateValueAndValidity());
  }

  open(): void {
    this.reset();
    this.dialog().nativeElement.showModal();
  }

  protected close(): void {
    this.dialog().nativeElement.close();
  }

  protected searchEmployee(): void {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }
    const { departmentCode, employeeNumber } = this.searchForm.getRawValue();
    this.searching.set(true);
    this.employee.set(null);
    this.bonusApi.searchEmployee(departmentCode, employeeNumber!).subscribe({
      next: (employee) => {
        this.employee.set(employee);
        this.searching.set(false);
        this.form.controls.bonusValue.updateValueAndValidity();
      },
      error: () => this.searching.set(false),
    });
  }

  protected save(): void {
    const employee = this.employee();
    if (!employee) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { period, bonusTypeId, bonusValue, reasonCode } = this.form.getRawValue();
    this.saving.set(true);
    this.bonusApi
      .register({
        departmentCode: employee.departmentCode,
        employeeNumber: employee.employeeNumber,
        period,
        bonusTypeId,
        bonusValue: bonusValue!,
        reasonCode,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.close();
          this.notification.show('success', 'Bono registrado correctamente.');
          this.saved.emit();
        },
        error: () => this.saving.set(false),
      });
  }

  private reset(): void {
    this.employee.set(null);
    this.searchForm.reset();
    this.form.reset();
  }

  private validateBonusValue(control: AbstractControl): ValidationErrors | null {
    const value = control.value as number | null;
    if (value == null) return null; // required se encarga del vacío
    if (value <= 0) return { min: true };

    const bonusTypeId = this.form?.controls.bonusTypeId.value;
    if (bonusTypeId === BonusTypes.SalaryPercentage && value > 100) return { maxPercentage: true };

    const employee = this.employee?.();
    if (bonusTypeId === BonusTypes.FixedAmount && employee && value > employee.monthlySalary) return { maxSalary: true };

    return null;
  }
}
