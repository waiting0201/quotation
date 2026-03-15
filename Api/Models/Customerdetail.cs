using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Customerdetail
{
    public Guid Customerdetailid { get; set; }

    public int? Customerid { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Ext { get; set; }

    public int? Freq { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
