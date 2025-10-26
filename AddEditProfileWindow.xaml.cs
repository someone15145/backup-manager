using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms; // Для FolderBrowserDialog.

// Окно для добавления или изменения профиля.
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

        // Выбор оригинальной папки через проводник.
        private void BrowseOriginal_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OriginalPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        // Выбор папки бэкапов через проводник.
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

        // Кнопка OK: сохраняем изменения.
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NameTextBox.Text) || string.IsNullOrEmpty(OriginalPathTextBox.Text) || string.IsNullOrEmpty(BackupPathTextBox.Text))
            {
                System.Windows.MessageBox.Show("Заполните все поля!");
                return;
            }

            Profile.Name = NameTextBox.Text;
            Profile.OriginalPath = OriginalPathTextBox.Text;
            Profile.BackupPath = BackupPathTextBox.Text;
            DialogResult = true; // Закрываем с успехом.
        }

        // Кнопка Отмена.
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}