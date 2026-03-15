export enum QuotationStatus {
  Quoted = 0,
  Contracted = 1,
  Closed = 2,
  Cancelled = 3,
}

export enum InvoiceStatus {
  Opened = 0,
  Sent = 1,
  Received = 2,
  Voided = 3,
}

export enum InvoiceType {
  TwoCopy = 0,
  ThreeCopy = 1,
}

export enum TaxType {
  Exclusive = 0,
  Inclusive = 1,
  Exempt = 2,
}
