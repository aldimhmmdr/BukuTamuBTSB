using System;

namespace BTSB.Models
{
    public class VisitNotification
    {
        public int Id { get; set; }

        public int KunjunganId { get; set; }

        public string Judul { get; set; } = string.Empty;

        public string Pesan { get; set; } = string.Empty;

        public bool Dibaca { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}