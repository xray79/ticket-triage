import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { IdResponse, RegisterUserRequest as GeneratedRegisterUserRequest } from './generated/api-types';

export type RegisterUserRequest = Required<GeneratedRegisterUserRequest>;

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/users`;

  constructor(private readonly http: HttpClient) {}

  register(request: RegisterUserRequest): Promise<Required<IdResponse>> {
    return firstValueFrom(this.http.post<Required<IdResponse>>(this.baseUrl, request));
  }
}
