using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Invoice
{
    public Guid Invoiceid { get; set; }

    public Guid? Incomeid { get; set; }

    public string? Invoicecode { get; set; }

    public int? Customerid { get; set; }

    public DateTime? Requestdate { get; set; }

    public string? Remark { get; set; }

    public int? Tax { get; set; }

    public int? Total { get; set; }

    public short? Status { get; set; }

    public DateTime? Createdate { get; set; }

    public Guid? Userid { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Income? Income { get; set; }

    public virtual ICollection<Invoicedetail> Invoicedetails { get; set; } = new List<Invoicedetail>();
}
