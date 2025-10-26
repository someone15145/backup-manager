using System;
using System.Collections.Generic;

// Модель для профиля (игра или сохранение).
namespace BackupManager
{
    public class Profile
    {
        public string Name { get; set; } // Имя профиля, например "nms".
        public string OriginalPath { get; set; } // Путь к оригинальной папке сохранений.
        public string BackupPath { get; set; } // Путь к папке для бэкапов.
        public List<Backup> Backups { get; set; } // Список бэкапов этого профиля.
        public DateTime LastBackupTime { get; set; } // Время последнего бэкапа (для проверки изменений).
    }
}