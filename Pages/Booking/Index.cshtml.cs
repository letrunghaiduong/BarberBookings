using BarberBooking.Data;
using BarberBooking.Models;
using BarberBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BarberBooking.Pages.Booking;

public class BookingIndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;

    public BookingIndexModel(AppDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public List<Service> Services { get; set; } = new();

    [BindProperty]
    public BookingInput Input { get; set; } = new();

    public class BookingInput
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ tên")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày giờ")]
        [Display(Name = "Ngày & giờ hẹn")]
        public DateTime AppointmentTime { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một dịch vụ")]
        public List<int> SelectedServiceIds { get; set; } = new();
    }

    public async Task OnGetAsync()
    {
        await LoadServicesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadServicesAsync();

        if (!ModelState.IsValid)
            return Page();

        var services = await _db.Services
        .Where(s => Input.SelectedServiceIds.Contains(s.Id) && s.IsApproved)
        .ToListAsync();

        if (!services.Any())
        {
            TempData["Error"] = "Dịch vụ không hợp lệ.";
            return Page();
        }

        // 2. Tính tổng thời gian
        var totalMinutes = services.Sum(s => s.DurationMinutes);

        var startTime = Input.AppointmentTime;
        var endTime = startTime.AddMinutes(totalMinutes);

        // 3. Lấy booking hiện có (cùng ngày để tối ưu)
        var existingBookings = await _db.Bookings
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
            .Where(b => b.Status != BookingStatus.Cancelled
                     && b.AppointmentTime.Date == startTime.Date)
            .ToListAsync();

        // 4. Check overlap
        var conflict = existingBookings.Any(b =>
        {
            var existingStart = b.AppointmentTime;

            var duration = b.BookingServices.Sum(bs => bs.Service?.DurationMinutes ?? 0);

            var existingEnd = existingStart.AddMinutes(duration);

            return startTime < existingEnd && endTime > existingStart;
        });


        if (conflict)
        {
            TempData["Error"] = $"Khung giờ này đã được đặt. Vui lòng chọn giờ khác.";
            return Page();
        }

        

        var booking = new Models.Booking
        {
            CustomerName = Input.CustomerName,
            CustomerPhone = Input.CustomerPhone,
            CustomerEmail = Input.CustomerEmail,
            AppointmentTime = Input.AppointmentTime,
            Notes = Input.Notes,
            Status = BookingStatus.Pending,
            CancelToken = Guid.NewGuid().ToString(),
            BookingServices = services.Select(s => new Models.BookingService
            {
                ServiceId = s.Id,
                PriceSnapshot = s.Price
            }).ToList()
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        // Gửi email xác nhận
        var cancelUrl = Url.PageLink("/Booking/Cancel", values: new { token = booking.CancelToken })!;
        booking.BookingServices = booking.BookingServices
            .Select(bs => { bs.Service = services.First(s => s.Id == bs.ServiceId); return bs; })
            .ToList();
        await _email.SendBookingConfirmationAsync(booking, cancelUrl);

        TempData["Success"] = $"Đặt lịch thành công! Vui lòng kiểm tra email {Input.CustomerEmail} để xác nhận.";
        return RedirectToPage("/Booking/Index");
    }

    private async Task LoadServicesAsync()
    {
        Services = await _db.Services
            .Where(s => s.IsApproved)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}
