// ProfileEditorWindow.xaml.cs
using System;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace BackupManager
{
    /// <summary>
    /// Логика взаимодействия для ProfileEditorWindow.xaml
    /// </summary>
    public partial class ProfileEditorWindow : Window
    {
        /// <summary>
        /// Профиль для редактирования или новый.
        /// </summary>
        public Profile Profile { get; set; }

        /// <summary>
        /// Конструктор окна.
        /// </summary>
        /// <param name="profile">Профиль для редактирования, или null для нового.</param>
        public ProfileEditorWindow(Profile profile = null)
        {
            InitializeComponent();
            if (profile != null)
            {
                Profile = profile;
                NameTextBox.Text = profile.Name;
                SourcePathTextBox.Text = profile.SourcePath;
                BackupPathTextBox.Text = profile.BackupPath;
            }
            else
            {
                Profile = new Profile();
            }
        }

        /// <summary>
        /// Обработчик кнопки выбора исходной папки.
        /// </summary>
        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this) == true)
            {
                SourcePathTextBox.Text = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// Обработчик кнопки выбора папки бэкапов.
        /// </summary>
        private void BrowseBackup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this) == true)
            {
                BackupPathTextBox.Text = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// Обработчик кнопки сохранения.
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Profile.Name = NameTextBox.Text;
            Profile.SourcePath = SourcePathTextBox.Text;
            Profile.BackupPath = BackupPathTextBox.Text;
            this.DialogResult = true;
            Close();
        }
    }
}