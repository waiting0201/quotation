using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QuotationApi.Models;

public partial class QuotationDbContext : DbContext
{
    public QuotationDbContext(DbContextOptions<QuotationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Aboutu> Aboutus { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Customerdetail> Customerdetails { get; set; }

    public virtual DbSet<Customertype> Customertypes { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Grouplim> Grouplims { get; set; }

    public virtual DbSet<Host> Hosts { get; set; }

    public virtual DbSet<Income> Incomes { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Invoicedetail> Invoicedetails { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Itemcontent> Itemcontents { get; set; }

    public virtual DbSet<Itemdetail> Itemdetails { get; set; }

    public virtual DbSet<Lim> Lims { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Spec> Specs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Userlim> Userlims { get; set; }

    public virtual DbSet<Vwquotationspec> Vwquotationspecs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aboutu>(entity =>
        {
            entity.ToTable("aboutus");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Account)
                .HasMaxLength(50)
                .HasColumnName("account");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.Bank)
                .HasMaxLength(50)
                .HasColumnName("bank");
            entity.Property(e => e.Branch)
                .HasMaxLength(15)
                .HasColumnName("branch");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.Enaddress)
                .HasMaxLength(100)
                .HasColumnName("enaddress");
            entity.Property(e => e.Enbank)
                .HasMaxLength(50)
                .HasColumnName("enbank");
            entity.Property(e => e.Enbranch)
                .HasMaxLength(50)
                .HasColumnName("enbranch");
            entity.Property(e => e.Entitle)
                .HasMaxLength(80)
                .HasColumnName("entitle");
            entity.Property(e => e.Fax)
                .HasMaxLength(50)
                .HasColumnName("fax");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.Swift)
                .HasMaxLength(20)
                .HasColumnName("swift");
            entity.Property(e => e.Title)
                .HasMaxLength(80)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("country");

            entity.Property(e => e.Countryid).HasColumnName("countryid");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");

            entity.HasIndex(e => e.Countryid, "IX_FK_customers_country");

            entity.HasIndex(e => e.Customertypeid, "IX_FK_customers_customertypes");

            entity.Property(e => e.Customerid).HasColumnName("customerid");
            entity.Property(e => e.Address)
                .HasMaxLength(80)
                .HasColumnName("address");
            entity.Property(e => e.Code)
                .HasMaxLength(14)
                .HasColumnName("code");
            entity.Property(e => e.Countryid).HasColumnName("countryid");
            entity.Property(e => e.Createdate)
                .HasColumnType("datetime")
                .HasColumnName("createdate");
            entity.Property(e => e.Customertypeid).HasColumnName("customertypeid");
            entity.Property(e => e.Fax)
                .HasMaxLength(50)
                .HasColumnName("fax");
            entity.Property(e => e.Logo)
                .HasMaxLength(20)
                .HasColumnName("logo");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.Vatnumber)
                .HasMaxLength(10)
                .HasColumnName("vatnumber");

            entity.HasOne(d => d.Country).WithMany(p => p.Customers)
                .HasForeignKey(d => d.Countryid)
                .HasConstraintName("FK_customers_country");

            entity.HasOne(d => d.Customertype).WithMany(p => p.Customers)
                .HasForeignKey(d => d.Customertypeid)
                .HasConstraintName("FK_customers_customertypes");
        });

        modelBuilder.Entity<Customerdetail>(entity =>
        {
            entity.ToTable("customerdetails");

            entity.Property(e => e.Customerdetailid)
                .ValueGeneratedNever()
                .HasColumnName("customerdetailid");
            entity.Property(e => e.Customerid).HasColumnName("customerid");
            entity.Property(e => e.Email)
                .HasMaxLength(80)
                .HasColumnName("email");
            entity.Property(e => e.Ext)
                .HasMaxLength(10)
                .HasColumnName("ext");
            entity.Property(e => e.Freq)
                .HasDefaultValue(0)
                .HasColumnName("freq");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");

            entity.HasOne(d => d.Customer).WithMany(p => p.Customerdetails)
                .HasForeignKey(d => d.Customerid)
                .HasConstraintName("FK_customerdetails_customers");
        });

        modelBuilder.Entity<Customertype>(entity =>
        {
            entity.ToTable("customertypes");

            entity.Property(e => e.Customertypeid).HasColumnName("customertypeid");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Groupid).HasName("PK_group_1");

            entity.ToTable("group");

            entity.Property(e => e.Groupid)
                .ValueGeneratedNever()
                .HasColumnName("groupid");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Grouplim>(entity =>
        {
            entity.HasKey(e => new { e.Groupid, e.Limid }).HasName("PK_grouplim_1");

            entity.ToTable("grouplim");

            entity.HasIndex(e => e.Limid, "IX_FK_grouplim_lim");

            entity.Property(e => e.Groupid).HasColumnName("groupid");
            entity.Property(e => e.Limid).HasColumnName("limid");
            entity.Property(e => e.Isdelete).HasColumnName("isdelete");
            entity.Property(e => e.Isinsert).HasColumnName("isinsert");
            entity.Property(e => e.Isquery).HasColumnName("isquery");
            entity.Property(e => e.Isupdate).HasColumnName("isupdate");

            entity.HasOne(d => d.Group).WithMany(p => p.Grouplims)
                .HasForeignKey(d => d.Groupid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_grouplim_group");

            entity.HasOne(d => d.Lim).WithMany(p => p.Grouplims)
                .HasForeignKey(d => d.Limid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_grouplim_lim");
        });

        modelBuilder.Entity<Host>(entity =>
        {
            entity.HasKey(e => e.Hostid).HasName("PK_hosts_1");

            entity.ToTable("hosts");

            entity.Property(e => e.Hostid)
                .ValueGeneratedNever()
                .HasColumnName("hostid");
            entity.Property(e => e.Expiredate)
                .HasColumnType("datetime")
                .HasColumnName("expiredate");
            entity.Property(e => e.Item).HasColumnName("item");
            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Startdate)
                .HasColumnType("datetime")
                .HasColumnName("startdate");
            entity.Property(e => e.Url).HasColumnName("url");

            entity.HasOne(d => d.ItemNavigation).WithMany(p => p.Hosts)
                .HasForeignKey(d => d.Itemid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_hosts_items");
        });

        modelBuilder.Entity<Income>(entity =>
        {
            entity.ToTable("incomes");

            entity.Property(e => e.Incomeid)
                .ValueGeneratedNever()
                .HasColumnName("incomeid");
            entity.Property(e => e.Amount)
                .HasDefaultValue(0)
                .HasColumnName("amount");
            entity.Property(e => e.Createdate)
                .HasColumnType("datetime")
                .HasColumnName("createdate");
            entity.Property(e => e.Customerid).HasColumnName("customerid");
            entity.Property(e => e.Fee)
                .HasDefaultValue(0)
                .HasColumnName("fee");
            entity.Property(e => e.Incomecode)
                .HasMaxLength(14)
                .HasColumnName("incomecode");
            entity.Property(e => e.Incomedate)
                .HasColumnType("datetime")
                .HasColumnName("incomedate");
            entity.Property(e => e.Remark)
                .HasColumnType("ntext")
                .HasColumnName("remark");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Customer).WithMany(p => p.Incomes)
                .HasForeignKey(d => d.Customerid)
                .HasConstraintName("FK_incomes_customers");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");

            entity.Property(e => e.Invoiceid)
                .ValueGeneratedNever()
                .HasColumnName("invoiceid");
            entity.Property(e => e.Createdate)
                .HasColumnType("datetime")
                .HasColumnName("createdate");
            entity.Property(e => e.Customerid).HasColumnName("customerid");
            entity.Property(e => e.Incomeid).HasColumnName("incomeid");
            entity.Property(e => e.Invoicecode)
                .HasMaxLength(14)
                .HasColumnName("invoicecode");
            entity.Property(e => e.Remark)
                .HasColumnType("ntext")
                .HasColumnName("remark");
            entity.Property(e => e.Requestdate)
                .HasColumnType("datetime")
                .HasColumnName("requestdate");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)0)
                .HasColumnName("status");
            entity.Property(e => e.Tax)
                .HasDefaultValue(0)
                .HasColumnName("tax");
            entity.Property(e => e.Total)
                .HasDefaultValue(0)
                .HasColumnName("total");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.Customerid)
                .HasConstraintName("FK_invoices_customers");

            entity.HasOne(d => d.Income).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.Incomeid)
                .HasConstraintName("FK_invoices_incomes");
        });

        modelBuilder.Entity<Invoicedetail>(entity =>
        {
            entity.ToTable("invoicedetails");

            entity.Property(e => e.Invoicedetailid)
                .ValueGeneratedNever()
                .HasColumnName("invoicedetailid");
            entity.Property(e => e.Freq)
                .HasDefaultValue(0)
                .HasColumnName("freq");
            entity.Property(e => e.Invoicedate)
                .HasColumnType("datetime")
                .HasColumnName("invoicedate");
            entity.Property(e => e.Invoiceid).HasColumnName("invoiceid");
            entity.Property(e => e.Invoicenumber)
                .HasMaxLength(10)
                .HasColumnName("invoicenumber");
            entity.Property(e => e.Invoicetype)
                .HasDefaultValue((short)0)
                .HasColumnName("invoicetype");
            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Remark)
                .HasMaxLength(250)
                .HasColumnName("remark");
            entity.Property(e => e.Tax)
                .HasDefaultValue(0)
                .HasColumnName("tax");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Invoicedetails)
                .HasForeignKey(d => d.Invoiceid)
                .HasConstraintName("FK_invoicedetails_invoices");

            entity.HasOne(d => d.Item).WithMany(p => p.Invoicedetails)
                .HasForeignKey(d => d.Itemid)
                .HasConstraintName("FK_invoicedetails_items");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("items");

            entity.HasIndex(e => e.Customerid, "IX_FK_items_customers");

            entity.HasIndex(e => e.Userid, "IX_FK_items_user");

            entity.Property(e => e.Itemid)
                .ValueGeneratedNever()
                .HasColumnName("itemid");
            entity.Property(e => e.Createdate)
                .HasColumnType("datetime")
                .HasColumnName("createdate");
            entity.Property(e => e.Customerdetailid).HasColumnName("customerdetailid");
            entity.Property(e => e.Customerid).HasColumnName("customerid");
            entity.Property(e => e.Deadline)
                .HasColumnType("datetime")
                .HasColumnName("deadline");
            entity.Property(e => e.Discount)
                .HasDefaultValue(0)
                .HasColumnName("discount");
            entity.Property(e => e.Enname)
                .HasMaxLength(80)
                .HasColumnName("enname");
            entity.Property(e => e.Enpayment)
                .HasMaxLength(200)
                .HasColumnName("enpayment");
            entity.Property(e => e.Enremark)
                .HasColumnType("ntext")
                .HasColumnName("enremark");
            entity.Property(e => e.Enversion).HasColumnName("enversion");
            entity.Property(e => e.Expiredate)
                .HasColumnType("datetime")
                .HasColumnName("expiredate");
            entity.Property(e => e.Income)
                .HasDefaultValue(0)
                .HasColumnName("income");
            entity.Property(e => e.Itemcode)
                .HasMaxLength(14)
                .HasColumnName("itemcode");
            entity.Property(e => e.Map)
                .HasMaxLength(50)
                .HasColumnName("map");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Payment)
                .HasMaxLength(200)
                .HasComment("付款條件")
                .HasColumnName("payment");
            entity.Property(e => e.Projectid).HasColumnName("projectid");
            entity.Property(e => e.Quotationdate)
                .HasColumnType("datetime")
                .HasColumnName("quotationdate");
            entity.Property(e => e.Remark)
                .HasColumnType("ntext")
                .HasColumnName("remark");
            entity.Property(e => e.Signdate)
                .HasColumnType("datetime")
                .HasColumnName("signdate");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Tax)
                .HasDefaultValue(0)
                .HasColumnName("tax");
            entity.Property(e => e.Taxtype)
                .HasDefaultValue((short)0)
                .HasColumnName("taxtype");
            entity.Property(e => e.Total)
                .HasDefaultValue(0)
                .HasColumnName("total");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Workdays)
                .HasDefaultValue(0)
                .HasColumnName("workdays");

            entity.HasOne(d => d.Customerdetail).WithMany(p => p.Items)
                .HasForeignKey(d => d.Customerdetailid)
                .HasConstraintName("FK_items_customerdetails");

            entity.HasOne(d => d.Customer).WithMany(p => p.Items)
                .HasForeignKey(d => d.Customerid)
                .HasConstraintName("FK_items_customers");

            entity.HasOne(d => d.Project).WithMany(p => p.Items)
                .HasForeignKey(d => d.Projectid)
                .HasConstraintName("FK_items_projects");
        });

        modelBuilder.Entity<Itemcontent>(entity =>
        {
            entity.ToTable("itemcontents");

            entity.Property(e => e.Itemcontentid)
                .ValueGeneratedNever()
                .HasColumnName("itemcontentid");
            entity.Property(e => e.Freq)
                .HasDefaultValue(0)
                .HasColumnName("freq");
            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Price)
                .HasDefaultValue(0)
                .HasColumnName("price");
            entity.Property(e => e.Remark)
                .HasColumnType("ntext")
                .HasColumnName("remark");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");

            entity.HasOne(d => d.Item).WithMany(p => p.Itemcontents)
                .HasForeignKey(d => d.Itemid)
                .HasConstraintName("FK_itemcontents_items");
        });

        modelBuilder.Entity<Itemdetail>(entity =>
        {
            entity.ToTable("itemdetails");

            entity.HasIndex(e => e.Itemid, "IX_FK_itemdetails_items");

            entity.Property(e => e.Itemdetailid)
                .ValueGeneratedNever()
                .HasColumnName("itemdetailid");
            entity.Property(e => e.Description)
                .HasColumnType("ntext")
                .HasColumnName("description");
            entity.Property(e => e.Endescription)
                .HasColumnType("ntext")
                .HasColumnName("endescription");
            entity.Property(e => e.Entitle)
                .HasMaxLength(80)
                .HasColumnName("entitle");
            entity.Property(e => e.Freq)
                .HasDefaultValue(0)
                .HasColumnName("freq");
            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Price)
                .HasDefaultValue(0)
                .HasColumnName("price");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.Specid).HasColumnName("specid");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
            entity.Property(e => e.Total)
                .HasDefaultValue(0)
                .HasColumnName("total");

            entity.HasOne(d => d.Item).WithMany(p => p.Itemdetails)
                .HasForeignKey(d => d.Itemid)
                .HasConstraintName("FK_itemdetails_items");
        });

        modelBuilder.Entity<Lim>(entity =>
        {
            entity.ToTable("lim");

            entity.Property(e => e.Limid).HasColumnName("limid");
            entity.Property(e => e.Freq).HasColumnName("freq");
            entity.Property(e => e.Icon)
                .HasMaxLength(20)
                .HasColumnName("icon");
            entity.Property(e => e.Key)
                .HasMaxLength(50)
                .HasColumnName("key");
            entity.Property(e => e.Parentid).HasColumnName("parentid");
            entity.Property(e => e.Value)
                .HasMaxLength(50)
                .HasColumnName("value");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.Property(e => e.Paymentid).HasColumnName("paymentid");
            entity.Property(e => e.Remark)
                .HasColumnType("ntext")
                .HasColumnName("remark");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");

            entity.Property(e => e.Projectid)
                .ValueGeneratedNever()
                .HasColumnName("projectid");
            entity.Property(e => e.Createdate)
                .HasColumnType("datetime")
                .HasColumnName("createdate");
            entity.Property(e => e.Projectcode)
                .HasMaxLength(14)
                .HasColumnName("projectcode");
            entity.Property(e => e.Startdate)
                .HasComment("專案開始日期")
                .HasColumnName("startdate");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)0)
                .HasColumnName("status");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<Spec>(entity =>
        {
            entity.ToTable("specs");

            entity.Property(e => e.Specid).HasColumnName("specid");
            entity.Property(e => e.Description)
                .HasColumnType("ntext")
                .HasColumnName("description");
            entity.Property(e => e.Endescription)
                .HasColumnType("ntext")
                .HasColumnName("endescription");
            entity.Property(e => e.Entitle)
                .HasMaxLength(80)
                .HasColumnName("entitle");
            entity.Property(e => e.Parentid).HasColumnName("parentid");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
            entity.Property(e => e.Unitprice).HasColumnName("unitprice");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.Parentid)
                .HasConstraintName("FK_specs_specs");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");

            entity.Property(e => e.Userid)
                .ValueGeneratedNever()
                .HasColumnName("userid");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.Groupid).HasColumnName("groupid");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");
            entity.Property(e => e.Status)
                .HasDefaultValue(false)
                .HasColumnName("status");
            entity.Property(e => e.Updatetime)
                .HasColumnType("datetime")
                .HasColumnName("updatetime");

            entity.HasOne(d => d.Group).WithMany(p => p.Users)
                .HasForeignKey(d => d.Groupid)
                .HasConstraintName("FK_user_group");
        });

        modelBuilder.Entity<Userlim>(entity =>
        {
            entity.HasKey(e => new { e.Userid, e.Limid });

            entity.ToTable("userlim");

            entity.HasIndex(e => e.Limid, "IX_FK_userlim_lim");

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Limid).HasColumnName("limid");
            entity.Property(e => e.Isdelete).HasColumnName("isdelete");
            entity.Property(e => e.Isinsert).HasColumnName("isinsert");
            entity.Property(e => e.Isquery).HasColumnName("isquery");
            entity.Property(e => e.Isupdate).HasColumnName("isupdate");

            entity.HasOne(d => d.Lim).WithMany(p => p.Userlims)
                .HasForeignKey(d => d.Limid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_userlim_lim");

            entity.HasOne(d => d.User).WithMany(p => p.Userlims)
                .HasForeignKey(d => d.Userid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_userlim_user");
        });

        modelBuilder.Entity<Vwquotationspec>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwquotationspecs");

            entity.Property(e => e.Description)
                .HasColumnType("ntext")
                .HasColumnName("description");
            entity.Property(e => e.Endescription)
                .HasColumnType("ntext")
                .HasColumnName("endescription");
            entity.Property(e => e.Entitle)
                .HasMaxLength(80)
                .HasColumnName("entitle");
            entity.Property(e => e.Freq).HasColumnName("freq");
            entity.Property(e => e.Itemdetailid).HasColumnName("itemdetailid");
            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Ptitle)
                .HasMaxLength(50)
                .HasColumnName("ptitle");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Specid).HasColumnName("specid");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
            entity.Property(e => e.Total).HasColumnName("total");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
