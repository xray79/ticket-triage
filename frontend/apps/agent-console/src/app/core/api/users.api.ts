import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RegisterUserRequest {
  email: string;
  password: string;
  displayName: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/users`;

  constructor(private readonly http: HttpClient) {}

  register(request: RegisterUserRequest): Promise<{ id: string }> {
    return firstValueFrom(this.http.post<{ id: string }>(this.baseUrl, request));
  }
}
