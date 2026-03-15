using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Spec
{
    public int Specid { get; set; }

    public string? Title { get; set; }

    public string? Entitle { get; set; }

    public string? Description { get; set; }

    public string? Endescription { get; set; }

    public int? Unitprice { get; set; }

    public int? Parentid { get; set; }

    public virtual ICollection<Spec> InverseParent { get; set; } = new List<Spec>();

    public virtual Spec? Parent { get; set; }
}
