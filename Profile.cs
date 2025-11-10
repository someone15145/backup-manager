using System;
using System.Collections.Generic;

// Это модель для профиля (например, для игры или папки с файлами).
// Она хранит имя, пути и список бэкапов.
namespace BackupManager
{
    public class Profile
    {
        public string Name { get; set; } // Имя профиля, например "Мои документы".
        public string OriginalPath { get; set; } // Путь к оригинальной папке (откуда копируем).
        public string BackupPath { get; set; } // Путь к папке для хранения бэкапов.
        public List<Backup> Backups { get; set; } // Список всех бэкапов этого профиля.
        public DateTime LastBackupTime { get; set; } // Дата и время последнего бэкапа (для проверки изменений в файлах).
    }
}