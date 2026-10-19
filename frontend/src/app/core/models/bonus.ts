export interface BonusListItem {
  bonusId: number;
  bonusStateCode: string;
  bonusStateDescription: string;
  period: string;
  departmentCode: string;
  departmentName: string;
  employeeNumber: number;
  employeeFullName: string;
  bonusTypeId: number;
  bonusTypeDescription: string;
  bonusValue: number;
  reasonDescription: string;
  grossAmount: number;
  taxAmount: number;
  netAmount: number;
}

export interface EmployeeSummary {
  departmentCode: string;
  employeeNumber: number;
  fullName: string;
  hireDate: string;
  monthlySalary: number;
  taxRate: number;
}

export interface RegisterBonusRequest {
  departmentCode: string;
  employeeNumber: number;
  period: string;
  bonusTypeId: number;
  bonusValue: number;
  reasonCode: string;
}

export interface BonusListParams {
  pageIndex: number;
  pageSize: number;
  departmentCode?: string;
  employeeNumber?: number | null;
  period?: string;
  bonusStateCode?: string;
}
