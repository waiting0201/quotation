using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Vwquotationspec
{
    public Guid Itemdetailid { get; set; }

    public Guid? Itemid { get; set; }

    public int? Specid { get; set; }

    public string? Title { get; set; }

    public string? Entitle { get; set; }

    public string? Description { get; set; }

    public string? Endescription { get; set; }

    public int? Quantity { get; set; }

    public int? Price { get; set; }

    public int? Total { get; set; }

    public int? Freq { get; set; }

    public string? Ptitle { get; set; }
}
