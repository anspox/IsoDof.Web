using ClosedXML.Excel;
using IsoDof.Web.Models.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IsoDof.Web.Services;

public interface IDofReportService
{
    byte[] BuildExcel(IEnumerable<Dof> dofs);
    byte[] BuildPdf(IEnumerable<Dof> dofs, string title);
}

public class DofReportService : IDofReportService
{
    private static readonly string[] Headers =
    {
        "No", "Başlık", "Tür", "Kaynak", "Durum", "Departman",
        "Açan Kişi", "Atanan Kişi", "Oluşturma", "Son Tarih", "Gecikmiş mi?", "Arşiv"
    };

    private static string[] RowValues(Dof d) => new[]
    {
        d.Id.ToString(),
        d.Title,
        d.Type.ToString(),
        d.Source.ToString(),
        d.Status.ToString(),
        d.Department?.Name ?? "-",
        d.CreatedByUser?.FullName ?? "-",
        d.AssignedToUser?.FullName ?? "-",
        d.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"),
        d.DueDate?.ToLocalTime().ToString("dd.MM.yyyy") ?? "-",
        d.IsOverdue ? "Evet" : "Hayır",
        d.IsArchived ? "Arşivli" : ""
    };

    public byte[] BuildExcel(IEnumerable<Dof> dofs)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("DÖF Kayıtları");

        for (var c = 0; c < Headers.Length; c++)
            ws.Cell(1, c + 1).Value = Headers[c];

        var headerRange = ws.Range(1, 1, 1, Headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var d in dofs)
        {
            var values = RowValues(d);
            for (var c = 0; c < values.Length; c++)
                ws.Cell(row, c + 1).Value = values[c];
            row++;
        }

        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] BuildPdf(IEnumerable<Dof> dofs, string title)
    {
        var list = dofs.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(14).Bold();
                    col.Item().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}  •  Toplam: {list.Count} kayıt")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(28);  // No
                        columns.RelativeColumn(3);   // Başlık
                        columns.RelativeColumn(1.4f); // Tür
                        columns.RelativeColumn(1.4f); // Kaynak
                        columns.RelativeColumn(1.4f); // Durum
                        columns.RelativeColumn(1.6f); // Departman
                        columns.RelativeColumn(1.6f); // Açan
                        columns.RelativeColumn(1.6f); // Atanan
                        columns.RelativeColumn(1.2f); // Oluşturma
                        columns.RelativeColumn(1.2f); // Son Tarih
                        columns.RelativeColumn(1f);   // Gecikmiş
                        columns.RelativeColumn(1f);   // Arşiv
                    });

                    table.Header(header =>
                    {
                        foreach (var h in Headers)
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(h).Bold();
                    });

                    foreach (var d in list)
                    {
                        foreach (var v in RowValues(d))
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(v);
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
