using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Payment
{
    public int Paymentid { get; set; }

    public string? Remark { get; set; }
}
