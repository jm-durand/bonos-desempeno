import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BonusListItem, BonusListParams, EmployeeSummary, RegisterBonusRequest } from '../models/bonus';
import { PagedResponse } from '../models/paged-response';

@Injectable({ providedIn: 'root' })
export class BonusApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/Bonus`;

  getPaged(params: BonusListParams): Observable<PagedResponse<BonusListItem>> {
    let httpParams = new HttpParams()
      .set('pageIndex', params.pageIndex)
      .set('pageSize', params.pageSize);

    if (params.departmentCode) httpParams = httpParams.set('departmentCode', params.departmentCode);
    if (params.employeeNumber != null) httpParams = httpParams.set('employeeNumber', params.employeeNumber);
    if (params.period) httpParams = httpParams.set('period', params.period);
    if (params.bonusStateCode) httpParams = httpParams.set('bonusStateCode', params.bonusStateCode);

    return this.http.get<PagedResponse<BonusListItem>>(this.baseUrl, { params: httpParams });
  }

  searchEmployee(departmentCode: string, employeeNumber: number): Observable<EmployeeSummary> {
    return this.http.get<EmployeeSummary>(`${this.baseUrl}/employee/${departmentCode}/${employeeNumber}`);
  }

  register(request: RegisterBonusRequest): Observable<{ bonusId: number }> {
    return this.http.post<{ bonusId: number }>(this.baseUrl, request);
  }

  pay(bonusId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bonusId}/pay`, null);
  }

  cancel(bonusId: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${bonusId}/cancel`, null);
  }

  remove(bonusId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${bonusId}`);
  }
}
