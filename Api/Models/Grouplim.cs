using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Grouplim
{
    public Guid Groupid { get; set; }

    public int Limid { get; set; }

    public bool Isquery { get; set; }

    public bool Isinsert { get; set; }

    public bool Isupdate { get; set; }

    public bool Isdelete { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual Lim Lim { get; set; } = null!;
}
