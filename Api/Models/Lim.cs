using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Lim
{
    public int Limid { get; set; }

    public string? Key { get; set; }

    public string? Value { get; set; }

    public int Freq { get; set; }

    public string? Icon { get; set; }

    public int Parentid { get; set; }

    public virtual ICollection<Grouplim> Grouplims { get; set; } = new List<Grouplim>();

    public virtual ICollection<Userlim> Userlims { get; set; } = new List<Userlim>();
}
