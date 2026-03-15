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
    label: '主頁',
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
    label: '客戶管理',
    icon: 'users',
    permissionKey: 'CustomerList',
    children: [
      { label: '客戶清單', route: '/customer', permissionKey: 'CustomerList' },
      { label: '客戶分類', route: '/customer/category', permissionKey: 'CustomerTypeList' },
    ],
  },
  {
    label: '發票管理',
    icon: 'receipt',
    permissionKey: 'InvoiceList',
    children: [
      { label: '發票清單', route: '/invoice', permissionKey: 'InvoiceList' },
    ],
  },
  {
    label: '收款管理',
    icon: 'dollar',
    permissionKey: 'IncomeList',
    children: [
      { label: '收款清單', route: '/income', permissionKey: 'IncomeList' },
    ],
  },
  {
    label: '系統設定',
    icon: 'settings',
    permissionKey: 'UserList',
    children: [
      { label: '使用者管理', route: '/settings/users', permissionKey: 'UserList' },
      { label: '群組管理', route: '/settings/groups', permissionKey: 'GroupList' },
    ],
  },
];
