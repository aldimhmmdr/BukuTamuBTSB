using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BTSB.Pages
{
    public class PengaturanModel : AuthPageModel
    {
        private readonly AppDbContext _context;

        public PengaturanModel(AppDbContext context)
        {
            _context = context;
        }

        public User? CurrentUser { get; set; }

        [BindProperty]
        public string Nama { get; set; } = string.Empty;

        [BindProperty]
        public string PasswordSaatIni { get; set; } = string.Empty;

        [BindProperty]
        public string PasswordBaru { get; set; } = string.Empty;

        [BindProperty]
        public string KonfirmasiPassword { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var accessDenied = RequireAdmin();
            if (accessDenied != null) return accessDenied;

            var userId = HttpContext.Session.GetInt32("UserId");

            CurrentUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId!.Value);

            if (CurrentUser == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Login");
            }

            Nama = CurrentUser.Nama;
            return Page();
        }

        public async Task<IActionResult> OnPostSimpanProfilAsync()
        {
            var accessDenied = RequireAdmin();
            if (accessDenied != null) return accessDenied;

            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.IsNullOrWhiteSpace(Nama))
            {
                TempData["Error"] = "Nama wajib diisi.";
                return RedirectToPage();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId!.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Login");
            }

            user.Nama = Nama.Trim();
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("Nama", user.Nama);

            TempData["Success"] = "Profil berhasil diperbarui.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUbahPasswordAsync()
        {
            var accessDenied = RequireAdmin();
            if (accessDenied != null) return accessDenied;

            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.IsNullOrWhiteSpace(PasswordSaatIni) ||
                string.IsNullOrWhiteSpace(PasswordBaru) ||
                string.IsNullOrWhiteSpace(KonfirmasiPassword))
            {
                TempData["PasswordError"] = "Semua kolom password wajib diisi.";
                return RedirectToPage();
            }

            if (PasswordBaru.Length < 6)
            {
                TempData["PasswordError"] = "Password baru minimal 6 karakter.";
                return RedirectToPage();
            }

            if (PasswordBaru != KonfirmasiPassword)
            {
                TempData["PasswordError"] = "Konfirmasi password tidak sesuai.";
                return RedirectToPage();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId!.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Login");
            }

            var passwordLamaHash = HashPassword(PasswordSaatIni);

            if (user.PasswordHash != passwordLamaHash)
            {
                TempData["PasswordError"] = "Password saat ini tidak benar.";
                return RedirectToPage();
            }

            user.PasswordHash = HashPassword(PasswordBaru);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password berhasil diubah.";
            return RedirectToPage();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
