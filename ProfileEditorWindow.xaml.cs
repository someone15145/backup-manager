// ProfileEditorWindow.xaml.cs
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Windows;

namespace BackupManager
{
    public partial class ProfileEditorWindow : Window
    {
        public Profile Profile { get; set; }

        public ProfileEditorWindow(Profile profile = null)
        {
            InitializeComponent();
            Profile = profile ?? new Profile();
            NameTextBox.Text = Profile.Name;
            SourcePathTextBox.Text = Profile.SourcePath;
            BackupPathTextBox.Text = Profile.BackupPath;
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this) == true)
                SourcePathTextBox.Text = dialog.SelectedPath;
        }

        private void BrowseBackup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this) == true)
                BackupPathTextBox.Text = dialog.SelectedPath;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            string name = NameTextBox.Text.Trim();
            string source = SourcePathTextBox.Text.Trim();
            string backup = BackupPathTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                ErrorTextBlock.Text = "Введите название профиля.";
                return;
            }
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(backup))
            {
                ErrorTextBlock.Text = "Укажите обе папки.";
                return;
            }
            if (!Directory.Exists(source))
            {
                ErrorTextBlock.Text = "Исходная папка не существует.";
                return;
            }
            if (!Directory.Exists(backup))
            {
                ErrorTextBlock.Text = "Папка бэкапов не существует.";
                return;
            }
            if (Path.GetFullPath(source).Equals(Path.GetFullPath(backup), StringComparison.OrdinalIgnoreCase))
            {
                ErrorTextBlock.Text = "Пути не должны совпадать.";
                return;
            }

            Profile.Name = name;
            Profile.SourcePath = source;
            Profile.BackupPath = backup;

            DialogResult = true;
            Close();
        }
    }
}