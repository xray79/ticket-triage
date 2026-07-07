// Thin re-export layer over the generated OpenAPI schema so the rest of the app imports
// named types instead of reaching into `components['schemas'][...]` everywhere.
import type { components } from './schema';

export type TicketSummaryDto = components['schemas']['TicketSummaryDto'];
export type TicketDto = components['schemas']['TicketDto'];
export type TriageResultDto = components['schemas']['TriageResultDto'];
export type CreateTicketRequest = components['schemas']['CreateTicketRequest'];
export type AssignTicketRequest = components['schemas']['AssignTicketRequest'];
export type IdResponse = components['schemas']['IdResponse'];

export type AuthResultDto = components['schemas']['AuthResultDto'];
export type LoginRequest = components['schemas']['LoginRequest'];
export type RefreshRequest = components['schemas']['RefreshRequest'];

export type OrgSettingsDto = components['schemas']['OrgSettingsDto'];
export type SetOrgSettingsRequest = components['schemas']['SetOrgSettingsRequest'];

export type ProviderPreferenceResponse = components['schemas']['ProviderPreferenceResponse'];
export type SetProviderPreferenceRequest = components['schemas']['SetProviderPreferenceRequest'];

export type ReportingSummaryDto = components['schemas']['ReportingSummaryDto'];
export type ProviderBreakdownDto = components['schemas']['ProviderBreakdownDto'];

export type RegisterUserRequest = components['schemas']['RegisterUserRequest'];
