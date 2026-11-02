using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BTSB.Pages
{
    public class NotifikasiModel : AuthPageModel
    {
        private readonly AppDbContext _context;

        public NotifikasiModel(AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // DATA NOTIFIKASI
        // =====================================================

        public List<NotificationViewModel> Notifications { get; set; }
            = new();


        // =====================================================
        // JUMLAH NOTIFIKASI
        // =====================================================

        public int TotalSemua { get; set; }

        public int TotalBelumDibaca { get; set; }

        public int TotalSudahDibaca { get; set; }


        // =====================================================
        // FILTER AKTIF
        // =====================================================

        public string Filter { get; set; } = "semua";


        // =====================================================
        // TAMPILKAN NOTIFIKASI
        // =====================================================

        public async Task<IActionResult> OnGetAsync(string? filter)
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            Filter = string.IsNullOrWhiteSpace(filter)
                ? "semua"
                : filter.ToLower();


            // =================================================
            // JUMLAH NOTIFIKASI
            // =================================================

            TotalSemua = await _context.VisitNotifications
                .AsNoTracking()
                .CountAsync();


            TotalBelumDibaca = await _context.VisitNotifications
                .AsNoTracking()
                .CountAsync(x => !x.Dibaca);


            TotalSudahDibaca = await _context.VisitNotifications
                .AsNoTracking()
                .CountAsync(x => x.Dibaca);


            // =================================================
            // QUERY NOTIFIKASI + DATA KUNJUNGAN
            // =================================================

            var query =
                from notification in _context.VisitNotifications
                join kunjungan in _context.Kunjungan
                    on notification.KunjunganId equals kunjungan.Id
                select new NotificationViewModel
                {
                    Id = notification.Id,

                    KunjunganId = notification.KunjunganId,

                    Judul = notification.Judul,

                    Pesan = notification.Pesan,

                    Dibaca = notification.Dibaca,

                    CreatedAt = notification.CreatedAt,

                    Nama = kunjungan.Nama,

                    Instansi = kunjungan.Instansi,

                    Keperluan = kunjungan.Keperluan,

                    WaktuDatang = kunjungan.WaktuDatang,

                    StatusKunjungan = kunjungan.Status
                };


            // =================================================
            // FILTER
            // =================================================

            if (Filter == "belum")
            {
                query = query.Where(x => !x.Dibaca);
            }
            else if (Filter == "sudah")
            {
                query = query.Where(x => x.Dibaca);
            }


            // =================================================
            // URUTKAN
            // =================================================

            Notifications = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Page();
        }


        // =====================================================
        // BUKA NOTIFIKASI
        // =====================================================

        public async Task<IActionResult> OnGetBukaAsync(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            var notification =
                await _context.VisitNotifications
                    .FirstOrDefaultAsync(x => x.Id == id);


            if (notification != null)
            {
                notification.Dibaca = true;

                await _context.SaveChangesAsync();

                return RedirectToPage(
                    "/DataKunjungan",
                    new
                    {
                        HighlightId = notification.KunjunganId
                    });
            }


            return RedirectToPage("/Notifikasi");
        }


        // =====================================================
        // MODEL UNTUK TAMPILAN
        // =====================================================

        public class NotificationViewModel
        {
            public int Id { get; set; }

            public int KunjunganId { get; set; }

            public string Judul { get; set; } = string.Empty;

            public string Pesan { get; set; } = string.Empty;

            public bool Dibaca { get; set; }

            public DateTime CreatedAt { get; set; }

            public string Nama { get; set; } = string.Empty;

            public string Instansi { get; set; } = string.Empty;

            public string Keperluan { get; set; } = string.Empty;

            public DateTime WaktuDatang { get; set; }

            public string StatusKunjungan { get; set; } = string.Empty;
        }
    }
}