/** 維護清單項目 */
export interface Host {
  hostId: number;
  item: string;
  url: string | null;
  startDate: string | null;
  expireDate: string | null;
}

/** 建立或更新維護清單項目的表單資料 */
export interface HostCreateUpdate {
  item: string;
  url: string | null;
  startDate: string | null;
  expireDate: string | null;
}
