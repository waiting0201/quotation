export interface InvoiceListItem {
  invoiceId: string;
  invoiceCode: string;
  customerId: number;
  customerName: string;
  requestDate: string;
  tax: number;       // 0=稅外加, 1=稅內含, 2=免稅
  total: number;
  status: number;    // 0=已開, 1=已寄出, 2=已入帳, 3=作廢
  createDate: string;
  hasIncomes: boolean;
}

export interface InvoiceDetail {
  invoiceDetailId: string | null;
  itemId: string | null;
  itemCode?: string;
  itemName?: string;
  itemTaxType?: number;
  invoiceType: number | null;  // 0=二聯, 1=三聯
  invoiceDate: string | null;
  invoiceNumber: string;
  price: number | null;
  tax?: number;
  remark: string;
  freq?: number;
}

export interface InvoiceDetailResponse {
  invoiceId: string;
  invoiceCode: string;
  customerId: number | null;
  customerName: string;
  requestDate: string | null;
  remark: string;
  tax: number;
  total: number;
  status: number;
  createDate: string;
  details: InvoiceDetail[];
}

export interface InvoiceCreateUpdateDto {
  customerId: number | null;
  requestDate: string | null;
  remark: string;
  status: number;
  details: {
    invoiceDetailId: string | null;
    itemId: string | null;
    invoiceType: number | null;
    invoiceDate: string | null;
    invoiceNumber: string;
    price: number | null;
    remark: string;
  }[];
}

export interface QuotationLookup {
  itemId: string;
  itemCode: string;
  name: string;
  taxType: number;
  total: number;
}

export interface CustomerLookup {
  customerId: number;
  name: string;
  code: string;
}
