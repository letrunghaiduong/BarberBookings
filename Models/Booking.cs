namespace BarberBooking.Models;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Cancelled
}

public class Booking
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime AppointmentTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>Token bí mật dùng để tạo link huỷ lịch trong email (không cần đăng nhập)</summary>
    public string CancelToken { get; set; } = Guid.NewGuid().ToString();

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}
