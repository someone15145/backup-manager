using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms; // Для FolderBrowserDialog (диалог выбора папки).

// Это окно для добавления или изменения профиля.
// Оно возвращает объект Profile, если OK pressed.
namespace BackupManager
{
    public partial class AddEditProfileWindow : Window
    {
        public Profile Profile { get; private set; } // Результат: новый или изменённый профиль.

        public AddEditProfileWindow(Profile existingProfile)
        {
            InitializeComponent();
            if (existingProfile != null)
            {
                Profile = existingProfile;
                NameTextBox.Text = Profile.Name;
                OriginalPathTextBox.Text = Profile.OriginalPath;
                BackupPathTextBox.Text = Profile.BackupPath;
            }
            else
            {
                Profile = new Profile { Backups = new List<Backup>(), LastBackupTime = DateTime.MinValue };
            }
        }

        // Кнопка для выбора оригинальной папки (открывает диалог).
        private void BrowseOriginal_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OriginalPathTextBox.Text = dialog.SelectedPath; // Устанавливаем выбранный путь в текстбокс.
                }
            }
        }

        // Кнопка для выбора папки бэкапов (аналогично).
        private void BrowseBackup_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    BackupPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        // Кнопка OK: проверяем поля и сохраняем.
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NameTextBox.Text) || string.IsNullOrEmpty(OriginalPathTextBox.Text) || string.IsNullOrEmpty(BackupPathTextBox.Text))
            {
                // Вместо всплывающего окна — записываем в лог главного окна.
                MainWindow.LogMessage("Ошибка: Заполните все поля в окне профиля!");
                return; // Не закрываем окно.
            }

            Profile.Name = NameTextBox.Text;
            Profile.OriginalPath = OriginalPathTextBox.Text;
            Profile.BackupPath = BackupPathTextBox.Text;
            DialogResult = true; // Закрываем окно с успехом.
        }

        // Кнопка Отмена: просто закрываем.
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}