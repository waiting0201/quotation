using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Invoicedetail
{
    public Guid Invoicedetailid { get; set; }

    public Guid? Invoiceid { get; set; }

    public Guid? Itemid { get; set; }

    public short? Invoicetype { get; set; }

    public DateTime? Invoicedate { get; set; }

    public string? Invoicenumber { get; set; }

    public int? Price { get; set; }

    public int? Tax { get; set; }

    public string? Remark { get; set; }

    public int? Freq { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual Item? Item { get; set; }
}
