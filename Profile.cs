// Profile.cs
namespace BackupManager
{
    /// <summary>
    /// Класс, представляющий профиль бэкапа (хранится в JSON).
    /// </summary>
    public class Profile
    {
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