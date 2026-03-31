using BarberBooking.Data;
using BarberBooking.Models;
using BarberBooking.Services;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberBooking.Controllers;

[ApiController]
public class ApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IExportService _export;

    public ApiController(AppDbContext db, IExportService export)
    {
        _db = db;
        _export = export;
    }

    // GET /api/services
    [HttpGet("/api/services")]
    public async Task<IActionResult> GetServices()
    {
        var services = await _db.Services
            .Where(s => s.IsApproved)
            .Select(s => new { s.Id, s.Name, s.Description, s.ImageUrl, s.DurationMinutes, s.Price })
            .ToListAsync();
        return Ok(services);
    }

    // POST /api/bookings
    [HttpPost("/api/bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        if (dto.ServiceIds == null || !dto.ServiceIds.Any())
            return BadRequest(new { error = "Vui lòng chọn ít nhất một dịch vụ." });

        var services = await _db.Services
        .Where(s => dto.ServiceIds.Contains(s.Id) && s.IsApproved)
        .ToListAsync();

        if (!services.Any())
            return BadRequest(new { error = "Dịch vụ không hợp lệ." });

        var totalMinutes = services.Sum(s => s.DurationMinutes);

        var startTime = dto.AppointmentTime;
        var endTime = startTime.AddMinutes(totalMinutes);

        var existingBookings = await _db.Bookings
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        var conflict = existingBookings.Any(b =>
        {
            var existingStart = b.AppointmentTime;
            var duration = b.BookingServices.Sum(bs => bs.Service?.DurationMinutes ?? 0);

            var existingEnd = existingStart.AddMinutes(duration);

            return startTime < existingEnd && endTime > existingStart;
        });

        if (conflict) return BadRequest(new { error = "Khung giờ đã được đặt." });

        var booking = new Booking
        {
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            CustomerEmail = dto.CustomerEmail,
            AppointmentTime = dto.AppointmentTime,
            Notes = dto.Notes,
            CancelToken = Guid.NewGuid().ToString(),
            BookingServices = services.Select(s => new BookingService
            {
                ServiceId = s.Id, PriceSnapshot = s.Price
            }).ToList()
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return Ok(new { booking.Id, booking.CancelToken, message = "Đặt lịch thành công!" });
    }

    // GET /api/bookings/user?email=...
    [HttpGet("/api/bookings/user")]
    public async Task<IActionResult> GetUserBookings([FromQuery] string email)
    {
        var bookings = await _db.Bookings
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .Where(b => b.CustomerEmail == email)
            .OrderByDescending(b => b.AppointmentTime)
            .Select(b => new
            {
                b.Id, b.CustomerName, b.AppointmentTime, b.Status,
                Services = b.BookingServices.Select(bs => new { bs.Service!.Name, bs.PriceSnapshot }),
                Total = b.BookingServices.Sum(bs => bs.PriceSnapshot)
            })
            .ToListAsync();
        return Ok(bookings);
    }

    // DELETE /api/bookings/{id}
    [HttpDelete("/api/bookings/{id}")]
    public async Task<IActionResult> CancelBooking(int id, [FromQuery] string token)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null) return NotFound();
        if (booking.CancelToken != token) return Forbid();
        if (booking.Status == BookingStatus.Cancelled)
            return BadRequest(new { error = "Lịch đã được huỷ trước đó." });

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Huỷ lịch thành công." });
    }

    // GET /api/admin/bookings
    [HttpGet("/api/admin/bookings")]
    [Authorize]
    public async Task<IActionResult> GetAdminBookings([FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? status)
    {
        var query = _db.Bookings
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .AsQueryable();

        if (DateTime.TryParse(from, out var fromDate)) query = query.Where(b => b.AppointmentTime >= fromDate);
        if (DateTime.TryParse(to, out var toDate)) query = query.Where(b => b.AppointmentTime <= toDate.AddDays(1));
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var s)) query = query.Where(b => b.Status == s);

        var bookings = await query.OrderByDescending(b => b.AppointmentTime).ToListAsync();
        return Ok(bookings.Select(b => new
        {
            b.Id, b.CustomerName, b.CustomerPhone, b.CustomerEmail, b.AppointmentTime, b.Status,
            Services = b.BookingServices.Select(bs => new { bs.Service!.Name, bs.PriceSnapshot }),
            Total = b.BookingServices.Sum(bs => bs.PriceSnapshot)
        }));
    }

    // GET /api/admin/bookings/export?format=excel|csv|pdf
    [HttpGet("/api/admin/bookings/export")]
    [Authorize]
    public async Task<IActionResult> ExportBookings([FromQuery] string format = "excel", [FromQuery] string? from = null, [FromQuery] string? to = null, [FromQuery] string? status = null)
    {
        var query = _db.Bookings.Include(b => b.BookingServices).ThenInclude(bs => bs.Service).AsQueryable();
        if (DateTime.TryParse(from, out var fromDate)) query = query.Where(b => b.AppointmentTime >= fromDate);
        if (DateTime.TryParse(to, out var toDate)) query = query.Where(b => b.AppointmentTime <= toDate.AddDays(1));
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var s)) query = query.Where(b => b.Status == s);
        var bookings = await query.OrderBy(b => b.Id).ToListAsync();

        return format.ToLower() switch
        {
            "csv" => File(_export.ExportToCsv(bookings), "text/csv", $"bookings_{DateTime.Now:yyyyMMdd}.csv"),
            "pdf" => File(_export.ExportToPdf(bookings), "application/pdf", $"bookings_{DateTime.Now:yyyyMMdd}.pdf"),
            _ => File(_export.ExportToExcel(bookings), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"bookings_{DateTime.Now:yyyyMMdd}.xlsx")
        };
    }
}

public class CreateBookingDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime AppointmentTime { get; set; }
    public string? Notes { get; set; }
    public List<int> ServiceIds { get; set; } = new();
}
