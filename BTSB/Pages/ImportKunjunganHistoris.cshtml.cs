using BTSB.Data;
using BTSB.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BTSB.Pages
{
    public class ImportKunjunganHistorisModel : AuthPageModel
    {
        private readonly AppDbContext _context;
        private const int MaksimalBaris = 1000;

        public ImportKunjunganHistorisModel(AppDbContext context) { _context = context; }

        [BindProperty] public IFormFile? FileExcel { get; set; }
        public string? Hasil { get; set; }

        public IActionResult OnGet()
        {
            var access = RequireAdmin();
            if (access != null) return access;
            return Page();
        }

        public IActionResult OnGetTemplate()
        {
            var access = RequireAdmin();
            if (access != null) return access;

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("DataKunjungan");
            string[] headers = { "Nama", "Telepon", "Instansi", "Email", "Alamat", "Keperluan", "DetailKeperluan", "WaktuDatang", "WaktuSelesai", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
                sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }
            sheet.Cell(2, 1).Value = "Contoh Nama";
            sheet.Cell(2, 2).Value = "+62 81234567890";
            sheet.Cell(2, 3).Value = "Contoh Instansi";
            sheet.Cell(2, 6).Value = "Advis Teknis";
            sheet.Cell(2, 8).Value = "2026-10-15 09:00";
            sheet.Cell(2, 9).Value = "2026-10-15 10:00";
            sheet.Cell(2, 10).Value = "Selesai";
            sheet.Cell(2, 1).Style.Font.Italic = true;
            sheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
            sheet.Columns().AdjustToContents();
            sheet.SheetView.FreezeRows(1);

            var info = workbook.Worksheets.Add("Petunjuk");
            info.Cell("A1").Value = "PETUNJUK PENGISIAN DATA KUNJUNGAN HISTORIS";
            info.Cell("A1").Style.Font.Bold = true;
            info.Cell("A2").Value = "Hapus baris contoh sebelum mengisi data sebenarnya.";
            info.Cell("A3").Value = "Kolom wajib: Nama, Instansi, Keperluan, WaktuDatang.";
            info.Cell("A4").Value = "Format tanggal: yyyy-MM-dd HH:mm, contoh 2026-10-15 09:00.";
            info.Cell("A5").Value = "WaktuSelesai boleh kosong jika tidak tercatat.";
            info.Cell("A6").Value = "Status boleh kosong (otomatis Selesai) atau diisi Selesai.";
            info.Cell("A7").Value = "Keperluan harus sesuai pilihan aplikasi atau tulis sesuai buku jika Lainnya.";
            info.Column(1).Width = 100;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Kunjungan_Historis_BTSB.xlsx");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var access = RequireAdmin();
            if (access != null) return access;

            if (FileExcel == null || FileExcel.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Pilih file Excel terlebih dahulu.");
                return Page();
            }
            if (!Path.GetExtension(FileExcel.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Format file harus .xlsx.");
                return Page();
            }
            if (FileExcel.Length > 10 * 1024 * 1024)
            {
                ModelState.AddModelError(string.Empty, "Ukuran file maksimal 10 MB.");
                return Page();
            }

            using var stream = new MemoryStream();
            await FileExcel.CopyToAsync(stream);
            stream.Position = 0;

            int berhasil = 0, dilewati = 0;
            var errors = new List<string>();
            try
            {
                using var workbook = new XLWorkbook(stream);
                var sheet = workbook.Worksheets.FirstOrDefault(x => x.Name == "DataKunjungan") ?? workbook.Worksheet(1);
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var cell in sheet.Row(1).CellsUsed())
                    headerMap[cell.GetString().Trim()] = cell.Address.ColumnNumber;

                string[] requiredHeaders = { "Nama", "Instansi", "Keperluan", "WaktuDatang" };
                var missing = requiredHeaders.Where(h => !headerMap.ContainsKey(h)).ToList();
                if (missing.Count > 0)
                {
                    ModelState.AddModelError(string.Empty, "Kolom wajib tidak ditemukan: " + string.Join(", ", missing));
                    return Page();
                }

                var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
                if (lastRow - 1 > MaksimalBaris)
                {
                    ModelState.AddModelError(string.Empty, $"Maksimal {MaksimalBaris} baris data per impor.");
                    return Page();
                }

                var existing = await _context.Kunjungan.AsNoTracking().ToListAsync();
                var existingKeys = new HashSet<string>(existing.Select(MakeKey), StringComparer.OrdinalIgnoreCase);
                var importedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var records = new List<Kunjungan>();

                string Get(IXLRow row, string name) => headerMap.TryGetValue(name, out int col) ? row.Cell(col).GetFormattedString().Trim() : string.Empty;

                for (int rowNo = 2; rowNo <= lastRow; rowNo++)
                {
                    var row = sheet.Row(rowNo);
                    if (!row.CellsUsed().Any()) continue;
                    string nama = Get(row, "Nama");
                    string instansi = Get(row, "Instansi");
                    string keperluan = Get(row, "Keperluan");
                    string datangText = Get(row, "WaktuDatang");
                    if (string.IsNullOrWhiteSpace(nama) && string.IsNullOrWhiteSpace(instansi) && string.IsNullOrWhiteSpace(datangText)) continue;

                    if (string.IsNullOrWhiteSpace(nama) || string.IsNullOrWhiteSpace(instansi) || string.IsNullOrWhiteSpace(keperluan) || !TryReadDate(row, headerMap, "WaktuDatang", datangText, out DateTime datang))
                    {
                        errors.Add($"Baris {rowNo}: Nama, Instansi, Keperluan, atau WaktuDatang kosong/tidak valid.");
                        dilewati++;
                        continue;
                    }

                    string status = Get(row, "Status");
                    if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Selesai", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Baris {rowNo}: Status harus Selesai atau dikosongkan.");
                        dilewati++;
                        continue;
                    }

                    DateTime? selesai = null;
                    string selesaiText = Get(row, "WaktuSelesai");
                    if (!string.IsNullOrWhiteSpace(selesaiText))
                    {
                        if (!TryReadDate(row, headerMap, "WaktuSelesai", selesaiText, out DateTime parsedSelesai) || parsedSelesai < datang)
                        {
                            errors.Add($"Baris {rowNo}: WaktuSelesai tidak valid atau lebih awal dari WaktuDatang.");
                            dilewati++;
                            continue;
                        }
                        selesai = parsedSelesai;
                    }

                    var item = new Kunjungan
                    {
                        Nama = nama, Telepon = Get(row, "Telepon"), Instansi = instansi,
                        Email = Get(row, "Email"), Alamat = Get(row, "Alamat"),
                        Keperluan = keperluan, DetailKeperluan = Get(row, "DetailKeperluan"),
                        WaktuDatang = datang, WaktuSelesai = selesai,
                        Durasi = selesai.HasValue ? Math.Max(0, (int)Math.Round((selesai.Value - datang).TotalMinutes)) : null,
                        Status = "Selesai", SumberData = "Historis Excel"
                    };

                    string key = MakeKey(item);
                    if (existingKeys.Contains(key) || !importedKeys.Add(key))
                    {
                        errors.Add($"Baris {rowNo}: Data terindikasi duplikat (nama, instansi, dan waktu datang sama), dilewati.");
                        dilewati++;
                        continue;
                    }
                    records.Add(item);
                }

                if (records.Count > 0)
                {
                    _context.Kunjungan.AddRange(records);
                    await _context.SaveChangesAsync();
                    berhasil = records.Count;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "File Excel tidak dapat dibaca. Pastikan file berasal dari template BTSB dan tidak rusak. Detail: " + ex.Message);
                return Page();
            }

            TempData["Success"] = $"Impor selesai. Berhasil: {berhasil} data; dilewati: {dilewati} data.";
            if (errors.Count > 0) TempData["ImportErrors"] = string.Join("\n", errors.Take(30));
            return RedirectToPage("/DataKunjungan");
        }

        private static bool TryReadDate(IXLRow row, Dictionary<string, int> map, string name, string text, out DateTime value)
        {
            value = default;
            if (!map.TryGetValue(name, out int col)) return false;
            var cell = row.Cell(col);
            if (cell.TryGetValue<DateTime>(out value)) return true;
            string[] formats = { "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "dd/MM/yyyy HH:mm", "dd-MM-yyyy HH:mm", "yyyy-MM-dd" };
            return DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                || DateTime.TryParse(text, new CultureInfo("id-ID"), DateTimeStyles.None, out value);
        }

        private static string MakeKey(Kunjungan x) => $"{x.Nama.Trim().ToUpperInvariant()}|{x.Instansi.Trim().ToUpperInvariant()}|{x.WaktuDatang:yyyyMMddHHmm}";
    }
}
