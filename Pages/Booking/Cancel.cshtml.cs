using BarberBooking.Data;
using BarberBooking.Models;
using BarberBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BarberBooking.Pages.Booking;

public class CancelModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;

    public CancelModel(AppDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public bool Success { get; set; }
    public bool AlreadyCancelled { get; set; }

    public async Task OnGetAsync(string token)
    {
        var booking = await _db.Bookings
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .FirstOrDefaultAsync(b => b.CancelToken == token);

        if (booking == null) return;

        if (booking.Status == BookingStatus.Cancelled)
        {
            AlreadyCancelled = true;
            return;
        }

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();

        await _email.SendCancellationConfirmationAsync(booking);
        Success = true;
    }
}
