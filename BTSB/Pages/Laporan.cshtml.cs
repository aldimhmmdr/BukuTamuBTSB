using BTSB.Data;
using BTSB.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BTSB.Pages
{
    public class LaporanModel : AuthPageModel
    {
        private readonly AppDbContext _context;

        public LaporanModel(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // FILTER
        // =====================================================

        [BindProperty(SupportsGet = true)]
        public DateTime? TanggalMulai { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? TanggalSelesai { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? KeperluanFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? InstansiFilter { get; set; }

        // =====================================================
        // ALIAS UNTUK EXPORT EXCEL
        // Digunakan oleh Laporan.cshtml
        // =====================================================

        public DateTime? DariTanggal
        {
            get => TanggalMulai;
            set => TanggalMulai = value;
        }

        public DateTime? SampaiTanggal
        {
            get => TanggalSelesai;
            set => TanggalSelesai = value;
        }

        // =====================================================
        // DAFTAR PILIHAN FILTER
        // =====================================================

        public List<string> DaftarInstansi { get; set; } = new();

        public List<string> DaftarKeperluan { get; set; } = new();

        // =====================================================
        // DATA LAPORAN
        // HANYA DATA DENGAN STATUS SELESAI
        // =====================================================

        public List<Kunjungan> DataLaporan { get; set; } = new();

        // =====================================================
        // STATISTIK
        // =====================================================

        public int TotalKunjungan { get; set; }

        public int TotalMenunggu { get; set; }

        public int TotalDilayani { get; set; }

        public int TotalSelesai { get; set; }

        // =====================================================
        // STATISTIK KEPERLUAN
        // =====================================================

        public int TotalTeknis { get; set; }

        public int TotalBimbingan { get; set; }

        public int TotalKonsultasi { get; set; }

        public int TotalLainnya { get; set; }

        // =====================================================
        // GRAFIK KEPERLUAN
        // =====================================================

        public List<GrafikKeperluanItem> GrafikKeperluan { get; set; } = new();

        // =====================================================
        // NOTIFIKASI
        // =====================================================

        public int TotalNotifikasi { get; set; }

        // =====================================================
        // ON GET
        // =====================================================

        public async Task<IActionResult> OnGetAsync(
            DateTime? tanggalMulai,
            DateTime? tanggalSelesai,
            string? keperluanFilter,
            string? instansiFilter)
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            // =================================================
            // SIMPAN FILTER
            // =================================================

            TanggalMulai = tanggalMulai;
            TanggalSelesai = tanggalSelesai;
            KeperluanFilter = keperluanFilter;
            InstansiFilter = instansiFilter;

            // =================================================
            // DAFTAR INSTANSI
            // =================================================

            DaftarInstansi = await _context.Kunjungan
                .AsNoTracking()
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Instansi))
                .Select(x => x.Instansi)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // =================================================
            // DAFTAR KEPERLUAN
            // =================================================

            DaftarKeperluan = await _context.Kunjungan
                .AsNoTracking()
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Keperluan))
                .Select(x => x.Keperluan)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // =================================================
            // QUERY LAPORAN
            // HANYA TAMU YANG SUDAH SELESAI
            // =================================================

            var query = _context.Kunjungan
                .AsNoTracking()
                .Where(x => x.Status == "Selesai")
                .AsQueryable();

            // =================================================
            // FILTER TANGGAL MULAI
            // =================================================

            if (TanggalMulai.HasValue)
            {
                var tanggalAwal = TanggalMulai.Value.Date;

                query = query.Where(x =>
                    x.WaktuDatang >= tanggalAwal);
            }

            // =================================================
            // FILTER TANGGAL SELESAI
            // =================================================

            if (TanggalSelesai.HasValue)
            {
                var tanggalAkhir =
                    TanggalSelesai.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.WaktuDatang < tanggalAkhir);
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
            // FILTER INSTANSI
            // =================================================

            if (!string.IsNullOrWhiteSpace(InstansiFilter) &&
                InstansiFilter != "Semua")
            {
                query = query.Where(x =>
                    x.Instansi == InstansiFilter);
            }

            // =================================================
            // AMBIL DATA LAPORAN
            // =================================================

            DataLaporan = await query
                .OrderByDescending(x => x.WaktuDatang)
                .ToListAsync();

            // =================================================
            // TOTAL
            // =================================================

            TotalKunjungan = DataLaporan.Count;

            // Karena laporan hanya berisi data Selesai
            TotalSelesai = DataLaporan.Count;

            TotalMenunggu = 0;
            TotalDilayani = 0;

            // =================================================
            // AKTIVITAS TEKNIS
            // =================================================

            TotalTeknis = DataLaporan.Count(x =>
                x.Keperluan == "Advis Teknis" ||
                x.Keperluan == "Pengujian Termal" ||
                x.Keperluan == "Pengujian Ketahanan Api" ||
                x.Keperluan == "Pengujian Akustik");

            // =================================================
            // BIMBINGAN TEKNIS
            // =================================================

            TotalBimbingan = DataLaporan.Count(x =>
                x.Keperluan == "Bimbingan Teknis");

            // =================================================
            // KONSULTASI
            // =================================================

            TotalKonsultasi = DataLaporan.Count(x =>
                x.Keperluan != null &&
                x.Keperluan.StartsWith("Konsultasi"));

            // =================================================
            // LAINNYA
            // =================================================

            TotalLainnya = DataLaporan.Count(x =>
                x.Keperluan != "Advis Teknis" &&
                x.Keperluan != "Pengujian Termal" &&
                x.Keperluan != "Pengujian Ketahanan Api" &&
                x.Keperluan != "Pengujian Akustik" &&
                x.Keperluan != "Bimbingan Teknis" &&
                (
                    x.Keperluan == null ||
                    !x.Keperluan.StartsWith("Konsultasi")
                ));

            // =================================================
            // DATA GRAFIK
            // =================================================

            GrafikKeperluan = new List<GrafikKeperluanItem>
            {
                new GrafikKeperluanItem
                {
                    Label = "Aktivitas Teknis",
                    Value = TotalTeknis
                },

                new GrafikKeperluanItem
                {
                    Label = "Bimbingan Teknis",
                    Value = TotalBimbingan
                },

                new GrafikKeperluanItem
                {
                    Label = "Konsultasi",
                    Value = TotalKonsultasi
                },

                new GrafikKeperluanItem
                {
                    Label = "Lainnya",
                    Value = TotalLainnya
                }
            };

            // =================================================
            // NOTIFIKASI
            // =================================================

            TotalNotifikasi = 0;

            return Page();
        }

        // =====================================================
        // EXPORT EXCEL
        // HANYA DATA DENGAN STATUS SELESAI
        // =====================================================

        public async Task<IActionResult> OnGetExportExcelAsync(
            DateTime? DariTanggal,
            DateTime? SampaiTanggal,
            string? KeperluanFilter,
            string? InstansiFilter)
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            // =================================================
            // QUERY DATA
            // =================================================

            var query = _context.Kunjungan
                .AsNoTracking()
                .Where(x => x.Status == "Selesai")
                .AsQueryable();

            // =================================================
            // FILTER TANGGAL MULAI
            // =================================================

            if (DariTanggal.HasValue)
            {
                var tanggalMulai = DariTanggal.Value.Date;

                query = query.Where(x =>
                    x.WaktuDatang >= tanggalMulai);
            }

            // =================================================
            // FILTER TANGGAL SELESAI
            // =================================================

            if (SampaiTanggal.HasValue)
            {
                var tanggalAkhir =
                    SampaiTanggal.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.WaktuDatang < tanggalAkhir);
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
            // FILTER INSTANSI
            // =================================================

            if (!string.IsNullOrWhiteSpace(InstansiFilter) &&
                InstansiFilter != "Semua")
            {
                query = query.Where(x =>
                    x.Instansi == InstansiFilter);
            }

            // =================================================
            // AMBIL DATA
            // =================================================

            var data = await query
                .OrderByDescending(x => x.WaktuDatang)
                .ToListAsync();

            // =================================================
            // BUAT WORKBOOK
            // =================================================

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Laporan Kunjungan");

            // =================================================
            // JUDUL
            // =================================================

            worksheet.Cell("A1").Value =
                "BALAI TEKNIK SAINS BANGUNAN";

            worksheet.Cell("A2").Value =
                "LAPORAN KUNJUNGAN TAMU";

            worksheet.Cell("A3").Value =
                "Buku Tamu Digital";

            worksheet.Range("A1:J1").Merge();
            worksheet.Range("A2:J2").Merge();
            worksheet.Range("A3:J3").Merge();

            worksheet.Cell("A1")
                .Style.Font.SetBold()
                .Font.SetFontSize(16);

            worksheet.Cell("A2")
                .Style.Font.SetBold()
                .Font.SetFontSize(14);

            worksheet.Cell("A3")
                .Style.Font.SetFontSize(10);

            worksheet.Range("A1:J3")
                .Style.Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            // =================================================
            // INFORMASI LAPORAN
            // =================================================

            worksheet.Cell("A5").Value = "Periode";

            worksheet.Cell("B5").Value =
                FormatPeriode(
                    DariTanggal,
                    SampaiTanggal);

            worksheet.Cell("A6").Value = "Status";

            worksheet.Cell("B6").Value = "Selesai";

            worksheet.Cell("A7").Value =
                "Tanggal Ekspor";

            worksheet.Cell("B7").Value =
                DateTime.Now.ToString(
                    "dd-MM-yyyy HH:mm");

            worksheet.Cell("A8").Value =
                "Total Kunjungan";

            worksheet.Cell("B8").Value =
                data.Count;

            worksheet.Range("A5:A8")
                .Style.Font.SetBold();

            // =================================================
            // HEADER TABEL
            // =================================================

            int headerRow = 10;

            worksheet.Cell(headerRow, 1)
                .Value = "No";

            worksheet.Cell(headerRow, 2)
                .Value = "Nama";

            worksheet.Cell(headerRow, 3)
                .Value = "Telepon";

            worksheet.Cell(headerRow, 4)
                .Value = "Instansi / Asal";

            worksheet.Cell(headerRow, 5)
                .Value = "Alamat";

            worksheet.Cell(headerRow, 6)
                .Value = "Keperluan";

            worksheet.Cell(headerRow, 7)
                .Value = "Jam Datang";

            worksheet.Cell(headerRow, 8)
                .Value = "Jam Selesai";

            worksheet.Cell(headerRow, 9)
                .Value = "Durasi";

            worksheet.Cell(headerRow, 10)
                .Value = "Status";

            // =================================================
            // STYLE HEADER
            // =================================================

            var headerRange =
                worksheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    10);

            headerRange.Style.Font.SetBold();

            headerRange.Style.Font.SetFontColor(
                XLColor.White);

            headerRange.Style.Fill.SetBackgroundColor(
                XLColor.FromHtml("#14558C"));

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            headerRange.Style.Border
            .SetOutsideBorder(XLBorderStyleValues.Thin);

            headerRange.Style.Border
            .SetInsideBorder(XLBorderStyleValues.Thin);

            // =================================================
            // ISI DATA
            // =================================================

            int row = headerRow + 1;
            int nomor = 1;

            foreach (var item in data)
            {
                worksheet.Cell(row, 1)
                    .Value = nomor;

                worksheet.Cell(row, 2)
                    .Value = item.Nama;

                worksheet.Cell(row, 3)
                    .Value = item.Telepon;

                worksheet.Cell(row, 4)
                    .Value = item.Instansi;

                worksheet.Cell(row, 5)
                    .Value = item.Alamat;

                worksheet.Cell(row, 6)
                    .Value = item.Keperluan;

                worksheet.Cell(row, 7)
                    .Value =
                        item.WaktuDatang.ToString(
                            "dd-MM-yyyy HH:mm"
                        );

                worksheet.Cell(row, 8)
                    .Value =
                        item.WaktuSelesai.HasValue
                            ? item.WaktuSelesai.Value
                                .ToString("dd-MM-yyyy HH:mm")
                            : "-";

                worksheet.Cell(row, 9)
                    .Value =
                    item.Durasi.HasValue
                        ? $"{item.Durasi.Value} menit"
                        : "-";

                worksheet.Cell(row, 10)
                    .Value = "Selesai";

                row++;
                nomor++;
            }

            // =================================================
            // BORDER TABEL
            // =================================================

            if (data.Count > 0)
            {
                var dataRange =
                    worksheet.Range(
                        headerRow,
                        1,
                        row - 1,
                        10);

                dataRange.Style.Border
                    .SetOutsideBorder(
                        XLBorderStyleValues.Thin);

                dataRange.Style.Border
                    .SetInsideBorder(
                        XLBorderStyleValues.Thin);

                dataRange.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;
            }

            // =================================================
            // FORMAT KOLOM
            // =================================================

            worksheet.Column(1).Width = 7;
            worksheet.Column(2).Width = 25;
            worksheet.Column(3).Width = 18;
            worksheet.Column(4).Width = 25;
            worksheet.Column(5).Width = 35;
            worksheet.Column(6).Width = 25;
            worksheet.Column(7).Width = 22;
            worksheet.Column(8).Width = 22;
            worksheet.Column(9).Width = 15;
            worksheet.Column(10).Width = 15;

            // =================================================
            // WRAP TEXT
            // =================================================

            worksheet.Range(
                headerRow,
                1,
                Math.Max(row - 1, headerRow),
                10)
                .Style.Alignment.WrapText = true;

            // =================================================
            // FREEZE HEADER
            // =================================================

            worksheet.SheetView.FreezeRows(
                headerRow);

            // =================================================
            // FILTER EXCEL
            // =================================================

            if (data.Count > 0)
            {
                worksheet.Range(
                    headerRow,
                    1,
                    row - 1,
                    10)
                    .SetAutoFilter();
            }

            // =================================================
            // SIMPAN KE MEMORY
            // =================================================

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            // =================================================
            // NAMA FILE
            // =================================================

            string namaFile =
                $"Laporan_Kunjungan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                namaFile);
        }

        // =====================================================
        // FORMAT PERIODE
        // =====================================================

        private string FormatPeriode(
            DateTime? mulai,
            DateTime? selesai)
        {
            if (mulai.HasValue && selesai.HasValue)
            {
                return
                    $"{mulai.Value:dd-MM-yyyy} s/d " +
                    $"{selesai.Value:dd-MM-yyyy}";
            }

            if (mulai.HasValue)
            {
                return
                    $"Mulai {mulai.Value:dd-MM-yyyy}";
            }

            if (selesai.HasValue)
            {
                return
                    $"Sampai {selesai.Value:dd-MM-yyyy}";
            }

            return "Seluruh Data";
        }

        // =====================================================
        // CLASS GRAFIK
        // =====================================================

        public class GrafikKeperluanItem
        {
            public string Label { get; set; } = string.Empty;

            public int Value { get; set; }
        }
    }
}