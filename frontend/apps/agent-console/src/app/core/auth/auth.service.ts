import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResult, LoginRequest } from '../models/auth.models';
import { asArray, decodeJwt } from './jwt.util';

const STORAGE_KEY = 'ticket-triage.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authState = signal<AuthResult | null>(this.readFromStorage());

  readonly isAuthenticated = computed(() => this.authState() !== null);
  readonly displayName = computed(() => this.authState()?.displayName ?? '');
  readonly roles = computed(() => this.authState()?.roles ?? []);
  readonly permissions = computed(() => {
    const token = this.authState()?.accessToken;
    if (!token) return [] as string[];
    return asArray(decodeJwt(token)?.permission);
  });

  constructor(private readonly http: HttpClient) {}

  get accessToken(): string | null {
    return this.authState()?.accessToken ?? null;
  }

  get refreshToken(): string | null {
    return this.authState()?.refreshToken ?? null;
  }

  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  async login(request: LoginRequest): Promise<void> {
    const result = await firstValueFrom(
      this.http.post<AuthResult>(`${environment.apiBaseUrl}/api/auth/login`, request)
    );
    this.setAuth(result);
  }

  async refresh(): Promise<AuthResult> {
    const result = await firstValueFrom(
      this.http.post<AuthResult>(`${environment.apiBaseUrl}/api/auth/refresh`, {
        refreshToken: this.refreshToken
      })
    );
    this.setAuth(result);
    return result;
  }

  logout(): void {
    this.authState.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  private setAuth(result: AuthResult): void {
    this.authState.set(result);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(result));
  }

  private readFromStorage(): AuthResult | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthResult;
    } catch {
      return null;
    }
  }
}
