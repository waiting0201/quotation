export interface NavItem {
  label: string;
  icon: string;
  route?: string;
  permissionKey?: string;
  children?: NavChild[];
}

export interface NavChild {
  label: string;
  route: string;
  permissionKey?: string;
}

export const NAV_ITEMS: NavItem[] = [
  {
    label: 'Dashboard',
    icon: 'home',
    route: '/dashboard',
  },
  {
    label: '報價管理',
    icon: 'file-text',
    permissionKey: 'ItemList',
    children: [
      { label: '報價清單', route: '/quotation', permissionKey: 'ItemList' },
      { label: '網站清單', route: '/hosts', permissionKey: 'HostList' },
    ],
  },
  {
    label: '請款管理',
    icon: 'receipt',
    permissionKey: 'InvoiceList',
    children: [
      { label: '請款清單', route: '/invoice', permissionKey: 'InvoiceList' },
      { label: '入帳清單', route: '/income', permissionKey: 'IncomeList' },
    ],
  },
  {
    label: '客戶管理',
    icon: 'users',
    permissionKey: 'CustomerList',
    children: [
      { label: '客戶清單', route: '/customer', permissionKey: 'CustomerList' },
      { label: '客戶分類清單', route: '/customer/category', permissionKey: 'CustomerTypeList' },
    ],
  },
  {
    label: '系統管理',
    icon: 'settings',
    permissionKey: 'UserList',
    children: [
      { label: '使用者清單', route: '/settings/users', permissionKey: 'UserList' },
      { label: '群組清單', route: '/settings/groups', permissionKey: 'GroupList' },
      { label: '國家清單', route: '/settings/countries', permissionKey: 'CountryList' },
      { label: '付款條件清單', route: '/settings/payments', permissionKey: 'PaymentsList' },
    ],
  },
];
