using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;

namespace BTSB.Pages
{
    public class PenggunaModel : AuthPageModel
    {
        private readonly AppDbContext _context;

        public PenggunaModel(AppDbContext context)
        {
            _context = context;
        }

        public List<User> DaftarPengguna { get; set; } = new();

        [BindProperty]
        public UserInput Input { get; set; } = new();

        [BindProperty]
        public int EditId { get; set; }

        [BindProperty]
        public string PasswordBaru { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var accessDenied = RequireAdmin();
            if (accessDenied != null) return accessDenied;

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostTambahAsync()
        {
            var accessDenied = RequireAdmin();
            if (accessDenied != null) return accessDenied;

            if (string.IsNullOrWhiteSpace(Input.Nama) ||
                string.IsNullOrWhiteSpace(Input.Username) ||
                string.IsNullOrWhiteSpace(Input.Email) ||
                string.IsNullOrWhiteSpace(Input.Password))
            {
                TempData["Error"] = "Nama, username, email, dan password wajib diisi.";
                return RedirectToPage();
            }

            var username = Input.Username.Trim();
            var email = Input.Email.Trim().ToLowerInvariant();

            if (!EmailValid(email))
            {
                TempData["Error"] = "Format email tidak valid.";
                return RedirectToPage();
            }

            if (await _context.Users.AnyAsync(x => x.Email != null && x.Email.ToLower() == email))
            {
                TempData["Error"] = "Email sudah digunakan oleh pengguna lain.";
                return RedirectToPage();
            }

            var usernameSudahAda = await _context.Users
                .AnyAsync(x => x.Username.ToLower() == username.ToLower());

            if (usernameSudahAda)
            {
                TempData["Error"] = "Username sudah digunakan.";
                return RedirectToPage();
            }

            var user = new User
            {
                Nama = Input.Nama.Trim(),
                Username = username,
                Email = email,
                PasswordHash = HashPassword(Input.Password),
                Role = Input.Role,
                Status = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Pengguna berhasil ditambahkan.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var accessDenied = RequireAdmin();
            if (accessDenied != null) return accessDenied;

            var user = await _context.Users.FindAsync(EditId);

            if (user == null)
            {
                TempData["Error"] = "Data pengguna tidak ditemukan.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Input.Nama) ||
                string.IsNullOrWhiteSpace(Input.Username) ||
                string.IsNullOrWhiteSpace(Input.Email))
            {
                TempData["Error"] = "Nama, username, dan email wajib diisi.";
                return RedirectToPage();
            }

            var username = Input.Username.Trim();
            var email = Input.Email.Trim().ToLowerInvariant();

            if (!EmailValid(email))
            {
                TempData["Error"] = "Format email tidak valid.";
                return RedirectToPage();
            }

            if (await _context.Users.AnyAsync(x =>
                x.Id != EditId && x.Email != null && x.Email.ToLower() == email))
            {
                TempData["Error"] = "Email sudah digunakan oleh pengguna lain.";
                return RedirectToPage();
            }

            var usernameSudahAda = await _context.Users
                .AnyAsync(x =>
                    x.Id != EditId &&
                    x.Username.ToLower() == username.ToLower());

            if (usernameSudahAda)
            {
                TempData["Error"] = "Username sudah digunakan oleh pengguna lain.";
                return RedirectToPage();
            }

            user.Nama = Input.Nama.Trim();
            user.Username = username;
            user.Email = email;
            user.Role = Input.Role;

            if (!string.IsNullOrWhiteSpace(PasswordBaru))
            {
                user.PasswordHash = HashPassword(PasswordBaru);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data pengguna berhasil diperbarui.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var accessDenied = RequireAdmin();
            if (accessDenied != null) return accessDenied;

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                TempData["Error"] = "Data pengguna tidak ditemukan.";
                return RedirectToPage();
            }

            user.Status = !user.Status;
            await _context.SaveChangesAsync();

            TempData["Success"] = user.Status
                ? "Pengguna berhasil diaktifkan."
                : "Pengguna berhasil dinonaktifkan.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostHapusAsync(int id)
        {
            var accessDenied = RequireAdmin();
            if (accessDenied != null) return accessDenied;

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                TempData["Error"] = "Data pengguna tidak ditemukan.";
                return RedirectToPage();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Pengguna berhasil dihapus.";
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            DaftarPengguna = await _context.Users
                .OrderBy(x => x.Nama)
                .ToListAsync();
        }

        private static bool EmailValid(string email)
        {
            try
            {
                var parsed = new MailAddress(email);
                return parsed.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public class UserInput
        {
            public string Nama { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = "Petugas";
        }
    }
}
