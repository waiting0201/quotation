using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Group
{
    public Guid Groupid { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<Grouplim> Grouplims { get; set; } = new List<Grouplim>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
