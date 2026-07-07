export type TicketStatus = 'New' | 'Triaged' | 'InProgress' | 'Resolved' | 'TriageFailed';

export interface TicketSummary {
  id: string;
  subject: string;
  customerEmail: string;
  status: TicketStatus;
  priority: string | null;
  assignedToUserId: string | null;
  createdAtUtc: string;
}

export interface TriageResult {
  category: string;
  priority: string;
  summary: string;
  draftReply: string;
  provider: string;
  wasFallback: boolean;
  triagedAtUtc: string;
}

export interface TicketDetail {
  id: string;
  subject: string;
  body: string;
  customerEmail: string;
  status: TicketStatus;
  requestedProvider: string;
  createdByUserId: string;
  createdAtUtc: string;
  assignedToUserId: string | null;
  triage: TriageResult | null;
}

export interface CreateTicketRequest {
  subject: string;
  body: string;
  customerEmail: string;
  requestedProvider: string;
}
