using System;
using System.ComponentModel; // Для интерфейса INotifyPropertyChanged (уведомления об изменениях в UI).
using System.Runtime.CompilerServices; // Для CallerMemberName (автоматическое имя свойства).

// Это модель для одного бэкапа.
// Она хранит имя, дату, размер и путь.
namespace BackupManager
{
    public class Backup : INotifyPropertyChanged // Интерфейс для автоматического обновления UI при изменениях.
    {
        private string _name; // Приватное поле для свойства Name (чтобы отслеживать изменения).

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(); // Уведомляем UI, что свойство изменилось.
                }
            }
        }

        public DateTime CreationDate { get; set; } // Дата создания бэкапа.
        public string Size { get; set; } // Размер бэкапа в формате "95,00 MB" (строка для отображения в таблице).
        public string Path { get; set; } // Полный путь к файлу/папке бэкапа.
        public bool IsArchived { get; set; } // Флаг: true, если бэкап заархивирован в ZIP.

        // Вычисляемое свойство для столбца "Архивирован" в таблице (показывает "Да" или "Нет").
        public string IsArchivedString => IsArchived ? "Да" : "Нет";

        // Событие для уведомления об изменениях (требуется для INotifyPropertyChanged).
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для вызова события. [CallerMemberName] автоматически берёт имя свойства (например, "Name").
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}