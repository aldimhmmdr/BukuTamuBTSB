using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BTSB.Pages
{
    public class TambahKunjunganHistorisModel : AuthPageModel
    {
        private readonly AppDbContext _context;

        public TambahKunjunganHistorisModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty] public string Nama { get; set; } = string.Empty;
        [BindProperty] public string Telepon { get; set; } = string.Empty;
        [BindProperty] public string Instansi { get; set; } = string.Empty;
        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string Alamat { get; set; } = string.Empty;
        [BindProperty] public string Keperluan { get; set; } = string.Empty;
        [BindProperty] public string DetailKeperluan { get; set; } = string.Empty;
        [BindProperty] public DateTime? WaktuDatang { get; set; }
        [BindProperty] public DateTime? WaktuSelesai { get; set; }

        public IActionResult OnGet()
        {
            var access = RequireAdmin();
            if (access != null) return access;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var access = RequireAdmin();
            if (access != null) return access;

            if (string.IsNullOrWhiteSpace(Nama) ||
                string.IsNullOrWhiteSpace(Instansi) ||
                string.IsNullOrWhiteSpace(Keperluan) ||
                !WaktuDatang.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Nama, instansi, keperluan, dan tanggal/jam datang wajib diisi.");
                return Page();
            }

            if (WaktuSelesai.HasValue && WaktuSelesai.Value < WaktuDatang.Value)
            {
                ModelState.AddModelError(nameof(WaktuSelesai), "Waktu selesai tidak boleh lebih awal dari waktu datang.");
                return Page();
            }

            int? durasi = null;
            if (WaktuSelesai.HasValue)
            {
                durasi = Math.Max(0, (int)Math.Round((WaktuSelesai.Value - WaktuDatang.Value).TotalMinutes));
            }

            var data = new Kunjungan
            {
                Nama = Nama.Trim(),
                Telepon = Telepon?.Trim() ?? string.Empty,
                Instansi = Instansi.Trim(),
                Email = Email?.Trim() ?? string.Empty,
                Alamat = Alamat?.Trim() ?? string.Empty,
                Keperluan = Keperluan.Trim(),
                DetailKeperluan = DetailKeperluan?.Trim() ?? string.Empty,
                WaktuDatang = WaktuDatang.Value,
                WaktuSelesai = WaktuSelesai,
                Durasi = durasi,
                Status = "Selesai",
                SumberData = "Historis Manual"
            };

            _context.Kunjungan.Add(data);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Data kunjungan historis berhasil ditambahkan.";
            return RedirectToPage("/DataKunjungan");
        }
    }
}
