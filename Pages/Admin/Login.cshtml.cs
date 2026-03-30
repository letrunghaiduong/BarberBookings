using BarberBooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BarberBooking.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager)
        => _signInManager = signInManager;

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        // Nếu đã đăng nhập rồi thì redirect luôn
        if (_signInManager.IsSignedIn(User))
            return RedirectToPage("/Admin/Bookings");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.PasswordSignInAsync(
            userName: Email,
            password: Password,
            isPersistent: RememberMe,
            lockoutOnFailure: true   // Khoá tài khoản sau 5 lần sai
        );

        if (result.Succeeded)
            return RedirectToPage("/Admin/Bookings");

        if (result.IsLockedOut)
            ErrorMessage = "Tài khoản bị khoá tạm thời do đăng nhập sai nhiều lần. Thử lại sau 15 phút.";
        else
            ErrorMessage = "Email hoặc mật khẩu không đúng.";

        return Page();
    }
}
