using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Customer
{
    public int Customerid { get; set; }

    public int? Customertypeid { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public int? Countryid { get; set; }

    public string? Logo { get; set; }

    public string? Phone { get; set; }

    public string? Fax { get; set; }

    public string? Vatnumber { get; set; }

    public DateTime? Createdate { get; set; }

    public virtual Country? Country { get; set; }

    public virtual ICollection<Customerdetail> Customerdetails { get; set; } = new List<Customerdetail>();

    public virtual Customertype? Customertype { get; set; }

    public virtual ICollection<Income> Incomes { get; set; } = new List<Income>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
