// MainWindow.xaml.cs
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BackupManager
{
    public partial class MainWindow : Window
    {
        private const string ProfilesFile = "profiles.json";
        private const string LogsFile = "logs.txt";

        private ObservableCollection<Profile> profiles = new ObservableCollection<Profile>();
        private Profile selectedProfile;

        public MainWindow()
        {
            InitializeComponent();
            LoadProfiles();
            ProfilesDataGrid.ItemsSource = profiles;

            if (File.Exists(LogsFile))
            {
                GeneralLogsTextBox.Text = File.ReadAllText(LogsFile);
                GeneralLogsTextBox.ScrollToEnd();
            }
        }

        private void LoadProfiles()
        {
            profiles.Clear();
            if (File.Exists(ProfilesFile))
            {
                string json = File.ReadAllText(ProfilesFile);
                var loaded = JsonConvert.DeserializeObject<ObservableCollection<Profile>>(json);
                if (loaded != null)
                    foreach (var p in loaded) profiles.Add(p);
            }
        }

        private void SaveProfiles() => File.WriteAllText(ProfilesFile, JsonConvert.SerializeObject(profiles));

        private void ProfilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProfile = ProfilesDataGrid.SelectedItem as Profile;
            LoadBackups();
        }

        private void LoadBackups()
        {
            var backups = new ObservableCollection<Backup>();
            if (selectedProfile != null && Directory.Exists(selectedProfile.BackupPath))
            {
                foreach (var folder in Directory.GetDirectories(selectedProfile.BackupPath))
                {
                    string folderName = Path.GetFileName(folder);
                    DateTime created = Directory.GetCreationTime(folder);
                    backups.Add(new Backup { DisplayName = folderName, FolderName = folderName, Created = created });
                }
                backups = new ObservableCollection<Backup>(backups.OrderByDescending(b => b.Created));
            }
            BackupsDataGrid.ItemsSource = backups;
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ProfileEditorWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            if (editor.ShowDialog() == true)
            {
                profiles.Add(editor.Profile);
                SaveProfiles();
                LogGeneral($"Создан профиль {editor.Profile.Name}");
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button?.Tag as Profile;
            if (profile == null) return;

            var editor = new ProfileEditorWindow(profile) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            if (editor.ShowDialog() == true)
            {
                profile.Name = editor.Profile.Name;
                profile.SourcePath = editor.Profile.SourcePath;
                profile.BackupPath = editor.Profile.BackupPath;
                SaveProfiles();
                LogGeneral($"Профиль переименован на {profile.Name}");
            }
        }

        private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            var profile = (sender as Button)?.Tag as Profile;
            if (profile != null && Directory.Exists(profile.BackupPath))
                Process.Start("explorer.exe", profile.BackupPath);
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var profile = (sender as Button)?.Tag as Profile;
            if (profile == null) return;

            if (Directory.Exists(profile.BackupPath))
            {
                foreach (var dir in Directory.GetDirectories(profile.BackupPath))
                    Directory.Delete(dir, true);
            }

            profiles.Remove(profile);
            SaveProfiles();
            LogGeneral($"Профиль удалён {profile.Name}");
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProfile == null) return;

            DateTime now = DateTime.Now;
            string folderName = $"Backup {now:yyyy-MM-dd HH-mm-ss}";
            string backupDir = Path.Combine(selectedProfile.BackupPath, folderName);

            try
            {
                CopyDirectory(selectedProfile.SourcePath, backupDir, true);
                LoadBackups();
                LogGeneral($"Создан бэкап {folderName} для {selectedProfile.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания бэкапа: {ex.Message}");
                LogGeneral($"Ошибка: {ex.Message}");
            }
        }

        private static void CopyDirectory(string source, string target, bool overwrite)
        {
            Directory.CreateDirectory(target);
            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(source, target));

            foreach (string file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(source, target), overwrite);
        }

        private void BackupsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            var backup = e.Row.Item as Backup;
            var textBox = e.EditingElement as TextBox;
            string newName = textBox?.Text.Trim() ?? "";
            if (string.IsNullOrEmpty(newName) || newName == backup.DisplayName) return;

            string oldDir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);
            string newFolderName = newName;
            string newDir = Path.Combine(selectedProfile.BackupPath, newFolderName);

            // Разрешение конфликтов имён
            int i = 1;
            while (Directory.Exists(newDir))
            {
                newFolderName = $"{newName} ({i++})";
                newDir = Path.Combine(selectedProfile.BackupPath, newFolderName);
            }

            try
            {
                Directory.Move(oldDir, newDir);
                backup.DisplayName = newFolderName;
                backup.FolderName = newFolderName;
                LogGeneral($"Бэкап переименован в {newFolderName} для {selectedProfile.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось переименовать: {ex.Message}");
                e.Cancel = true;
            }
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            var backup = (sender as Button)?.Tag as Backup;
            if (backup == null || selectedProfile == null) return;

            string backupDir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);

            try
            {
                // Полная очистка исходной папки
                foreach (var file in Directory.GetFiles(selectedProfile.SourcePath, "*.*", SearchOption.AllDirectories))
                    File.Delete(file);
                foreach (var dir in Directory.GetDirectories(selectedProfile.SourcePath, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                    Directory.Delete(dir);

                CopyDirectory(backupDir, selectedProfile.SourcePath, true);
                LogGeneral($"Восстановлен бэкап {backup.DisplayName} для {selectedProfile.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка восстановления: {ex.Message}");
                LogGeneral($"Ошибка: {ex.Message}");
            }
        }

        private void OpenBackupInExplorer_Click(object sender, RoutedEventArgs e)
        {
            var backup = (sender as Button)?.Tag as Backup;
            if (backup != null && selectedProfile != null)
            {
                string dir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);
                if (Directory.Exists(dir))
                    Process.Start("explorer.exe", dir);
            }
        }

        private void DeleteBackup_Click(object sender, RoutedEventArgs e)
        {
            var backup = (sender as Button)?.Tag as Backup;
            if (backup == null || selectedProfile == null) return;

            string dir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            LoadBackups();
            LogGeneral($"Удалён бэкап {backup.DisplayName} для {selectedProfile.Name}");
        }

        private void LogGeneral(string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\n";
            GeneralLogsTextBox.AppendText(line);
            GeneralLogsTextBox.ScrollToEnd();
            File.AppendAllText(LogsFile, line);
        }
    }
}