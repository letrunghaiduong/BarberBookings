namespace BarberBooking.Models;

public class BookingService
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public decimal PriceSnapshot { get; set; }
}
