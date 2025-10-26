using System;
using System.ComponentModel; // Добавили для INotifyPropertyChanged.
using System.Runtime.CompilerServices; // Для CallerMemberName.

// Модель для одного бэкапа.
namespace BackupManager
{
    public class Backup : INotifyPropertyChanged // Реализуем интерфейс для уведомления об изменениях.
    {
        private string _name; // Приватное поле для Name, чтобы отслеживать изменения.

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(); // Уведомляем UI об изменении.
                }
            }
        }

        public DateTime CreationDate { get; set; } // Дата создания.
        public string Size { get; set; } // Размер в МБ (строка для отображения).
        public string Path { get; set; } // Полный путь к бэкапу (папка или ZIP).
        public bool IsArchived { get; set; } // Флаг: заархивирован ли (для столбца).

        // Свойство для отображения в таблице (Да/Нет).
        public string IsArchivedString => IsArchived ? "Да" : "Нет";

        // Событие для INotifyPropertyChanged.
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для вызова события (с CallerMemberName для автоматического имени свойства).
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}