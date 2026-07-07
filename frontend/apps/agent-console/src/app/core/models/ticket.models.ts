import type { CreateTicketRequest as GeneratedCreateTicketRequest, TicketDto, TicketSummaryDto, TriageResultDto } from '../api/generated/api-types';

// Not encoded as an enum in the OpenAPI schema (the backend serializes the domain enum as a
// plain string), so this union is hand-maintained alongside Tickets.Domain.TicketStatus.
export type TicketStatus = 'New' | 'Triaged' | 'InProgress' | 'Resolved' | 'TriageFailed';

// The generated DTOs mark every field optional (the backend doesn't populate OpenAPI's
// `required` list) - `Required` reflects that a successful response always carries real values,
// and `status` is narrowed from `string` to the actual status union above.
export type TicketSummary = Omit<Required<TicketSummaryDto>, 'status'> & { status: TicketStatus };

export type TriageResult = Required<TriageResultDto>;

export type TicketDetail = Omit<Required<TicketDto>, 'status' | 'triage'> & {
  status: TicketStatus;
  triage: TriageResult | null;
};

export type CreateTicketRequest = Required<GeneratedCreateTicketRequest>;
