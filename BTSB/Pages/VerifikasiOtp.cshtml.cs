using BTSB.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BTSB.Pages
{
    public class VerifikasiOtpModel : PageModel
    {
        private readonly AppDbContext _context;

        public VerifikasiOtpModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string KodeOtp { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            if (!HttpContext.Session.GetInt32("OtpPendingUserId").HasValue)
                return RedirectToPage("/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var pendingUserId = HttpContext.Session.GetInt32("OtpPendingUserId");
            var savedHash = HttpContext.Session.GetString("OtpHash");
            var expiryText = HttpContext.Session.GetString("OtpExpiresAt");

            if (!pendingUserId.HasValue || string.IsNullOrEmpty(savedHash) ||
                string.IsNullOrEmpty(expiryText))
                return RedirectToPage("/Login");

            if (!DateTimeOffset.TryParse(expiryText, out var expiresAt) ||
                DateTimeOffset.UtcNow > expiresAt)
            {
                ClearOtpSession();
                TempData["OtpError"] = "Kode OTP sudah kedaluwarsa. Silakan login kembali.";
                return RedirectToPage("/Login");
            }

            var attempts = HttpContext.Session.GetInt32("OtpAttempts") ?? 0;
            if (attempts >= 5)
            {
                ClearOtpSession();
                TempData["OtpError"] = "Batas percobaan OTP tercapai. Silakan login kembali.";
                return RedirectToPage("/Login");
            }

            var input = (KodeOtp ?? "").Trim();
            var validFormat = input.Length == 6 && input.All(c => c >= '0' && c <= '9');
            var validCode = false;

            if (validFormat)
            {
                var expectedBytes = Convert.FromBase64String(savedHash);
                var actualBytes = Convert.FromBase64String(HashOtp(input));
                validCode = CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
            }

            if (!validCode)
            {
                attempts++;
                HttpContext.Session.SetInt32("OtpAttempts", attempts);
                ModelState.AddModelError("", $"Kode OTP tidak sesuai. Sisa percobaan: {5 - attempts}.");
                    return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == pendingUserId.Value);
            if (user == null || !user.Status)
            {
                ClearOtpSession();
                TempData["OtpError"] = "Akun tidak tersedia atau sudah dinonaktifkan.";
                return RedirectToPage("/Login");
            }

            ClearOtpSession();

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
            HttpContext.Session.SetString("Nama", user.Nama ?? string.Empty);
            HttpContext.Session.SetString("Role", (user.Role ?? string.Empty).Trim());

            // Pastikan perubahan Session tersimpan sebelum redirect ke Dashboard.
            await HttpContext.Session.CommitAsync();

            // Diagnostik sementara untuk memastikan Session Petugas/Admin tersimpan.
            Console.WriteLine();
            Console.WriteLine("========== SESSION LOGIN BTSB ==========");
            Console.WriteLine($"UserId   : {HttpContext.Session.GetInt32("UserId")}");
            Console.WriteLine($"Username : {HttpContext.Session.GetString("Username")}");
            Console.WriteLine($"Nama     : {HttpContext.Session.GetString("Nama")}");
            Console.WriteLine($"Role     : {HttpContext.Session.GetString("Role")}");
            Console.WriteLine($"Session  : {HttpContext.Session.Id}");
            Console.WriteLine("========================================");

            return RedirectToPage("/Dashboard");
        }

        public async Task<IActionResult> OnPostKirimUlangAsync()
        {
            var pendingUserId = HttpContext.Session.GetInt32("OtpPendingUserId");
            if (!pendingUserId.HasValue)
                return RedirectToPage("/Login");

            var lastSentText = HttpContext.Session.GetString("OtpLastSentAt");
            if (DateTimeOffset.TryParse(lastSentText, out var lastSent) &&
                DateTimeOffset.UtcNow < lastSent.AddSeconds(60))
            {
                ModelState.AddModelError("", "Tunggu 60 detik sebelum meminta kode OTP baru.");
                    return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == pendingUserId.Value);
            if (user == null || !user.Status)
            {
                ClearOtpSession();
                return RedirectToPage("/Login");
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var now = DateTimeOffset.UtcNow;

            HttpContext.Session.SetString("OtpHash", HashOtp(otp));
            HttpContext.Session.SetString("OtpExpiresAt", now.AddMinutes(5).ToString("O"));
            HttpContext.Session.SetString("OtpLastSentAt", now.ToString("O"));
            HttpContext.Session.SetInt32("OtpAttempts", 0);

            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine("           KIRIM ULANG OTP BTSB (LOKAL)");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Waktu       : {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            Console.WriteLine($"Username    : {user.Username}");
            Console.WriteLine($"KODE OTP    : {otp}");
            Console.WriteLine("Berlaku     : 5 menit");
            Console.WriteLine("==================================================");
            Console.WriteLine();

            TempData["OtpInfo"] = "Kode OTP baru ditampilkan pada CMD/terminal aplikasi.";
            return RedirectToPage();
        }

        private void ClearOtpSession()
        {
            HttpContext.Session.Remove("OtpPendingUserId");
            HttpContext.Session.Remove("OtpHash");
            HttpContext.Session.Remove("OtpExpiresAt");
            HttpContext.Session.Remove("OtpAttempts");
            HttpContext.Session.Remove("OtpLastSentAt");
            HttpContext.Session.Remove("OtpMaskedEmail");
        }

        private static string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(otp)));
        }
    }
}
