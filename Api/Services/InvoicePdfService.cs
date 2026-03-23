using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuotationApi.DTOs.Invoice;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 請款單 PDF 產生服務（版面參照舊系統 SSRS Invoice.rdlc）
/// </summary>
public class InvoicePdfService
{
    private readonly InvoiceService _invoiceService;
    private readonly QuotationDbContext _db;

    public InvoicePdfService(InvoiceService invoiceService, QuotationDbContext db)
    {
        _invoiceService = invoiceService;
        _db = db;
    }

    public async Task<byte[]?> GeneratePdfAsync(Guid invoiceId)
    {
        var invoice = await _invoiceService.GetByIdAsync(invoiceId);
        if (invoice == null) return null;

        var company = await _db.Aboutus.AsNoTracking().FirstOrDefaultAsync();

        Customer? customer = null;
        if (invoice.CustomerId.HasValue)
            customer = await _db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Customerid == invoice.CustomerId.Value);

        var document = new InvoicePdfDocument(invoice, company, customer);
        return document.GeneratePdf();
    }
}

/// <summary>
/// QuestPDF 請款單版面定義（仿舊系統 SSRS Invoice.rdlc 樣式）
///
/// 版面結構：
/// Page Header: 中文 brand logo（左）、英文 brandeng logo（右）、彩色裝飾條
/// Content:
///   1. 公司資訊（左上）
///   2.「請款單」標題（置中）
///   3. 客戶資訊（表格，含底線欄位）
///   4. 明細表格（編號 / 內容 / 發票號碼 / 金額）+ 合計 / 營業稅 / 總計
///   5. 備註
///   6. 匯款資訊
/// Page Footer: 彩色裝飾條
/// </summary>
internal class InvoicePdfDocument : IDocument
{
    private readonly InvoiceDetailResponseDto _invoice;
    private readonly Aboutu? _company;
    private readonly Customer? _customer;

    // ── 嵌入圖片（從 Assembly Resources 載入）────────────────────────────
    private static readonly byte[] BrandImage = LoadResource("brand.jpg");
    private static readonly byte[] BrandEngImage = LoadResource("brandeng.jpg");
    private static readonly byte[] ColorStripe = LoadResource("color.jpg");
    private static readonly byte[] ColorStripe1 = LoadResource("color1.jpg");

    // ── 色彩（匹配舊系統 RDLC） ─────────────────────────────────────────
    private const string ColorHeaderBg = "#bfbfbf";       // 表頭灰
    private const string ColorSummaryBg = "#bec0bf";      // 合計列灰
    private const string ColorSectionTitle = "#379bd5";   // 區段標題藍
    private const string ColorTitleBg = "#d3d3d3";        // 「請款單」背景淺灰
    private const string ColorBorder = "#d3d3d3";         // 邊框灰
    private const string ColorBlack = "#000000";
    private const string ColorRed = "#ff0000";

    // ── 字型大小（匹配 RDLC：公司名 11pt，內文 9pt）─────────────────────
    private const float FontCompanyTitle = 11f;
    private const float FontBody = 9f;
    private const float FontTitle = 11f;

