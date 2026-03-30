using BarberBooking.Data;
using BarberBooking.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BarberBooking.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public List<Service> Services { get; set; } = new();

    public IndexModel(AppDbContext db) => _db = db;

    public async Task OnGetAsync()
    {
        Services = await _db.Services
            .Where(s => s.IsApproved)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}
