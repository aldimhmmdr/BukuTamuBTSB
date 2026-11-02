using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTSB.Data;
using System.Globalization;

namespace BTSB.Pages
{
    public class DashboardModel : AuthPageModel
    {
        private readonly AppDbContext _context;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // STATISTIK
        // =====================================================

        public int TotalKunjungan { get; set; }

        public int Menunggu { get; set; }

        public int SedangDilayani { get; set; }

        public int Selesai { get; set; }


        // =====================================================
        // GRAFIK
        // =====================================================

        public List<GrafikItem> GrafikKunjungan { get; set; }
            = new List<GrafikItem>();


        public string PeriodeGrafik { get; set; } = "";


        // =====================================================
        // DONUT
        // =====================================================

        public string DonutStyle { get; set; } =
            "conic-gradient(#d8e0ea 0deg 360deg)";


        // =====================================================
        // ON GET
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            // =================================================
            // CEK LOGIN
            // =================================================
            //
            // Jika belum login,
            // jangan izinkan membuka Dashboard.
            //

            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }


            // =================================================
            // TANGGAL
            // =================================================

            DateTime hariIni =
                DateTime.Today;


            DateTime besok =
                hariIni.AddDays(1);


            // =================================================
            // DATA KUNJUNGAN HARI INI
            // =================================================

            var kunjunganHariIni =
                await _context.Kunjungan
                    .AsNoTracking()
                    .Where(x =>
                        x.WaktuDatang >= hariIni &&
                        x.WaktuDatang < besok)
                    .ToListAsync();


            // =================================================
            // TOTAL
            // =================================================

            TotalKunjungan =
                kunjunganHariIni.Count;


            // =================================================
            // MENUNGGU
            // =================================================

            Menunggu =
                kunjunganHariIni.Count(x =>
                    x.Status == "Menunggu");


            // =================================================
            // SEDANG DILAYANI
            // =================================================

            SedangDilayani =
                kunjunganHariIni.Count(x =>
                    x.Status == "Sedang Dilayani");


            // =================================================
            // SELESAI
            // =================================================

            Selesai =
                kunjunganHariIni.Count(x =>
                    x.Status == "Selesai");


            // =================================================
            // GRAFIK 7 HARI
            // =================================================

            for (int i = 6; i >= 0; i--)
            {
                DateTime tanggal =
                    hariIni.AddDays(-i);


                DateTime tanggalBerikutnya =
                    tanggal.AddDays(1);


                int jumlah =
                    await _context.Kunjungan
                        .AsNoTracking()
                        .CountAsync(x =>
                            x.WaktuDatang >= tanggal &&
                            x.WaktuDatang < tanggalBerikutnya);


                GrafikKunjungan.Add(
                    new GrafikItem
                    {
                        Tanggal =
                            tanggal.ToString(
                                "dd MMM",
                                new CultureInfo("id-ID")
                            ),

                        Jumlah = jumlah
                    }
                );
            }


            // =================================================
            // PERIODE
            // =================================================

            DateTime tanggalAwal =
                hariIni.AddDays(-6);


            PeriodeGrafik =
                tanggalAwal.ToString(
                    "MMMM yyyy",
                    new CultureInfo("id-ID")
                );


            // =================================================
            // TINGGI BAR
            // =================================================

            int nilaiMaksimum =
                GrafikKunjungan
                    .Select(x => x.Jumlah)
                    .DefaultIfEmpty(1)
                    .Max();


            foreach (var item in GrafikKunjungan)
            {
                if (item.Jumlah == 0)
                {
                    item.TinggiBar = 4;
                }
                else
                {
                    item.TinggiBar =
                        Math.Max(
                            15,
                            (item.Jumlah * 180) /
                            Math.Max(
                                1,
                                nilaiMaksimum
                            )
                        );
                }
            }


            // =================================================
            // DONUT CHART
            // =================================================

            BuatDonut();


            // =================================================
            // SELESAI
            // =================================================

