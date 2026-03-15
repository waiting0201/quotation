using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Itemcontent
{
    public Guid Itemcontentid { get; set; }

    public Guid? Itemid { get; set; }

    public string? Title { get; set; }

    public string? Remark { get; set; }

    public int? Price { get; set; }

    public int? Freq { get; set; }

    public virtual Item? Item { get; set; }
}
