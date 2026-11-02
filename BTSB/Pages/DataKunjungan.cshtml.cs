using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BTSB.Pages
{
    public class DataKunjunganModel : AuthPageModel
    {
        private readonly AppDbContext _context;

        public DataKunjunganModel(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // DATA
        // =====================================================

        public List<Kunjungan> Data { get; set; } = new();

        // =====================================================
        // NOTIFIKASI
        // =====================================================

        public int TotalNotifikasi { get; set; }

        // =====================================================
        // FILTER
        // =====================================================

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? KeperluanFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DariTanggal { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SampaiTanggal { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? HighlightId { get; set; }


        // =====================================================
        // MENAMPILKAN DATA
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            // =================================================
            // CEK LOGIN
            // =================================================

            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }


            // =================================================
            // JUMLAH NOTIFIKASI
            // Notifikasi = tamu yang masih Menunggu
            // =================================================

            TotalNotifikasi = await _context.Kunjungan
                .AsNoTracking()
                .CountAsync(x => x.Status == "Menunggu");


            // =================================================
            // QUERY DATA KUNJUNGAN
            // =================================================

            var query = _context.Kunjungan
                .AsNoTracking()
                .AsQueryable();


            // =================================================
            // PENCARIAN
            // =================================================

            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(x =>
                    x.Nama.Contains(Search) ||
                    x.Instansi.Contains(Search) ||
                    x.Keperluan.Contains(Search) ||
                    x.Telepon.Contains(Search));
            }


            // =================================================
            // FILTER KEPERLUAN
            // =================================================

            if (!string.IsNullOrWhiteSpace(KeperluanFilter) &&
                KeperluanFilter != "Semua")
            {
                query = query.Where(x =>
                    x.Keperluan == KeperluanFilter);
            }


            // =================================================
            // FILTER STATUS
            // =================================================

            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
                StatusFilter != "Semua")
            {
                query = query.Where(x =>
                    x.Status == StatusFilter);
            }


            // =================================================
            // FILTER TANGGAL AWAL
            // =================================================

            if (DariTanggal.HasValue)
            {
                var tanggalMulai =
                    DariTanggal.Value.Date;

                query = query.Where(x =>
                    x.WaktuDatang >= tanggalMulai);
            }


            // =================================================
            // FILTER TANGGAL AKHIR
            // =================================================

            if (SampaiTanggal.HasValue)
            {
                var tanggalAkhir =
                    SampaiTanggal.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.WaktuDatang < tanggalAkhir);
            }


            // =================================================
            // HIGHLIGHT DATA
            // =================================================

            if (HighlightId.HasValue)
            {
                var highlightId =
                    HighlightId.Value;

                query = query
                    .OrderByDescending(
                        x => x.Id == highlightId)
                    .ThenByDescending(
                        x => x.WaktuDatang);
            }
            else
            {
                query = query
                    .OrderByDescending(
                        x => x.WaktuDatang);
            }


            // =================================================
            // AMBIL DATA
            // =================================================

            Data =
                await query.ToListAsync();

            return Page();
        }


        // =====================================================
        // MULAI MELAYANI TAMU
        // Menunggu → Sedang Dilayani
        // =====================================================

        public async Task<IActionResult> OnPostLayaniAsync(
            int id)
        {
            // =================================================
            // CEK LOGIN
            // =================================================

            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }


            var kunjungan =
                await _context.Kunjungan
                    .FirstOrDefaultAsync(
                        x => x.Id == id);


            if (kunjungan == null)
            {
                return NotFound();
            }


            // Hanya tamu yang masih menunggu
            // yang dapat mulai dilayani

            if (kunjungan.Status == "Menunggu")
            {
                kunjungan.Status =
                    "Sedang Dilayani";

                await _context.SaveChangesAsync();
            }


            return RedirectToPage();
        }


        // =====================================================
        // SELESAIKAN KUNJUNGAN
        // Sedang Dilayani → Selesai
        // =====================================================

        public async Task<IActionResult> OnPostSelesaiAsync(
            int id)
        {
            // =================================================
            // CEK LOGIN
            // =================================================

            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }


            var kunjungan =
                await _context.Kunjungan
                    .FirstOrDefaultAsync(
                        x => x.Id == id);


            if (kunjungan == null)
            {
                return NotFound();
            }


            // Hanya tamu yang sedang dilayani
            // yang dapat diselesaikan

            if (kunjungan.Status ==
                "Sedang Dilayani")
            {
                var waktuSelesai =
                    DateTime.Now;


                kunjungan.Status =
                    "Selesai";


                kunjungan.WaktuSelesai =
                    waktuSelesai;


                var durasiMenit =
                    (int)Math.Round(
                        (
                            waktuSelesai -
                            kunjungan.WaktuDatang
                        ).TotalMinutes
                    );


                // Minimal 1 menit

                kunjungan.Durasi =
                    Math.Max(
                        1,
                        durasiMenit);


                await _context.SaveChangesAsync();
            }


            return RedirectToPage();
        }
    }
}