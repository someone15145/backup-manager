// Backup.cs
using System;

namespace BackupManager
{
    /// <summary>
    /// Класс, представляющий бэкап.
    /// DisplayName и FolderName всегда одинаковы (имя папки).
    /// </summary>
    public class Backup
    {
        /// <summary>
        /// Отображаемое имя бэкапа (то же, что имя папки).
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Имя папки бэкапа.
        /// </summary>
        public string FolderName { get; set; }

        /// <summary>
        /// Дата создания (берётся из свойств папки).
        /// </summary>
        public DateTime Created { get; set; }
    }
}