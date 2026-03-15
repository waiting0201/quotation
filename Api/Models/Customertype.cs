using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Customertype
{
    public int Customertypeid { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
