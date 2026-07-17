using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Item
{
    public Guid Itemid { get; set; }

    public Guid? Projectid { get; set; }

    public string? Itemcode { get; set; }

    public bool Enversion { get; set; }

    public string? Name { get; set; }

    public string? Enname { get; set; }

    public int? Customerid { get; set; }

    public Guid? Customerdetailid { get; set; }

    public DateTime? Quotationdate { get; set; }

    public DateTime? Expiredate { get; set; }

    public DateTime? Signdate { get; set; }

    public int? Workdays { get; set; }

    public DateTime? Deadline { get; set; }

    /// <summary>
    /// 付款條件
    /// </summary>
    public string? Payment { get; set; }

    public string? Enpayment { get; set; }

    public string? Remark { get; set; }

    public string? Enremark { get; set; }

    public string? Map { get; set; }

    public short? Taxtype { get; set; }

    /// <summary>
    /// 折扣百分比（0-100 整數，0=無折扣）；套用在未稅小計上，打折後再依 taxtype 計稅
    /// </summary>
    public int? Discount { get; set; }

    public int? Tax { get; set; }

    public int? Total { get; set; }

    public int? Income { get; set; }

    public short? Status { get; set; }

    public DateTime? Createdate { get; set; }

    public Guid? Userid { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Customerdetail? Customerdetail { get; set; }

    public virtual ICollection<Host> Hosts { get; set; } = new List<Host>();

    public virtual ICollection<Invoicedetail> Invoicedetails { get; set; } = new List<Invoicedetail>();

    public virtual ICollection<Itemcontent> Itemcontents { get; set; } = new List<Itemcontent>();

    public virtual ICollection<Itemdetail> Itemdetails { get; set; } = new List<Itemdetail>();

    public virtual Project? Project { get; set; }
}