    public InvoicePdfDocument(
        InvoiceDetailResponseDto invoice, Aboutu? company, Customer? customer)
    {
        _invoice = invoice;
        _company = company;
        _customer = customer;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"請款單 {_invoice.InvoiceCode}",
        Author = _company?.Title ?? "威庭科技",
        Subject = "請款單",
        Creator = "Quotation System"
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            // A4，邊距：上1cm 左1cm 右1cm 下0cm（匹配 RDLC）
            page.Size(PageSizes.A4);
            page.MarginTop(1, Unit.Centimetre);
            page.MarginLeft(1, Unit.Centimetre);
            page.MarginRight(1, Unit.Centimetre);
            page.MarginBottom(0, Unit.Centimetre);
            page.DefaultTextStyle(ts => ts
                .FontFamily("Noto Sans TC")
                .FontSize(FontBody)
                .FontColor(ColorBlack));

            // ── Page Header：品牌 logo + 彩色裝飾條 ────────────────────
            page.Header().Column(col =>
            {
                // Logo 列：中文 brand（左）、英文 brandeng（右）
                col.Item().Row(row =>
                {
                    row.AutoItem().Height(18).Image(BrandImage).FitHeight();
                    row.RelativeItem(); // 中間空白
                    row.AutoItem().Height(24).Image(BrandEngImage).FitHeight();
                });

                // 彩色裝飾條（頂部）
                col.Item().PaddingTop(4).Height(5).Image(ColorStripe).FitWidth();
            });

            // ── Page Footer：彩色裝飾條 ────────────────────────────────
            page.Footer().Height(12).Image(ColorStripe1).FitWidth();

            // ── Content ────────────────────────────────────────────────
            page.Content().PaddingTop(8).Column(col =>
            {
                // 1. 公司資訊
                col.Item().Element(ComposeCompanyInfo);

                // 2. 請款單標題
                col.Item().PaddingTop(10).Element(ComposeTitle);

                // 3. 客戶資訊
                col.Item().PaddingTop(10).Element(ComposeCustomerInfo);

                // 4. 明細表格（含合計列）
                col.Item().PaddingTop(10).Element(ComposeDetailsTable);

                // 5. 備註
                col.Item().PaddingTop(10).Element(ComposeRemark);

                // 6. 匯款資訊
                col.Item().PaddingTop(10).Element(ComposeBankInfo);
            });
        });
    }

    // ── 1. 公司資訊（仿 Tablix1：6 行，公司名 + 電話/傳真/Email/統編/地址）

    private void ComposeCompanyInfo(IContainer container)
    {
        container.Width(240).Column(col =>
        {
            col.Item().Text(_company?.Title ?? "威庭科技有限公司")
                .FontSize(FontCompanyTitle).Bold();
            col.Item().PaddingTop(2).Text($"電話：{_company?.Phone ?? ""}");
            col.Item().Text($"傳真：{_company?.Fax ?? ""}");
            col.Item().Text($"電子郵件：{_company?.Email ?? ""}");
            col.Item().Text($"統一編號：{_company?.Id.ToString() ?? ""}");
            col.Item().Text($"地址：{_company?.Address ?? ""}");
        });
    }

    // ── 2. 請款單標題（置中，淺灰背景，11pt 粗體）

    private void ComposeTitle(IContainer container)
    {
        container.AlignCenter()
            .Width(65)
            .Background(ColorTitleBg)
            .PaddingVertical(3)
            .PaddingHorizontal(8)
            .Text("請款單")
            .FontSize(FontTitle)
            .Bold()
            .AlignCenter();
    }

    // ── 3. 客戶資訊（仿 Tablix2：4 行，標籤 + 底線值）

    private void ComposeCustomerInfo(IContainer container)
    {
        container.Table(table =>
        {
            // 欄寬比例仿 RDLC：1.86 / 8.24 / 1.86 / 7.03 cm
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(70);
                cols.RelativeColumn(8.24f);
                cols.ConstantColumn(70);
                cols.RelativeColumn(7.03f);
            });

            // Row 1: 客戶名稱 / 統一編號
            table.Cell().Element(LabelCell).Text("客戶名稱：");
            table.Cell().Element(UnderlineCell).Text(_customer?.Name ?? "");
            table.Cell().Element(LabelCell).AlignRight().Text("統一編號：");
            table.Cell().Element(UnderlineCell).Text(_customer?.Vatnumber ?? "");

            // Row 2: 客戶地址
            table.Cell().Element(LabelCell).Text("客戶地址：");
            table.Cell().ColumnSpan(3).Element(UnderlineCell).Text(_customer?.Address ?? "");

            // Row 3: 聯絡電話
            table.Cell().Element(LabelCell).Text("聯絡電話：");
            table.Cell().ColumnSpan(3).Element(UnderlineCell).Text(_customer?.Phone ?? "");

            // Row 4: 傳真電話
            table.Cell().Element(LabelCell).Text("傳真電話：");
            table.Cell().ColumnSpan(3).Element(UnderlineCell).Text(_customer?.Fax ?? "");
        });

        static IContainer LabelCell(IContainer c)
            => c.PaddingVertical(3).PaddingHorizontal(2);

        static IContainer UnderlineCell(IContainer c)
            => c.BorderBottom(0.5f).BorderColor("#000000")
                .PaddingVertical(3).PaddingHorizontal(2);
    }

    // ── 4. 明細表格（仿 Tablix3：表頭灰 #bfbfbf + 合計列 #bec0bf + 總計紅字）

    private void ComposeDetailsTable(IContainer container)
    {
        var subtotal = _invoice.Details.Sum(d => d.Price ?? 0);
        var taxTotal = _invoice.Tax ?? 0;
        var grandTotal = subtotal + taxTotal;

        container.Table(table =>
        {
            // 欄寬仿 RDLC：1.57 / 10.08 / 3.64 / 2.84 cm
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(45);     // 編號
                cols.RelativeColumn(10.08f); // 內容
                cols.RelativeColumn(3.64f);  // 發票號碼
                cols.RelativeColumn(2.84f);  // 金額(NTD)
            });

            // ── 表頭
            table.Header(header =>
            {
                header.Cell().Element(ThCell).AlignCenter().Text("編號");
                header.Cell().Element(ThCell).AlignLeft().Text("內容");
                header.Cell().Element(ThCell).AlignCenter().Text("發票號碼");
                header.Cell().Element(ThCell).AlignRight().Text("金額(NTD)");
            });

            // ── 明細列
            for (var i = 0; i < _invoice.Details.Count; i++)
            {
                var d = _invoice.Details[i];
                table.Cell().Element(TdCell).AlignCenter().Text((i + 1).ToString());
                table.Cell().Element(TdCell).AlignLeft().Text(d.Remark ?? "");
                table.Cell().Element(TdCell).AlignCenter().Text(d.InvoiceNumber ?? "");
                table.Cell().Element(TdCell).AlignRight().Text(Fmt(d.Price));
            }

            // ── 合計
            table.Cell().ColumnSpan(2).Element(SumCell);
            table.Cell().Element(SumCell).AlignCenter().Text("合計").Bold();
            table.Cell().Element(SumCell).AlignRight().Text(Fmt(subtotal));

            // ── +營業稅5%
            table.Cell().ColumnSpan(2).Element(SumCell);
            table.Cell().Element(SumCell).AlignCenter().Text("+營業稅5%").Bold();
            table.Cell().Element(SumCell).AlignRight().Text(Fmt(taxTotal));

            // ── 總計（紅字粗體，前綴 NT）
            table.Cell().ColumnSpan(2).Element(SumCell);
            table.Cell().Element(SumCell).AlignCenter().Text("總計").Bold();
            table.Cell().Element(SumCell).AlignRight()
                .Text($"NT{Fmt(grandTotal)}")
                .Bold().FontColor(ColorRed);
        });

        static IContainer ThCell(IContainer c)
            => c.Background(ColorHeaderBg)
                .Border(0.5f).BorderColor("#d3d3d3")
                .PaddingVertical(4).PaddingHorizontal(4)
                .DefaultTextStyle(ts => ts.FontSize(9f).Bold());

        static IContainer TdCell(IContainer c)
            => c.Border(0.5f).BorderColor("#d3d3d3")
                .PaddingVertical(4).PaddingHorizontal(4)
                .DefaultTextStyle(ts => ts.FontSize(9f));

        static IContainer SumCell(IContainer c)
            => c.Border(0.5f).BorderColor("#d3d3d3")
                .PaddingVertical(4).PaddingHorizontal(4)
                .DefaultTextStyle(ts => ts.FontSize(9f));
    }

    // ── 5. 備註（仿 Tablix4：藍色標題 #379bd5）

    private void ComposeRemark(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("備註")
                .Bold().FontColor(ColorSectionTitle);
            col.Item().PaddingTop(2)
                .Text(_invoice.Remark ?? "");
        });
    }

    // ── 6. 匯款資訊（仿 Tablix5：藍色標題 + 2 欄 × 2 列）

    private void ComposeBankInfo(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.RelativeColumn();
            });

            // Header
            table.Cell().ColumnSpan(2).PaddingVertical(3).PaddingHorizontal(2)
                .Text("匯款資訊")
                .Bold().FontColor(ColorSectionTitle);

            // Row 1: 受款銀行 / 戶名
            table.Cell().PaddingVertical(2).PaddingHorizontal(2)
                .Text($"受款銀行：{_company?.Bank ?? ""}");
            table.Cell().PaddingVertical(2).PaddingHorizontal(2)
                .Text($"戶名：{_company?.Title ?? ""}");

            // Row 2: 分行 / 帳號
            table.Cell().PaddingVertical(2).PaddingHorizontal(2)
                .Text($"分行：{_company?.Branch ?? ""}");
            table.Cell().PaddingVertical(2).PaddingHorizontal(2)
                .Text($"帳號：{_company?.Account ?? ""}");
        });
    }

    // ── 輔助方法 ─────────────────────────────────────────────────────────

    private static string Fmt(int? amount)
        => (amount ?? 0).ToString("N0");

    /// <summary>從 Assembly 嵌入資源載入圖片 bytes</summary>
    private static byte[] LoadResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"QuotationApi.Assets.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return [];

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
