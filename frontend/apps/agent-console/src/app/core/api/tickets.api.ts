import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateTicketRequest, TicketDetail, TicketStatus, TicketSummary } from '../models/ticket.models';
import { IdResponse } from './generated/api-types';

@Injectable({ providedIn: 'root' })
export class TicketsApi {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/tickets`;

  constructor(private readonly http: HttpClient) {}

  list(status?: TicketStatus): Promise<TicketSummary[]> {
    const params: Record<string, string> = status ? { status } : {};
    return firstValueFrom(this.http.get<TicketSummary[]>(this.baseUrl, { params }));
  }

  get(id: string): Promise<TicketDetail> {
    return firstValueFrom(this.http.get<TicketDetail>(`${this.baseUrl}/${id}`));
  }

  create(request: CreateTicketRequest): Promise<Required<IdResponse>> {
    return firstValueFrom(this.http.post<Required<IdResponse>>(this.baseUrl, request));
  }

  resolve(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${id}/resolve`, {}));
  }

  assign(id: string, assigneeUserId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${id}/assign`, { assigneeUserId }));
  }
}
