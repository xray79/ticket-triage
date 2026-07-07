import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OrgSettings {
  forceLocalOnly: boolean;
}

@Injectable({ providedIn: 'root' })
export class PreferencesApi {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  getMyProviderPreference(): Promise<{ providerPreference: string }> {
    return firstValueFrom(this.http.get<{ providerPreference: string }>(`${this.baseUrl}/api/users/me/provider-preference`));
  }

  setMyProviderPreference(providerPreference: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${this.baseUrl}/api/users/me/provider-preference`, { providerPreference })
    );
  }

  getOrgSettings(): Promise<OrgSettings> {
    return firstValueFrom(this.http.get<OrgSettings>(`${this.baseUrl}/api/admin/org-settings`));
  }

  setOrgSettings(settings: OrgSettings): Promise<void> {
    return firstValueFrom(this.http.put<void>(`${this.baseUrl}/api/admin/org-settings`, settings));
  }
}
