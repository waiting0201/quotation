export interface IncomeListItem {
  incomeId: string;
  incomeCode: string;
  customerId: number | null;
  customerName: string;
  amount: number | null;
  fee: number | null;
  incomeDate: string | null;
  remark: string | null;
  createDate: string | null;
  hasInvoices: boolean;
}

export interface IncomeCreateDto {
  customerId: number;
  amount?: number;
  fee?: number;
  incomeDate?: string;
  remark?: string;
  invoiceIds?: string[];
}

export interface CustomerLookup {
  customerId: number;
  name: string;
  code: string;
}

/** 入帳可選（可核銷）請款單選項 */
export interface IncomeInvoiceOption {
  invoiceId: string;
  invoiceCode: string;
  requestDate: string | null;
  tax: number | null;
  total: number | null;
  status: number | null;
  createDate: string | null;
}
