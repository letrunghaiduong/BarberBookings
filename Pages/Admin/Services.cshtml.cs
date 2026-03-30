using BarberBooking.Data;
using BarberBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BarberBooking.Pages.Admin;

public class ServicesModel : PageModel
{
    private readonly AppDbContext _db;
    public ServicesModel(AppDbContext db) => _db = db;

    public List<Service> Services { get; set; } = new();

    public async Task OnGetAsync()
    {
        Services = await _db.Services.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(string name, string description, decimal price, int durationMinutes, string? imageUrl)
    {
        _db.Services.Add(new Service
        {
            Name = name, Description = description,
            Price = price, DurationMinutes = durationMinutes, ImageUrl = imageUrl
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm dịch vụ thành công!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string name, string description, decimal price, int durationMinutes, string? imageUrl)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc == null) return NotFound();
        svc.Name = name; svc.Description = description;
        svc.Price = price; svc.DurationMinutes = durationMinutes; svc.ImageUrl = imageUrl;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật dịch vụ thành công!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc != null) { _db.Services.Remove(svc); await _db.SaveChangesAsync(); }
        TempData["Success"] = "Đã xoá dịch vụ.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleApprovalAsync(int id)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc != null) { svc.IsApproved = !svc.IsApproved; await _db.SaveChangesAsync(); }
        TempData["Success"] = svc?.IsApproved == true ? "Đã duyệt dịch vụ." : "Đã huỷ duyệt dịch vụ.";
        return RedirectToPage();
    }
}
