using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Host
{
    public int Hostid { get; set; }

    public Guid? Itemid { get; set; }

    public string Item { get; set; } = null!;

    public string? Url { get; set; }

    public DateTime? Startdate { get; set; }

    public DateTime? Expiredate { get; set; }

    public virtual Item? ItemNavigation { get; set; }
}
