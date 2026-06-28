export interface QuotationListItem {
  itemId: string;
  itemCode: string;
  name: string;
  customerName: string;
  quotationDate: string;
  taxType: number;
  tax: number;
  total: number;
  status: number;
  createDate: string;
  hasInvoices: boolean;
}

export interface QuotationDetailResponse {
  itemId: string;
  itemCode: string;
  name: string;
  enName: string;
  customerId: number;
  customerName: string;
  customerDetailId: string | null;
  quotationDate: string;
  expireDate: string | null;
  taxType: number;
  tax: number;
  total: number;
  payment: string;
  enPayment: string;
  remark: string;
  enRemark: string;
  workdays: number | null;
  status: number;
  details: QuotationDetailItem[];
  contents: QuotationContentItem[];
}

export interface QuotationDetailItem {
  itemDetailId: string | null;
  title: string;
  enTitle: string;
  description: string;
  enDescription: string;
  quantity: number;
  price: number;
  total: number;
  freq: number;
}

export interface QuotationContentItem {
  itemContentId: string | null;
  title: string;
  remark: string;
  price: number;
  freq: number;
}

export interface QuotationCreateUpdateDto {
  customerId: number | null;
  customerDetailId: string | null;
  name: string;
  quotationDate: string;
  expireDate: string | null;
  taxType: number;
  payment: string;
  remark: string;
  workdays: number | null;
  status: number;
  details: {
    itemDetailId: string | null;
    title: string;
    description: string;
    quantity: number;
    price: number;
    freq: number;
  }[];
  contents: {
    itemContentId: string | null;
    title: string;
    remark: string;
    price: number;
    freq: number;
  }[];
}

// For customer dropdown
export interface CustomerLookup {
  customerId: number;
  code: string;
  name: string;
}

// For contact person dropdown
export interface ContactLookup {
  customerDetailId: string;
  name: string;
  email: string;
}
