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
}

export interface CustomerLookup {
  customerId: number;
  name: string;
  code: string;
}
