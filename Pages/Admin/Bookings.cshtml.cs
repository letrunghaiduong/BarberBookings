using BarberBooking.Data;
using BarberBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BookingEntity = BarberBooking.Models.Booking;

namespace BarberBooking.Pages.Admin;

public class BookingsModel : PageModel
{
    private readonly AppDbContext _db;
    public BookingsModel(AppDbContext db) => _db = db;

    public List<BookingEntity> Bookings { get; set; } = new();
    public List<BookingEntity> CalendarBookings { get; set; } = new();

    // ✅ Thêm [BindProperty] để nhận giá trị từ cả GET lẫn POST
    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string From { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string To { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = string.Empty;

    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    private const int PageSize = 10;

    public async Task OnGetAsync()
    {
        if (Page < 1) Page = 1;

        var query = _db.Bookings
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .AsQueryable();

        if (DateTime.TryParse(From, out var fromDate))
            query = query.Where(b => b.AppointmentTime >= fromDate);
        if (DateTime.TryParse(To, out var toDate))
            query = query.Where(b => b.AppointmentTime <= toDate.AddDays(1));
        if (!string.IsNullOrEmpty(Status) && Enum.TryParse<BookingStatus>(Status, out var s))
            query = query.Where(b => b.Status == s);

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

        CalendarBookings = await query
            .OrderBy(b => b.AppointmentTime)
            .ToListAsync();

        Bookings = await query
            .OrderBy(b => b.Id)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetTableAsync(int pageIndex = 1, string from = "", string to = "", string status = "")
    {
        if (pageIndex < 1) pageIndex = 1;

        var query = _db.Bookings
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .AsQueryable();

        if (DateTime.TryParse(from, out var fromDate))
            query = query.Where(b => b.AppointmentTime >= fromDate);

        if (DateTime.TryParse(to, out var toDate))
            query = query.Where(b => b.AppointmentTime <= toDate.AddDays(1));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var s))
            query = query.Where(b => b.Status == s);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        var bookings = await query
            .OrderBy(b => b.Id)
            .Skip((pageIndex - 1) * PageSize)
            .Take(PageSize)
            .Select(b => new
            {
                b.Id,
                b.CustomerName,
                b.CustomerPhone,
                b.CustomerEmail,
                AppointmentTime = b.AppointmentTime.ToString("dd/MM/yyyy HH:mm"),
                Services = string.Join(", ", b.BookingServices.Select(bs => bs.Service != null ? bs.Service.Name : "")),
                Total = b.BookingServices.Sum(bs => bs.PriceSnapshot),
                Status = b.Status.ToString()
            })
            .ToListAsync();

        return new JsonResult(new { bookings, totalCount, totalPages, pageIndex });
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var b = await _db.Bookings.FindAsync(id);
        if (b != null) { b.Status = BookingStatus.Confirmed; await _db.SaveChangesAsync(); }

        // ✅ Page/From/To/Status đã được bind tự động từ hidden fields trong form
        return RedirectToPage(new { page = Page, from = From, to = To, status = Status });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var b = await _db.Bookings.FindAsync(id);
        if (b != null) { b.Status = BookingStatus.Cancelled; await _db.SaveChangesAsync(); }

        return RedirectToPage(new { page = Page, from = From, to = To, status = Status });
    }
}