            return Page();
        }


        // =====================================================
        // REALTIME NOTIFIKASI
        // Digunakan oleh halaman admin selain Dashboard.
        // =====================================================

        public async Task<IActionResult> OnGetNotificationCountAsync()
        {
            if (!IsLoggedIn())
            {
                return Unauthorized();
            }

            int totalNotifikasi =
                await _context.VisitNotifications
                    .AsNoTracking()
                    .CountAsync(x => !x.Dibaca);

            return new JsonResult(new
            {
                totalNotifikasi
            });
        }


        // =====================================================
        // REALTIME DASHBOARD
        // Dipanggil JavaScript setiap beberapa detik agar
        // statistik dan notifikasi berubah tanpa refresh.
        // =====================================================

        public async Task<IActionResult> OnGetRealtimeAsync()
        {
            if (!IsLoggedIn())
            {
                return Unauthorized();
            }

            DateTime hariIni = DateTime.Today;
            DateTime besok = hariIni.AddDays(1);

            var kunjunganHariIni =
                await _context.Kunjungan
                    .AsNoTracking()
                    .Where(x =>
                        x.WaktuDatang >= hariIni &&
                        x.WaktuDatang < besok)
                    .ToListAsync();

            int totalKunjungan = kunjunganHariIni.Count;

            int menunggu = kunjunganHariIni.Count(x =>
                x.Status == "Menunggu");

            int sedangDilayani = kunjunganHariIni.Count(x =>
                x.Status == "Sedang Dilayani");

            int selesai = kunjunganHariIni.Count(x =>
                x.Status == "Selesai");

            var grafik = new List<GrafikItem>();

            for (int i = 6; i >= 0; i--)
            {
                DateTime tanggal = hariIni.AddDays(-i);
                DateTime tanggalBerikutnya = tanggal.AddDays(1);

                int jumlah =
                    await _context.Kunjungan
                        .AsNoTracking()
                        .CountAsync(x =>
                            x.WaktuDatang >= tanggal &&
                            x.WaktuDatang < tanggalBerikutnya);

                grafik.Add(new GrafikItem
                {
                    Tanggal = tanggal.ToString(
                        "dd MMM",
                        new CultureInfo("id-ID")
                    ),
                    Jumlah = jumlah
                });
            }

            int nilaiMaksimum = grafik
                .Select(x => x.Jumlah)
                .DefaultIfEmpty(1)
                .Max();

            foreach (var item in grafik)
            {
                item.TinggiBar = item.Jumlah == 0
                    ? 4
                    : Math.Max(
                        15,
                        (item.Jumlah * 180) / Math.Max(1, nilaiMaksimum)
                    );
            }

            string donutStyle = BuatDonutStyle(
                menunggu,
                sedangDilayani,
                selesai
            );

            int totalNotifikasi =
                await _context.VisitNotifications
                    .AsNoTracking()
                    .CountAsync(x => !x.Dibaca);

            return new JsonResult(new
            {
                totalKunjungan,
                menunggu,
                sedangDilayani,
                selesai,
                totalNotifikasi,
                grafik,
                donutStyle
            });
        }


        // =====================================================
        // MEMBUAT STYLE DONUT UNTUK DATA REALTIME
        // =====================================================

        private static string BuatDonutStyle(
            int menunggu,
            int sedangDilayani,
            int selesai)
        {
            int total =
                menunggu +
                sedangDilayani +
                selesai;

            if (total == 0)
            {
                return "conic-gradient(#d8e0ea 0deg 360deg)";
            }

            double waiting =
                (double)menunggu / total * 360;

            double serving =
                (double)sedangDilayani / total * 360;

            double batasWaiting = waiting;
            double batasServing = waiting + serving;
            double batasSelesai = 360;

            return
                $"conic-gradient(" +
                $"#2384d8 0deg {batasWaiting}deg, " +
                $"#f39213 {batasWaiting}deg {batasServing}deg, " +
                $"#18a36d {batasServing}deg {batasSelesai}deg" +
                $")";
        }


        // =====================================================
        // MEMBUAT DONUT BERDASARKAN DATA
        // =====================================================

        private void BuatDonut()
        {
            int total =
                Menunggu +
                SedangDilayani +
                Selesai;


            if (total == 0)
            {
                DonutStyle =
                    "conic-gradient(#d8e0ea 0deg 360deg)";

                return;
            }


            double waiting =
                (double)Menunggu /
                total *
                360;


            double serving =
                (double)SedangDilayani /
                total *
                360;


            double selesai =
                (double)Selesai /
                total *
                360;


            double batasWaiting =
                waiting;


            double batasServing =
                waiting +
                serving;


            double batasSelesai =
                waiting +
                serving +
                selesai;


            DonutStyle =
                $"conic-gradient(" +
                $"#2384d8 0deg {batasWaiting}deg, " +
                $"#f39213 {batasWaiting}deg {batasServing}deg, " +
                $"#18a36d {batasServing}deg {batasSelesai}deg" +
                $")";
        }


        // =====================================================
        // CLASS DATA GRAFIK
        // =====================================================

        public class GrafikItem
        {
            public string Tanggal { get; set; } = "";

            public int Jumlah { get; set; }

            public int TinggiBar { get; set; }
        }
    }
}