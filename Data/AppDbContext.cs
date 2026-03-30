using BarberBooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BarberBooking.Data;

// Kế thừa IdentityDbContext<ApplicationUser> thay vì DbContext
// → tự sinh ra toàn bộ bảng Identity (AspNetUsers, AspNetRoles, ...)
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Quan trọng: gọi base để Identity tạo schema

        // Booking → BookingService (cascade delete)
        modelBuilder.Entity<BookingService>()
            .HasOne(bs => bs.Booking)
            .WithMany(b => b.BookingServices)
            .HasForeignKey(bs => bs.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Service → BookingService (no cascade, giữ lịch sử)
        modelBuilder.Entity<BookingService>()
            .HasOne(bs => bs.Service)
            .WithMany(s => s.BookingServices)
            .HasForeignKey(bs => bs.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lưu Status dạng string trong DB cho dễ đọc
        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasConversion<string>();
    }
}
