using BTSB.Data;
using BTSB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace BTSB.Pages
{
    public class BukuModel : PageModel
    {
        private readonly AppDbContext _context;

        public BukuModel(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // DATA FORM
        // =====================================================

        [BindProperty]
        public string Nama { get; set; } = string.Empty;

        [BindProperty]
        public string Telepon { get; set; } = string.Empty;

        [BindProperty]
        public string Instansi { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Alamat { get; set; } = string.Empty;

        [BindProperty]
        public string Keperluan { get; set; } = string.Empty;

        // Nullable karena hanya wajib ketika memilih "Lainnya"
        [BindProperty]
        public string? DetailKeperluan { get; set; }


        // =====================================================
        // PESAN
        // =====================================================

        public string? Pesan { get; set; }


        // =====================================================
        // GET
        // =====================================================

        public void OnGet()
        {
        }


        // =====================================================
        // POST
        // =====================================================

        public async Task<IActionResult> OnPostAsync()
        {
            // =================================================
            // BERSIHKAN DATA
            // =================================================

            Nama =
                (Nama ?? string.Empty).Trim();

            Telepon =
                (Telepon ?? string.Empty).Trim();

            Instansi =
                (Instansi ?? string.Empty).Trim();

            Email =
                (Email ?? string.Empty).Trim();

            Alamat =
                (Alamat ?? string.Empty).Trim();

            Keperluan =
                (Keperluan ?? string.Empty).Trim();

            DetailKeperluan =
                DetailKeperluan?.Trim();


            // =================================================
            // VALIDASI NAMA
            // =================================================

            if (string.IsNullOrWhiteSpace(Nama))
            {
                ModelState.AddModelError(
                    nameof(Nama),
                    "Nama lengkap wajib diisi."
                );
            }
            else if (!Regex.IsMatch(
                Nama,
                @"^[A-Za-zÀ-ÖØ-öø-ÿ\s.'-]+$"))
            {
                ModelState.AddModelError(
                    nameof(Nama),
                    "Nama hanya boleh berisi huruf dan spasi."
                );
            }


            // =================================================
            // VALIDASI NOMOR TELEPON
            // =================================================

            if (string.IsNullOrWhiteSpace(Telepon))
            {
                ModelState.AddModelError(
                    nameof(Telepon),
                    "Nomor telepon wajib diisi."
                );
            }
            else if (!Regex.IsMatch(
                Telepon,
                @"^[0-9]{10,15}$"))
            {
                ModelState.AddModelError(
                    nameof(Telepon),
                    "Nomor telepon harus terdiri dari 10-15 digit angka."
                );
            }


            // =================================================
            // VALIDASI INSTANSI
            // =================================================

            if (string.IsNullOrWhiteSpace(Instansi))
            {
                ModelState.AddModelError(
                    nameof(Instansi),
                    "Instansi / asal wajib diisi."
                );
            }


            // =================================================
            // VALIDASI EMAIL
            // =================================================

            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError(
                    nameof(Email),
                    "Email wajib diisi."
                );
            }
            else
            {
                try
                {
                    var mailAddress =
                        new MailAddress(Email);

                    if (!string.Equals(
                        mailAddress.Address,
                        Email,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(
                            nameof(Email),
                            "Format email tidak valid."
                        );
                    }
                }
                catch
                {
                    ModelState.AddModelError(
                        nameof(Email),
                        "Format email tidak valid. Contoh: nama@email.com."
                    );
                }
            }


            // =================================================
            // VALIDASI ALAMAT
            // =================================================

            if (string.IsNullOrWhiteSpace(Alamat))
            {
                ModelState.AddModelError(
                    nameof(Alamat),
                    "Alamat wajib diisi."
                );
            }


            // =================================================
            // VALIDASI KEPERLUAN
            // =================================================

            if (string.IsNullOrWhiteSpace(Keperluan))
            {
                ModelState.AddModelError(
                    nameof(Keperluan),
                    "Keperluan kunjungan wajib dipilih."
                );
            }


            // =================================================
            // VALIDASI DETAIL KEPERLUAN
            //
            // HANYA WAJIB JIKA MEMILIH "LAINNYA"
            // =================================================

            if (
                Keperluan == "Lainnya" &&
                string.IsNullOrWhiteSpace(DetailKeperluan)
            )
            {
                ModelState.AddModelError(
                    nameof(DetailKeperluan),
                    "Detail keperluan wajib diisi jika memilih Lainnya."
                );
            }


            // =================================================
            // JIKA VALIDASI GAGAL
            // =================================================

            if (!ModelState.IsValid)
            {
                Pesan =
                    "Mohon lengkapi dan periksa kembali data yang Anda masukkan.";

                return Page();
            }


            // =================================================
            // BUAT DATA KUNJUNGAN
            // =================================================

            var kunjungan = new Kunjungan
            {
                Nama =
                    Nama,

                Telepon =
                    Telepon,

                Instansi =
                    Instansi,

                Email =
                    Email,

                Alamat =
                    Alamat,

                Keperluan =
                    Keperluan,

                DetailKeperluan =
                    DetailKeperluan ?? string.Empty,

                WaktuDatang =
                    DateTime.Now,

                Status =
                    "Menunggu"
            };


            // =================================================
            // SIMPAN DATA KUNJUNGAN
            // =================================================

            _context.Kunjungan.Add(kunjungan);

            await _context.SaveChangesAsync();


            // =================================================
            // BUAT NOTIFIKASI
            // =================================================

            var notification =
                new VisitNotification
                {
                    KunjunganId =
                        kunjungan.Id,

                    Judul =
                        "Tamu Baru",

                    Pesan =
                        $"{kunjungan.Nama} telah mengisi buku tamu.",

                    Dibaca =
                        false,

                    CreatedAt =
                        DateTime.Now
                };


            _context.VisitNotifications.Add(
                notification
            );

            await _context.SaveChangesAsync();


            // =================================================
            // REDIRECT SETELAH BERHASIL
            // =================================================

            return RedirectToPage(
                "/Buku",
                new
                {
                    berhasil = true
                }
            );
        }
    }
}