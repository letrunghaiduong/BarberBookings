using BarberBooking.Models;
using MailKit.Net.Smtp;
using MimeKit;

namespace BarberBooking.Services;

public interface IEmailService
{
    Task SendBookingConfirmationAsync(Booking booking, string cancelUrl);
    Task SendCancellationConfirmationAsync(Booking booking);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendBookingConfirmationAsync(Booking booking, string cancelUrl)
    {
        var serviceNames = string.Join(", ", booking.BookingServices.Select(bs => bs.Service.Name));
        var totalPrice = booking.BookingServices.Sum(bs => bs.PriceSnapshot);

        var body = $@"
<h2>Xác nhận đặt lịch thành công! ✂️</h2>
<p>Xin chào <strong>{booking.CustomerName}</strong>,</p>
<p>Lịch hẹn của bạn đã được đặt thành công với thông tin sau:</p>
<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse'>
  <tr><td><strong>Dịch vụ</strong></td><td>{serviceNames}</td></tr>
  <tr><td><strong>Thời gian</strong></td><td>{booking.AppointmentTime:dd/MM/yyyy HH:mm}</td></tr>
  <tr><td><strong>Tổng tiền</strong></td><td>{totalPrice:N0} VNĐ</td></tr>
  <tr><td><strong>Trạng thái</strong></td><td>Chờ xác nhận</td></tr>
</table>
<br/>
<p>Nếu bạn muốn <strong>huỷ lịch</strong>, nhấn vào link bên dưới:</p>
<p><a href='{cancelUrl}' style='color:red'>👉 Huỷ lịch hẹn này</a></p>
<p>Trân trọng,<br/>Barber Shop</p>";

        await SendEmailAsync(booking.CustomerEmail, "Xác nhận đặt lịch cắt tóc", body);
    }

    public async Task SendCancellationConfirmationAsync(Booking booking)
    {
        var body = $@"
<h2>Lịch hẹn đã được huỷ</h2>
<p>Xin chào <strong>{booking.CustomerName}</strong>,</p>
<p>Lịch hẹn ngày <strong>{booking.AppointmentTime:dd/MM/yyyy HH:mm}</strong> của bạn đã được huỷ thành công.</p>
<p>Nếu bạn muốn đặt lại, vui lòng truy cập website của chúng tôi.</p>
<p>Trân trọng,<br/>Barber Shop</p>";

        await SendEmailAsync(booking.CustomerEmail, "Lịch hẹn đã được huỷ", body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var smtp = _config.GetSection("Smtp");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtp["SenderName"], smtp["SenderEmail"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp["Host"], int.Parse(smtp["Port"]!), MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email tới {Email}", toEmail);
            // Không throw để không làm fail luồng chính
        }
    }
}
