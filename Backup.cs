using System;
using System.ComponentModel.DataAnnotations;

namespace BackupManager
{
    /// <summary>
    /// Класс, представляющий бэкап.
    /// </summary>
    public class Backup
    {
        /// <summary>
        /// Уникальный идентификатор бэкапа.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор профиля, к которому относится бэкап.
        /// </summary>
        public int ProfileId { get; set; }

        /// <summary>
        /// Отображаемое название бэкапа (можно редактировать).
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Имя папки бэкапа (не меняется, используется для пути).
        /// </summary>
        public string FolderName { get; set; }

        /// <summary>
        /// Дата и время создания бэкапа.
        /// </summary>
        public DateTime Created { get; set; }
    }
}