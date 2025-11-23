// Profile.cs
using System.ComponentModel.DataAnnotations;

namespace BackupManager
{
    /// <summary>
    /// Класс, представляющий профиль бэкапа.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// Уникальный идентификатор профиля.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Название профиля.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Путь к исходной папке для бэкапа.
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Путь к папке, где хранятся бэкапы.
        /// </summary>
        public string BackupPath { get; set; }
    }
}