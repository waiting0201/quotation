using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Elements;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuotationApi.DTOs.Quotation;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 報價單 PDF 產生服務（版面參照舊系統 SSRS Quotation.rdlc）
/// </summary>
public class QuotationPdfService
{
    private readonly QuotationService _quotationService;
    private readonly QuotationDbContext _db;

    public QuotationPdfService(QuotationService quotationService, QuotationDbContext db)
    {
        _quotationService = quotationService;
        _db = db;
    }

    public async Task<byte[]?> GeneratePdfAsync(Guid itemId)
    {
        var quotation = await _quotationService.GetByIdAsync(itemId);
        if (quotation == null) return null;

        var company = await _db.Aboutus.AsNoTracking().FirstOrDefaultAsync();

        Customer? customer = null;
        if (quotation.CustomerId.HasValue)
            customer = await _db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Customerid == quotation.CustomerId.Value);

        Customerdetail? contactPerson = null;
        if (quotation.CustomerDetailId.HasValue)
            contactPerson = await _db.Customerdetails.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Customerdetailid == quotation.CustomerDetailId.Value);

        // Workdays は DTO に含まれないため、直接 Items テーブルから取得
        var item = await _db.Items.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Itemid == itemId);

        var document = new QuotationPdfDocument(quotation, company, customer, contactPerson, item);
        return document.GeneratePdf();
    }
}

/// <summary>
/// QuestPDF 報價單版面定義（仿舊系統 SSRS Quotation.rdlc 樣式）
///
/// 版面結構：
/// Page Header: 中文 brand logo（左）、英文 brandeng logo（右）、彩色裝飾條
/// Content:
///   1. 公司資訊（左上）+ 日期/單號方塊（右上）
///   2. 分隔線
///   3. 「報價單」標題（置中，淺灰背景）
///   4. 專案名稱行（專案名稱 + 預計工作天）
///   5. 客戶資訊（底線欄位表格）
///   6. 內容報價表格（編號 / 標題 / 說明 / 金額）+ 小計 / 營業稅 / 總計
///   7. 付款條件
///   8. 備註
/// Page 2（架構說明，有明細時顯示）:
///   9. 架構說明表格（子項目 / 說明）
/// Page Footer: 確認聲明 + 簽名欄 + 彩色裝飾條
/// </summary>
internal class QuotationPdfDocument : IDocument
{
    private readonly QuotationDetailDto _quotation;
    private readonly Aboutu? _company;
    private readonly Customer? _customer;
    private readonly Customerdetail? _contactPerson;
    private readonly Item? _item;

    // ── 嵌入圖片（從 Assembly Resources 載入）────────────────────────────
    private static readonly byte[] BrandImage = LoadResource("brand.jpg");
    private static readonly byte[] BrandEngImage = LoadResource("brandeng.jpg");
    private static readonly byte[] ColorStripe = LoadResource("color.jpg");
    private static readonly byte[] ColorStripe1 = LoadResource("color1.jpg");
    private static readonly byte[] StampImage = LoadResource("stamp.jpg");

    // ── 色彩（匹配舊系統 RDLC） ─────────────────────────────────────────
    private const string ColorHeaderBg = "#bfbfbf";       // 表頭灰
    private const string ColorSectionTitle = "#379bd5";   // 區段標題藍
    private const string ColorTitleBg = "#d3d3d3";        // 「報價單」背景淺灰
    private const string ColorBorder = "#d3d3d3";         // 邊框灰
    private const string ColorBlack = "#000000";
    private const string ColorRed = "#ff0000";

    // ── 字型大小（匹配 RDLC：公司名 11pt，內文 9pt）─────────────────────
    private const float FontCompanyTitle = 11f;
    private const float FontBody = 9f;
    private const float FontTitle = 11f;
    private const float FontProjectName = 12f;

