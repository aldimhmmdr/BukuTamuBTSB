using BTSB.Models;
using Microsoft.EntityFrameworkCore;

namespace BTSB.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Kunjungan> Kunjungan { get; set; }

        public DbSet<VisitNotification> VisitNotifications { get; set; }

        public DbSet<User> Users { get; set; }
    }
}