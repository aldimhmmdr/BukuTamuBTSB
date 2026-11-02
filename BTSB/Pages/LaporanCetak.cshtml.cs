using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BTSB.Pages
{
    public class LaporanCetakModel : AuthPageModel
    {
        private readonly AppDbContext _context;

        public LaporanCetakModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? TanggalMulai { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? TanggalSelesai { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? KeperluanFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? InstansiFilter { get; set; }

        public List<Kunjungan> DataLaporan { get; set; } = new();

        public int TotalKunjungan => DataLaporan.Count;

        public string Periode
        {
            get
            {
                if (TanggalMulai.HasValue && TanggalSelesai.HasValue)
                    return $"{TanggalMulai.Value:dd-MM-yyyy} s/d {TanggalSelesai.Value:dd-MM-yyyy}";

                if (TanggalMulai.HasValue)
                    return $"Mulai {TanggalMulai.Value:dd-MM-yyyy}";

                if (TanggalSelesai.HasValue)
                    return $"Sampai {TanggalSelesai.Value:dd-MM-yyyy}";

                return "Seluruh Data";
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            var query = _context.Kunjungan
                .AsNoTracking()
                .Where(x => x.Status == "Selesai")
                .AsQueryable();

            if (TanggalMulai.HasValue)
            {
                var tanggalAwal = TanggalMulai.Value.Date;

                query = query.Where(x =>
                    x.WaktuDatang >= tanggalAwal);
            }

            if (TanggalSelesai.HasValue)
            {
                var tanggalAkhir = TanggalSelesai.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.WaktuDatang < tanggalAkhir);
            }

            if (!string.IsNullOrWhiteSpace(KeperluanFilter) &&
                KeperluanFilter != "Semua")
            {
                query = query.Where(x =>
                    x.Keperluan == KeperluanFilter);
            }

            if (!string.IsNullOrWhiteSpace(InstansiFilter) &&
                InstansiFilter != "Semua")
            {
                query = query.Where(x =>
                    x.Instansi == InstansiFilter);
            }

            DataLaporan = await query
                .OrderByDescending(x => x.WaktuDatang)
                .ToListAsync();

            return Page();
        }
    }
}
