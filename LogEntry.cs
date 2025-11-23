// LogEntry.cs
using System;

namespace BackupManager
{
    /// <summary>
    /// Класс, представляющий запись лога.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Дата и время записи лога.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Сообщение лога.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Идентификатор профиля, к которому относится лог (null для общих).
        /// </summary>
        public string ProfileName { get; set; }
    }
}