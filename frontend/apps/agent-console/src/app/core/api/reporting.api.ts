import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProviderBreakdownDto, ReportingSummaryDto } from './generated/api-types';

export type ReportingSummary = Omit<Required<ReportingSummaryDto>, 'byProvider'> & {
  byProvider: Required<ProviderBreakdownDto>[];
};

@Injectable({ providedIn: 'root' })
export class ReportingApi {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/reporting`;

  constructor(private readonly http: HttpClient) {}

  getSummary(): Promise<ReportingSummary> {
    return firstValueFrom(this.http.get<ReportingSummary>(`${this.baseUrl}/summary`));
  }
}
