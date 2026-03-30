using Microsoft.AspNetCore.Identity;

namespace BarberBooking.Models;

/// <summary>
/// Extend IdentityUser để thêm field FullName.
/// Identity sẽ tự tạo các bảng: AspNetUsers, AspNetRoles,
/// AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, v.v.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
