export interface CustomerListItem {
  customerId: number;
  code: string;
  name: string;
  customerTypeName: string | null;
  phone: string | null;
  createDate: string | null;
  hasQuotations: boolean;
}

export interface CustomerDetail {
  customerId: number;
  code: string;
  name: string;
  address: string | null;
  customerTypeId: number | null;
  customerTypeName: string | null;
  countryId: number | null;
  countryName: string | null;
  phone: string | null;
  fax: string | null;
  vatNumber: string | null;
  logo: string | null;
  createDate: string | null;
  contacts: Contact[];
}

export interface Contact {
  contactId: string | null;
  name: string | null;
  email: string | null;
  phone: string | null;
  ext: string | null;
}

export interface CustomerCreateUpdate {
  name: string;
  address: string | null;
  customerTypeId: number | null;
  countryId: number | null;
  phone: string | null;
  fax: string | null;
  vatNumber: string | null;
  contacts: Contact[] | null;
}
