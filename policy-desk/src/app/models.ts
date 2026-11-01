export interface ProductSummary {
  code: string;
  name: string;
  description: string;
}

export interface Category {
  code: string;
  name: string;
  products: ProductSummary[];
}

export interface Question {
  key: string;
  label: string;
  type: string;
  isRequired: boolean;
  minValue: number | null;
  maxValue: number | null;
  options: string[] | null;
}

export interface ProductDetail {
  code: string;
  name: string;
  description: string;
  categoryCode: string;
  categoryName: string;
  coverageLabel: string;
  minCoverage: number;
  maxCoverage: number;
  questions: Question[];
}

// What the public form sends. There is no coverage amount:
// the server works it out from the answers.
export interface RequestInput {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  productCode: string;
  requestedEffectiveDate: string;
  answers: Record<string, string | number>;
}

export interface RequestReceived {
  id: number;
  message: string;
}

// One row of the "issued quotes" table on the dashboard
export interface IssuedPolicyRow {
  quoteId: number;
  policyNumber: string;
  customerName: string;
  productName: string;
  coverageAmount: number;
  premium: number;
  effectiveDate: string;
  expirationDate: string;
  issuedAt: string;
  policyStatus: string;
}

// Everything the dashboard shows
export interface DashboardData {
  newRequests: number;
  draftQuotes: number;
  issuedQuotes: number;
  issuedPolicies: IssuedPolicyRow[];
}

export interface NewRequestCount {
  count: number;
}

// One line of "how the coverage was worked out"
export interface CoverageLine {
  description: string;
  multiplier: number | null;
  amount: number;
}

export interface CoverageBreakdown {
  coverage: number;
  limitApplied: boolean;
  steps: CoverageLine[];
}

export type RequestStatus = 'New' | 'Accepted' | 'Declined';

// A quote request, as the agent sees it in the inbox
export interface QuoteRequestItem {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  productCode: string;
  productName: string;
  coverageAmount: number;
  coverageLabel: string;
  minCoverage: number;
  maxCoverage: number;
  coverageBreakdown: CoverageBreakdown | null;
  requestedEffectiveDate: string;
  answers: Record<string, string>;
  status: RequestStatus;
  declineReason: string | null;
  createdAt: string;
  reviewedAt: string | null;
  customerId: number | null;
  quoteId: number | null;
}

// ----- Quotes -----

export type QuoteStatus =
  | 'Draft'
  | 'Quoted'
  | 'Accepted'
  | 'Bound'
  | 'Issued'
  | 'Expired'
  | 'Discarded';

// One line of "how the premium was worked out"
export interface PremiumStep {
  description: string;
  multiplier: number | null;
  amount: number;
}

export interface PremiumBreakdown {
  premium: number;
  minimumApplied: boolean;
  steps: PremiumStep[];
}

export interface QuoteItem {
  id: number;
  customerId: number;
  customerName: string;
  productCode: string;
  productName: string;
  answers: Record<string, string>;
  coverageAmount: number;
  premium: number | null;
  premiumBreakdown: PremiumBreakdown | null;
  requestedEffectiveDate: string;
  status: QuoteStatus;
  createdBy: string | null;
  createdAt: string;
  expiresAt: string | null;
}

//----- Policies -----
export interface PolicyItem{
  id: number;
  policyNumber: string;
  segment: number;
  term: number;
  customerId: number;
  customerName: string;
  quoteId: number;
  productCode: string;
  productName: string;
  coverageAmount: number;
  premium: number;
  effectiveDate: string;
  expirationDate: string;
  status: string;
  cancelledAt: string | null;
  createdAt: string;
}

export interface PolicyEvent{
  id: number;
  policyId: number;
  type: string;
  occuredAt: string;
  performedBy: string | null;
}