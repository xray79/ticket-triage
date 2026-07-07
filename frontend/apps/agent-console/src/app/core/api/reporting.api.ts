import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ProviderBreakdown {
  provider: string;
  count: number;
  fallbackCount: number;
}

export interface ReportingSummary {
  totalTickets: number;
  newCount: number;
  triagedCount: number;
  resolvedCount: number;
  triageFailedCount: number;
  averageTriageLatencySeconds: number | null;
  byProvider: ProviderBreakdown[];
}

@Injectable({ providedIn: 'root' })
export class ReportingApi {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/reporting`;

  constructor(private readonly http: HttpClient) {}

  getSummary(): Promise<ReportingSummary> {
    return firstValueFrom(this.http.get<ReportingSummary>(`${this.baseUrl}/summary`));
  }
}
