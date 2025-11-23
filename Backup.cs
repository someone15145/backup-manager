// Backup.cs
using System;

namespace BackupManager
{
    /// <summary>
    /// Класс, представляющий бэкап (метаданные из JSON или папки).
    /// </summary>
    public class Backup
    {
        /// <summary>
        /// Отображаемое название бэкапа (можно редактировать в metadata.json).
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Имя папки бэкапа (не меняется, используется для пути).
        /// </summary>
        public string FolderName { get; set; }

        /// <summary>
        /// Дата и время создания бэкапа (парсится из FolderName или JSON).
        /// </summary>
        public DateTime Created { get; set; }
    }
}