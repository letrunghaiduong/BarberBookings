using BarberBooking.Models;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BarberBooking.Services;

public interface IExportService
{
    byte[] ExportToExcel(IEnumerable<Booking> bookings);
    byte[] ExportToCsv(IEnumerable<Booking> bookings);
    byte[] ExportToPdf(IEnumerable<Booking> bookings);
}

public class ExportService : IExportService
{
    public byte[] ExportToExcel(IEnumerable<Booking> bookings)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Danh sách đặt lịch");

        // Header
        string[] headers = { "ID", "Khách hàng", "Điện thoại", "Email", "Dịch vụ", "Thời gian", "Tổng tiền", "Trạng thái" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var b in bookings)
        {
            var services = string.Join(", ", b.BookingServices.Select(bs => bs.Service?.Name ?? ""));
            var total = b.BookingServices.Sum(bs => bs.PriceSnapshot);
            ws.Cell(row, 1).Value = b.Id;
            ws.Cell(row, 2).Value = b.CustomerName;
            ws.Cell(row, 3).Value = b.CustomerPhone;
            ws.Cell(row, 4).Value = b.CustomerEmail;
            ws.Cell(row, 5).Value = services;
            ws.Cell(row, 6).Value = b.AppointmentTime.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 7).Value = (double)total;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 8).Value = b.Status.ToString();
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportToCsv(IEnumerable<Booking> bookings)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ID,Khách hàng,Điện thoại,Email,Dịch vụ,Thời gian,Tổng tiền,Trạng thái");
        foreach (var b in bookings)
        {
            var services = string.Join(" | ", b.BookingServices.Select(bs => bs.Service?.Name ?? ""));
            var total = b.BookingServices.Sum(bs => bs.PriceSnapshot);
            sb.AppendLine($"{b.Id},\"{b.CustomerName}\",{b.CustomerPhone},{b.CustomerEmail},\"{services}\",{b.AppointmentTime:dd/MM/yyyy HH:mm},{total},{b.Status}");
        }
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportToPdf(IEnumerable<Booking> bookings)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var list = bookings.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Danh sách đặt lịch cắt tóc")
                    .FontSize(16).Bold().AlignCenter();

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(30);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                    });

                    // Header row - QuestPDF v2024 syntax
                    string[] cols = { "ID", "Khách hàng", "Điện thoại", "Email", "Dịch vụ", "Thời gian", "Tổng tiền", "Trạng thái" };
                    table.Header(header =>
                    {
                        foreach (var col in cols)
                            header.Cell().Background("#E0E0E0").Padding(4).Text(col).Bold();
                    });

                    // Data rows
                    foreach (var b in list)
                    {
                        var services = string.Join(", ", b.BookingServices.Select(bs => bs.Service?.Name ?? ""));
                        var total = b.BookingServices.Sum(bs => bs.PriceSnapshot);
                        table.Cell().Padding(4).Text(b.Id.ToString());
                        table.Cell().Padding(4).Text(b.CustomerName);
                        table.Cell().Padding(4).Text(b.CustomerPhone);
                        table.Cell().Padding(4).Text(b.CustomerEmail);
                        table.Cell().Padding(4).Text(services);
                        table.Cell().Padding(4).Text(b.AppointmentTime.ToString("dd/MM/yyyy HH:mm"));
                        table.Cell().Padding(4).Text($"{total:N0}đ");
                        table.Cell().Padding(4).Text(b.Status.ToString());
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
