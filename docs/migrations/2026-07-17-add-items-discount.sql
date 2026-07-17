-- 報價單折扣百分比（0-100 整數，0=無折扣）
-- 折扣套用在未稅小計上，打折後再依 taxtype 計稅；items.tax / items.total 存的是折後值。
ALTER TABLE dbo.items
    ADD discount int NULL
        CONSTRAINT DF_items_discount DEFAULT ((0));
GO
UPDATE dbo.items SET discount = 0 WHERE discount IS NULL;
GO
