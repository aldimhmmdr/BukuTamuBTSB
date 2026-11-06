using System.ComponentModel.DataAnnotations;

namespace BTSB.Models
{
    public class Kunjungan
    {
        public int Id { get; set; }

        public string Nama { get; set; } = string.Empty;

        public string Telepon { get; set; } = string.Empty;

        public string Instansi { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Alamat { get; set; } = string.Empty;

        public string Keperluan { get; set; } = string.Empty;

        public string? DetailKeperluan { get; set; }

        public DateTime WaktuDatang { get; set; }

        public DateTime? WaktuSelesai { get; set; }

        public int? Durasi { get; set; }

        public string Status { get; set; } = "Menunggu";

        // Menandai asal pencatatan: Online atau Historis Manual/Excel.
        public string SumberData { get; set; } = "Online";
    }
}