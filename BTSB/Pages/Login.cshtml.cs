using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BTSB.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }


        [BindProperty]
        public string Username { get; set; } = string.Empty;


        [BindProperty]
        public string Password { get; set; } = string.Empty;


        public IActionResult OnGet()
        {
            // Jika sudah login,
            // jangan tampilkan halaman login lagi.

            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
            {
                return RedirectToPage("/Dashboard");
            }

            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            // ==========================================
            // VALIDASI INPUT
            // ==========================================

            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(
                    "",
                    "Username dan password wajib diisi.");

                return Page();
            }


            // ==========================================
            // HASH PASSWORD
            // ==========================================

            string passwordHash =
                HashPassword(Password);


            // ==========================================
            // CARI USER DI DATABASE
            // ==========================================

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Username.ToLower() ==
                    Username.Trim().ToLower());


            // ==========================================
            // USER TIDAK DITEMUKAN
            // ==========================================

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Username atau password salah.");

                return Page();
            }


            // ==========================================
            // CEK STATUS USER
            // ==========================================

            if (!user.Status)
            {
                ModelState.AddModelError(
                    "",
                    "Akun Anda sedang dinonaktifkan.");

                return Page();
            }


            // ==========================================
            // CEK PASSWORD
            // ==========================================

            if (user.PasswordHash != passwordHash)
            {
                ModelState.AddModelError(
                    "",
                    "Username atau password salah.");

                return Page();
            }


            // ==========================================
            // LOGIN BERHASIL
            // ==========================================

            HttpContext.Session.SetInt32(
                "UserId",
                user.Id);

            HttpContext.Session.SetString(
                "Username",
                user.Username);

            HttpContext.Session.SetString(
                "Nama",
                user.Nama);

            HttpContext.Session.SetString(
                "Role",
                user.Role);


            // ==========================================
            // REDIRECT
            // ==========================================

            return RedirectToPage("/Dashboard");
        }


        // ==========================================
        // HASH PASSWORD
        // ==========================================

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();

            var bytes =
                Encoding.UTF8.GetBytes(password);

            var hash =
                sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }
    }
}