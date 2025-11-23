// AppDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace BackupManager
{
    /// <summary>
    /// Контекст базы данных для приложения.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Набор профилей.
        /// </summary>
        public DbSet<Profile> Profiles { get; set; }

        /// <summary>
        /// Набор бэкапов.
        /// </summary>
        public DbSet<Backup> Backups { get; set; }

        /// <summary>
        /// Конфигурация подключения к SQLite.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=backups.db");
        }
    }
}