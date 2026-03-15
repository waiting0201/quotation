using System;
using System.Collections.Generic;

namespace QuotationApi.Models;

public partial class Project
{
    public Guid Projectid { get; set; }

    public string? Projectcode { get; set; }

    /// <summary>
    /// 專案開始日期
    /// </summary>
    public DateTime? Startdate { get; set; }

    public short? Status { get; set; }

    public DateTime? Createdate { get; set; }

    public Guid? Userid { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
