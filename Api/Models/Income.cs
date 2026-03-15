using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Income
{
    public Guid Incomeid { get; set; }

    public int? Customerid { get; set; }

    public string? Incomecode { get; set; }

    public int? Amount { get; set; }

    public int? Fee { get; set; }

    public DateTime? Incomedate { get; set; }

    public DateTime? Createdate { get; set; }

    public string? Remark { get; set; }

    public Guid? Userid { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
