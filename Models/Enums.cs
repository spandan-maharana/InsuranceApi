namespace InsuranceApi.Models;

public enum QuoteStatus { Draft, Quoted, Accepted, Bound, Issued, Expired, Discarded}
public enum QuoteAction { Quote, Accept, Bind, Issue, Discard}
public enum PolicyStatus{ Issued, Cancelled}
public enum PolicyTransactionType { Issued, Renewed, Cancelled}