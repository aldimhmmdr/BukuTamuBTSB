using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BTSB.Pages
{
    public abstract class AuthPageModel : PageModel
    {
        protected bool IsLoggedIn()
        {
            return HttpContext.Session
                .GetInt32("UserId")
                .HasValue;
        }

        protected IActionResult RequireLogin()
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            return Page();
        }

        protected string? CurrentRole()
        {
            return HttpContext.Session
                .GetString("Role");
        }

        protected string? CurrentUsername()
        {
            return HttpContext.Session
                .GetString("Username");
        }

        protected string? CurrentName()
        {
            return HttpContext.Session
                .GetString("Nama");
        }

        protected bool IsAdmin()
        {
            return string.Equals(
                CurrentRole(),
                "Admin",
                StringComparison.OrdinalIgnoreCase
            );
        }

        protected IActionResult? RequireAdmin()
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            if (!IsAdmin())
            {
                TempData["Error"] =
                    "Anda tidak memiliki hak akses untuk halaman tersebut.";

                return RedirectToPage("/Dashboard");
            }

            return null;
        }
    }
}
