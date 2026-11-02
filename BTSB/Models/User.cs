using System.ComponentModel.DataAnnotations;

namespace BTSB.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama wajib diisi.")]
        [StringLength(100)]
        public string Nama { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username wajib diisi.")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        // Nullable agar akun lama tetap dapat dibaca sebelum email diisi.
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        [StringLength(255)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password wajib diisi.")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role wajib dipilih.")]
        [StringLength(20)]
        public string Role { get; set; } = "Petugas";

        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}