    public QuotationPdfDocument(
        QuotationDetailDto quotation,
        Aboutu? company,
        Customer? customer,
        Customerdetail? contactPerson,
        Item? item)
    {
        _quotation = quotation;
        _company = company;
        _customer = customer;
        _contactPerson = contactPerson;
        _item = item;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"報價單 {_quotation.ItemCode}",
        Author = _company?.Title ?? "威庭科技",
        Subject = "報價單",
        Creator = "Quotation System"
    };

    public void Compose(IDocumentContainer container)
    {
        // ── 第一頁：報價主體 ─────────────────────────────────────────────
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

            // ── Page Header：品牌 logo + 彩色裝飾條 ──────────────────────
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

            // ── Page Footer：固定預留簽名空間 + 彩色裝飾條 ───────────────
            // 每頁 Footer 高度一致（含簽名預留區），確保分頁判斷把簽名算進去；
            // 簽名實際內容僅於最後一頁渲染，前面各頁該區域留白。
            page.Footer().Dynamic(new LastPageSignatureFooter
            {
                State = new LastPageSignatureFooter.SignatureState(
                    CustomerName: _customer?.Name ?? "",
                    StampImage: StampImage,
                    ColorStripe: ColorStripe1)
            });

            // ── Content ────────────────────────────────────────────────────
            page.Content().PaddingTop(8).Column(col =>
            {
                // 1. 公司資訊 + 日期/單號方塊
                col.Item().Element(ComposeHeaderSection);

                // 2. 分隔線
                col.Item().PaddingTop(8).BorderBottom(0.5f).BorderColor(ColorBorder);

                // 3. 「報價單」標題
                col.Item().PaddingTop(8).Element(ComposeTitle);

                // 4. 專案名稱行
                col.Item().PaddingTop(8).Element(ComposeProjectRow);

                // 5. 客戶資訊
                col.Item().PaddingTop(8).Element(ComposeCustomerInfo);

                // 6. 內容報價表格（含合計列）
                col.Item().PaddingTop(8).Element(ComposeContentsTable);

                // 7. 付款條件
                col.Item().PaddingTop(8).Element(ComposePayment);

                // 8. 備註
                if (!string.IsNullOrWhiteSpace(_quotation.Remark))
                    col.Item().PaddingTop(8).Element(ComposeRemark);
            });
        });
    }

    // ── 1. 公司資訊 + 日期/單號方塊（仿 Tablix1 + Tablix3）───────────────

    private void ComposeHeaderSection(IContainer container)
    {
        container.Row(row =>
        {
            // 左：公司資訊（~6.4cm）
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_company?.Title ?? "威庭科技有限公司")
                    .FontSize(FontCompanyTitle).Bold();
                col.Item().PaddingTop(2).Text(_company?.Entitle ?? "Weypro Technology Ltd.")
                    .FontSize(9f).Bold();
                col.Item().PaddingTop(2).Text($"Tel: {_company?.Phone ?? ""}");
                col.Item().Text($"Fax: {_company?.Fax ?? ""}");
                col.Item().Text($"E-mail: {_company?.Email ?? ""}");
                col.Item().Text($"統一編號: {_company?.Id.ToString() ?? ""}");
                col.Item().Text($"地址: {_company?.Address ?? ""}");
            });

            row.ConstantItem(20); // 間距

            // 右：日期/單號方塊（~4.7cm，有外框）
            row.ConstantItem(178).Border(0.5f).BorderColor(ColorBorder).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1.5f);
                });

                table.Cell().Element(InfoLabelCell).Text("報價日期");
                table.Cell().Element(InfoValueCell)
                    .Text(_quotation.QuotationDate?.ToString("yyyy/MM/dd") ?? "");

                table.Cell().Element(InfoLabelCell).Text("有效日期");
                table.Cell().Element(InfoValueCell)
                    .Text(_quotation.ExpireDate?.ToString("yyyy/MM/dd") ?? "");

                table.Cell().Element(InfoLabelCell).Text("報價單號");
                table.Cell().Element(InfoValueCell).Text(_quotation.ItemCode);
            });
        });

        static IContainer InfoLabelCell(IContainer c)
            => c.BorderBottom(0.5f).BorderColor("#d3d3d3")
                .PaddingVertical(4).PaddingHorizontal(4);

        static IContainer InfoValueCell(IContainer c)
            => c.BorderBottom(0.5f).BorderColor("#d3d3d3")
                .PaddingVertical(4).PaddingHorizontal(4);
    }

    // ── 3. 「報價單」標題（置中，淺灰背景，11pt 粗體）─────────────────────

    private void ComposeTitle(IContainer container)
    {
        container.AlignCenter()
            .Width(65)
            .Background(ColorTitleBg)
            .PaddingVertical(3)
            .PaddingHorizontal(8)
            .Text("報價單")
            .FontSize(FontTitle)
            .Bold()
            .AlignCenter();
    }

    // ── 4. 專案名稱行（仿 Tablix6：左 = 專案名稱，右 = 預計工作天）────────

    private void ComposeProjectRow(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("專案名稱：").FontSize(FontBody);
                text.Span(_quotation.Name ?? "").FontSize(FontProjectName).Bold();
            });

            var workdays = _item?.Workdays;
            if (workdays.HasValue)
            {
                row.AutoItem().AlignRight().Text($"預計工作天：{workdays.Value} 天")
                    .FontSize(FontBody);
            }
        });
    }

    // ── 5. 客戶資訊（仿 Tablix2：4 行，標籤 + 底線值）──────────────────────

    private void ComposeCustomerInfo(IContainer container)
    {
        container.Table(table =>
        {
            // 欄寬比例仿 RDLC：1.86 / 8.24 / 1.89 / 6.89 cm
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(70);         // 標籤 1
                cols.RelativeColumn(8.24f);      // 值 1
                cols.ConstantColumn(72);         // 標籤 2
                cols.RelativeColumn(6.89f);      // 值 2
            });

            // Row 1: 客戶名稱 / 聯絡人（from customerdetails）
            table.Cell().Element(LabelCell).Text("客戶名稱：");
            table.Cell().Element(UnderlineCell).Text(_customer?.Name ?? "");
            table.Cell().Element(LabelCell).AlignRight().Text("聯絡人：");
            table.Cell().Element(UnderlineCell).Text(_contactPerson?.Name ?? "");

            // Row 2: 客戶地址（span 3）
            table.Cell().Element(LabelCell).Text("客戶地址：");
            table.Cell().ColumnSpan(3).Element(UnderlineCell).Text(_customer?.Address ?? "");

            // Row 3: E-mail（from customerdetails）/ 聯絡電話
            table.Cell().Element(LabelCell).Text("E-mail：");
            table.Cell().Element(UnderlineCell).Text(_contactPerson?.Email ?? "");
            table.Cell().Element(LabelCell).AlignRight().Text("聯絡電話：");
            table.Cell().Element(UnderlineCell).Text(_contactPerson?.Phone ?? _customer?.Phone ?? "");

            // Row 4: 統一編號 / 傳真電話
            table.Cell().Element(LabelCell).Text("統一編號：");
            table.Cell().Element(UnderlineCell).Text(_customer?.Vatnumber ?? "");
            table.Cell().Element(LabelCell).AlignRight().Text("傳真電話：");
            table.Cell().Element(UnderlineCell).Text(_customer?.Fax ?? "");
        });

        static IContainer LabelCell(IContainer c)
            => c.PaddingVertical(3).PaddingHorizontal(2);

        static IContainer UnderlineCell(IContainer c)
            => c.BorderBottom(0.5f).BorderColor("#000000")
                .PaddingVertical(3).PaddingHorizontal(2);
    }

    // ── 6. 內容報價表格（仿 Tablix7：表頭灰 + 合計列 + 總計紅字）────────────

    private void ComposeContentsTable(IContainer container)
    {
        var subtotal = _quotation.Details.Sum(d => d.Total ?? 0)
                     + _quotation.Contents.Sum(c => c.Price ?? 0);
        var taxTotal = _quotation.Tax ?? 0;
        var grandTotal = _quotation.Total ?? (subtotal + taxTotal);
        var taxType = _quotation.TaxType ?? 0;

        container.Table(table =>
        {
            // 欄寬仿 RDLC：1.25 / 7.72 / 7.45 / 2.5 cm
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(38);         // 編號
                cols.RelativeColumn(7.72f);      // 標題
                cols.RelativeColumn(7.45f);      // 說明
                cols.ConstantColumn(76);         // 金額(NTD)
            });

            // ── 表頭
            table.Header(header =>
            {
                header.Cell().Element(ThCell).AlignCenter().Text("編號");
                header.Cell().Element(ThCell).AlignLeft().Text("標題");
                header.Cell().Element(ThCell).AlignLeft().Text("說明");
                header.Cell().Element(ThCell).AlignRight().Text("金額(NTD)");
            });

            // ── 內容列（Contents）
            var rowNo = 0;
            for (var i = 0; i < _quotation.Contents.Count; i++)
            {
                rowNo++;
                var c = _quotation.Contents[i];
                table.Cell().Element(TdCell).AlignCenter().Text(rowNo.ToString());
                table.Cell().Element(TdCell).AlignLeft().Text(c.Title ?? "");
                table.Cell().Element(TdCell).AlignLeft().Text(c.Remark ?? "");
                table.Cell().Element(TdCell).AlignRight().Text(Fmt(c.Price));
            }

            // ── 明細列（Details，接續編號）
            foreach (var d in _quotation.Details)
            {
                rowNo++;
                table.Cell().Element(TdCell).AlignCenter().Text(rowNo.ToString());
                table.Cell().Element(TdCell).AlignLeft().Text(d.Title ?? "");
                table.Cell().Element(TdCell).AlignLeft().Text(d.Description ?? "");
                table.Cell().Element(TdCell).AlignRight().Text(Fmt(d.Total));
            }

            // ── 合計區（依稅別顯示不同內容）
            switch (taxType)
            {
                case 0: // 稅外加：小計 → +營業稅5% → 總計
                    table.Cell().ColumnSpan(3).Element(SumCell).AlignRight().Text("小計");
                    table.Cell().Element(SumCell).AlignRight().Text(Fmt(subtotal));

                    table.Cell().ColumnSpan(3).Element(SumCell).AlignRight().Text("+營業稅5%");
                    table.Cell().Element(SumCell).AlignRight().Text(Fmt(taxTotal));

                    table.Cell().ColumnSpan(3).Element(SumCell).AlignRight()
                        .Text("總計").Bold();
                    table.Cell().Element(SumCell).AlignRight()
                        .Text($"NT{Fmt(grandTotal)}")
                        .Bold().FontColor(ColorRed);
                    break;

                case 1: // 稅內含：合計（含稅）→ 內含營業稅（僅標示，不加總）
                    table.Cell().ColumnSpan(3).Element(SumCell).AlignRight()
                        .Text("合計（含稅）").Bold();
                    table.Cell().Element(SumCell).AlignRight()
                        .Text($"NT{Fmt(grandTotal)}")
                        .Bold().FontColor(ColorRed);

                    table.Cell().ColumnSpan(3).Element(SumCell).AlignRight().Text("內含營業稅");
                    table.Cell().Element(SumCell).AlignRight().Text(Fmt(taxTotal));
                    break;

                default: // 免稅：只顯示合計
                    table.Cell().ColumnSpan(3).Element(SumCell).AlignRight()
                        .Text("合計").Bold();
                    table.Cell().Element(SumCell).AlignRight()
                        .Text($"NT{Fmt(grandTotal)}")
                        .Bold().FontColor(ColorRed);
                    break;
            }
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

    // ── 7. 付款條件（仿 Tablix4：藍色標題 #379bd5）──────────────────────────

    private void ComposePayment(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("付款條件：")
                .Bold().FontColor(ColorSectionTitle);
            col.Item().PaddingTop(2)
                .Text(_quotation.Payment ?? "");
        });
    }

    // ── 8. 備註（仿 Tablix8：藍色標題 #379bd5）──────────────────────────────

    private void ComposeRemark(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("備註：")
                .Bold().FontColor(ColorSectionTitle);
            col.Item().PaddingTop(2)
                .Text(_quotation.Remark ?? "");
        });
    }

    // ── 輔助方法 ─────────────────────────────────────────────────────────────

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

    // ── Footer 動態元件：固定預留簽名空間，僅最後一頁渲染簽名內容 ────────────
    //
    // 為什麼用 Dynamic Component：
    //   QuestPDF 的 Page 排版會先扣掉 Footer 高度才決定 Content 區可用高度。
    //   把簽名區固定預留在 Footer 內，就能讓分頁演算法把簽名空間算進去；
    //   再用 PageNumber == TotalPages 判斷是否為最後一頁，只在最後一頁渲染。
    private sealed class LastPageSignatureFooter
        : IDynamicComponent<LastPageSignatureFooter.SignatureState>
    {
        // 預留高度：聲明文字(~16) + 簽名 Row(90) + 緩衝 = 約 114pt
        private const float SignatureBlockHeight = 114f;
        private const float StripeHeight = 12f;
        private const string ColorBorder = "#d3d3d3";
        private const float FontBody = 9f;

        public readonly record struct SignatureState(
            string CustomerName,
            byte[] StampImage,
            byte[] ColorStripe);

        public SignatureState State { get; set; }

        public DynamicComponentComposeResult Compose(DynamicContext context)
        {
            var isLastPage = context.PageNumber == context.TotalPages;

            var content = context.CreateElement(element =>
            {
                element.Column(col =>
                {
                    // 簽名區（固定高度；僅最後一頁渲染內容，否則留白佔位）
                    col.Item().Height(SignatureBlockHeight).Element(c =>
                    {
                        if (!isLastPage) return;

                        c.Column(sigCol =>
                        {
                            // 確認聲明
                            sigCol.Item().PaddingBottom(4).Text(
                                "我已閱讀並同意上述條款(請親簽並蓋公司章)，這份資料將被認為一份法律合約。")
                                .FontSize(FontBody)
                                .FontFamily("Noto Sans TC");

                            // 簽名 Row（90pt）
                            sigCol.Item().Height(90).Row(row =>
                            {
                                // 左：客戶確認回簽
                                row.RelativeItem().PaddingRight(10)
                                    .BorderBottom(0.5f).BorderColor(ColorBorder)
                                    .Column(s =>
                                    {
                                        s.Item().Shrink().Text(State.CustomerName).FontSize(FontBody);
                                        s.Item().Shrink().Text("客戶確認回簽").FontSize(FontBody);
                                    });

                                // 右：威庭科技用印
                                row.RelativeItem().PaddingLeft(10)
                                    .BorderBottom(0.5f).BorderColor(ColorBorder)
                                    .Column(s =>
                                    {
                                        s.Item().AlignRight()
                                            .Text("威庭科技有限公司 Weypro Technology Ltd.")
                                            .FontSize(FontBody);
                                        s.Item().AlignRight()
                                            .Text("徐偉禎 Angela Hsu")
                                            .FontSize(FontBody);
                                        if (State.StampImage.Length > 0)
                                        {
                                            s.Item().AlignCenter().PaddingTop(2)
                                                .Image(State.StampImage).FitArea();
                                        }
                                    });
                            });
                        });
                    });

                    // 彩色裝飾條（每頁都顯示）
                    col.Item().Height(StripeHeight).Image(State.ColorStripe).FitWidth();
                });
            });

            return new DynamicComponentComposeResult
            {
                Content = content,
                HasMoreContent = false
            };
        }
    }
}
