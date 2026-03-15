using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class User
{
    public Guid Userid { get; set; }

    public Guid? Groupid { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Name { get; set; }

    public DateTime? Updatetime { get; set; }

    public bool? Status { get; set; }

    public virtual Group? Group { get; set; }

    public virtual ICollection<Userlim> Userlims { get; set; } = new List<Userlim>();
}
