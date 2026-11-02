using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
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
            if (HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToPage("/Dashboard");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError("", "Username dan password wajib diisi.");
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Username.ToLower() == Username.Trim().ToLower());

            if (user == null)
            {
                ModelState.AddModelError("", "Username atau password salah.");
                return Page();
            }

            if (!user.Status)
            {
                ModelState.AddModelError("", "Akun Anda sedang dinonaktifkan.");
                return Page();
            }

            if (user.PasswordHash != HashPassword(Password))
            {
                ModelState.AddModelError("", "Username atau password salah.");
                return Page();
            }

            // Hapus status login/OTP sebelumnya sebelum membuat OTP baru.
            ClearOtpSession();
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("Nama");
            HttpContext.Session.Remove("Role");

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var now = DateTimeOffset.UtcNow;

            HttpContext.Session.SetInt32("OtpPendingUserId", user.Id);
            HttpContext.Session.SetString("OtpHash", HashOtp(otp));
            HttpContext.Session.SetString("OtpExpiresAt", now.AddMinutes(5).ToString("O"));
            HttpContext.Session.SetInt32("OtpAttempts", 0);
            HttpContext.Session.SetString("OtpLastSentAt", now.ToString("O"));

            // MODE PENGEMBANGAN LOKAL:
            // OTP ditampilkan pada terminal/CMD tempat aplikasi dijalankan.
            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine("             VERIFIKASI OTP BTSB");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Waktu       : {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            Console.WriteLine($"Nama        : {user.Nama}");
            Console.WriteLine($"Username    : {user.Username}");
            Console.WriteLine($"Email akun  : {user.Email}");
            Console.WriteLine($"KODE OTP    : {otp}");
            Console.WriteLine("Berlaku     : 5 menit");
            Console.WriteLine("==================================================");
            Console.WriteLine();

            return RedirectToPage("/VerifikasiOtp");
        }

        private void ClearOtpSession()
        {
            HttpContext.Session.Remove("OtpPendingUserId");
            HttpContext.Session.Remove("OtpHash");
            HttpContext.Session.Remove("OtpExpiresAt");
            HttpContext.Session.Remove("OtpAttempts");
            HttpContext.Session.Remove("OtpLastSentAt");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(otp)));
        }
    }
}
