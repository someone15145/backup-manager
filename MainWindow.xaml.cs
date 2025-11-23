// MainWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace BackupManager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Список профилей (observable для binding).
        /// </summary>
        private ObservableCollection<Profile> profiles = new ObservableCollection<Profile>();

        /// <summary>
        /// Текущий выбранный профиль.
        /// </summary>
        private Profile selectedProfile;

        /// <summary>
        /// Список логов сессии (все логи после запуска, с опциональным ProfileId).
        /// </summary>
        private ObservableCollection<LogEntry> sessionLogs = new ObservableCollection<LogEntry>();

        /// <summary>
        /// Конструктор главного окна.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            // Инициализация базы данных (создание, если не существует).
            using (var context = new AppDbContext())
            {
                context.Database.EnsureCreated();
            }
            // Загрузка профилей из БД.
            LoadProfiles();
            // Привязка списка профилей к ListBox.
            ProfilesListBox.ItemsSource = profiles;
        }

        /// <summary>
        /// Загрузка профилей из базы данных с использованием LINQ.
        /// </summary>
        private void LoadProfiles()
        {
            using (var context = new AppDbContext())
            {
                profiles.Clear();
                var loadedProfiles = context.Profiles.ToList();
                foreach (var profile in loadedProfiles)
                {
                    profiles.Add(profile);
                }
            }
        }

        /// <summary>
        /// Обработчик изменения выбора профиля.
        /// </summary>
        private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProfile = ProfilesListBox.SelectedItem as Profile;
            if (selectedProfile != null)
            {
                LoadBackups();
                LoadProfileLogs();
            }
            else
            {
                BackupsDataGrid.ItemsSource = null;
                LogsDataGrid.ItemsSource = null;
            }
        }

        /// <summary>
        /// Загрузка бэкапов для выбранного профиля с использованием LINQ.
        /// </summary>
        private void LoadBackups()
        {
            using (var context = new AppDbContext())
            {
                var backups = context.Backups
                    .Where(b => b.ProfileId == selectedProfile.Id)
                    .OrderByDescending(b => b.Created)
                    .ToList();
                BackupsDataGrid.ItemsSource = new ObservableCollection<Backup>(backups);
            }
        }

        /// <summary>
        /// Загрузка логов для выбранного профиля (фильтрация сессионных логов).
        /// </summary>
        private void LoadProfileLogs()
        {
            var filteredLogs = sessionLogs
                .Where(l => l.ProfileId == selectedProfile.Id)
                .OrderByDescending(l => l.Time)
                .ToList();
            LogsDataGrid.ItemsSource = new ObservableCollection<LogEntry>(filteredLogs);
        }

        /// <summary>
        /// Обработчик кнопки добавления профиля.
        /// </summary>
        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ProfileEditorWindow();
            if (editor.ShowDialog() == true)
            {
                using (var context = new AppDbContext())
                {
                    context.Profiles.Add(editor.Profile);
                    context.SaveChanges();
                }
                LoadProfiles();
                LogGeneral("Создан профиль");
            }
        }

        /// <summary>
        /// Обработчик кнопки редактирования профиля.
        /// </summary>
        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button.Tag as Profile;
            var editor = new ProfileEditorWindow(profile);
            if (editor.ShowDialog() == true)
            {
                using (var context = new AppDbContext())
                {
                    context.Profiles.Update(editor.Profile);
                    context.SaveChanges();
                }
                LoadProfiles();
                LogGeneral("Профиль отредактирован");
                LogProfile(profile.Id, "Профиль отредактирован");
            }
        }

        /// <summary>
        /// Обработчик кнопки открытия папки бэкапов профиля.
        /// </summary>
        private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button.Tag as Profile;
            if (Directory.Exists(profile.BackupPath))
            {
                Process.Start("explorer.exe", profile.BackupPath);
            }
        }

        /// <summary>
        /// Обработчик кнопки удаления профиля.
        /// </summary>
        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button.Tag as Profile;

            using (var context = new AppDbContext())
            {
                // Удаление связанных бэкапов (папок и записей)
                var backups = context.Backups.Where(b => b.ProfileId == profile.Id).ToList();
                foreach (var backup in backups)
                {
                    string folder = Path.Combine(profile.BackupPath, backup.FolderName);
                    if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder, true);
                    }
                }
                context.Backups.RemoveRange(backups);

                // Удаление профиля
                context.Profiles.Remove(profile);
                context.SaveChanges();
            }
            LoadProfiles();
            LogGeneral("Профиль удален");
        }

        /// <summary>
        /// Обработчик кнопки создания бэкапа.
        /// </summary>
        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProfile == null) return;
            DateTime now = DateTime.Now;
            string folderName = now.ToString("yyyy-MM-dd_HHmmss");
            string displayName = now.ToString("yyyy-MM-dd HH:mm:ss");
            string backupDir = Path.Combine(selectedProfile.BackupPath, folderName);
            try
            {
                // Копирование папки
                CopyDirectory(selectedProfile.SourcePath, backupDir, true);
                // Сохранение в БД
                var newBackup = new Backup
                {
                    ProfileId = selectedProfile.Id,
                    DisplayName = displayName,
                    FolderName = folderName,
                    Created = now
                };
                using (var context = new AppDbContext())
                {
                    context.Backups.Add(newBackup);
                    context.SaveChanges();
                }
                LoadBackups();
                LogGeneral("Создан бэкап");
                LogProfile(selectedProfile.Id, "Создан бэкап " + displayName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании бэкапа: " + ex.Message);
                LogGeneral("Ошибка: " + ex.Message);
                LogProfile(selectedProfile.Id, "Ошибка при создании бэкапа: " + ex.Message);
            }
        }

        /// <summary>
        /// Метод для рекурсивного копирования директории.
        /// </summary>
        /// <param name="source">Исходная директория.</param>
        /// <param name="target">Целевая директория.</param>
        /// <param name="overwrite">Перезаписывать файлы.</param>
        private static void CopyDirectory(string source, string target, bool overwrite)
        {
            Directory.CreateDirectory(target);
            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(source, target));
            }
            foreach (string file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(source, target), overwrite);
            }
        }

        /// <summary>
        /// Обработчик окончания редактирования ячейки (для переименования бэкапа).
        /// </summary>
        private void BackupsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var backup = e.Row.Item as Backup;
                var textBox = e.EditingElement as TextBox;
                string newName = textBox.Text;
                string oldName = backup.DisplayName;
                if (newName != oldName)
                {
                    using (var context = new AppDbContext())
                    {
                        var dbBackup = context.Backups.Find(backup.Id);
                        dbBackup.DisplayName = newName;
                        context.SaveChanges();
                    }
                    LogGeneral($"Бэкап '{oldName}' переименован в '{newName}'");
                    LogProfile(selectedProfile.Id, $"Бэкап '{oldName}' переименован в '{newName}'");
                    backup.DisplayName = newName; // Обновление в локальной модели
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки восстановления бэкапа.
        /// </summary>
        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var backup = button.Tag as Backup;
            string backupDir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);
            try
            {
                CopyDirectory(backupDir, selectedProfile.SourcePath, true);
                LogGeneral("Восстановлен бэкап " + backup.DisplayName);
                LogProfile(selectedProfile.Id, "Восстановлен бэкап " + backup.DisplayName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка восстановления: " + ex.Message);
                LogGeneral("Ошибка: " + ex.Message);
                LogProfile(selectedProfile.Id, "Ошибка восстановления: " + ex.Message);
            }
        }

        /// <summary>
        /// Обработчик кнопки открытия бэкапа в проводнике.
        /// </summary>
        private void OpenBackupInExplorer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var backup = button.Tag as Backup;
            string backupDir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);
            if (Directory.Exists(backupDir))
            {
                Process.Start("explorer.exe", backupDir);
            }
        }

        /// <summary>
        /// Обработчик кнопки удаления бэкапа.
        /// </summary>
        private void DeleteBackup_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var backup = button.Tag as Backup;
            string backupDir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);
            if (Directory.Exists(backupDir))
            {
                Directory.Delete(backupDir, true);
            }
            using (var context = new AppDbContext())
            {
                var dbBackup = context.Backups.Find(backup.Id);
                context.Backups.Remove(dbBackup);
                context.SaveChanges();
            }
            LoadBackups();
            LogGeneral("Удален бэкап " + backup.DisplayName);
            LogProfile(selectedProfile.Id, "Удален бэкап " + backup.DisplayName);
        }

        /// <summary>
        /// Логирование общего события (не привязанного к профилю).
        /// Добавляет в сессионные логи и в нижнюю область.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private void LogGeneral(string message)
        {
            var now = DateTime.Now;
            var log = new LogEntry { Time = now, Message = message, ProfileId = null };
            sessionLogs.Add(log);
            GeneralLogsTextBox.AppendText(now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + "\n");
            GeneralLogsTextBox.ScrollToEnd();
        }

        /// <summary>
        /// Логирование события, привязанного к профилю.
        /// Добавляет в сессионные логи и в нижнюю область (как общее).
        /// </summary>
        /// <param name="profileId">ID профиля.</param>
        /// <param name="message">Сообщение.</param>
        private void LogProfile(int profileId, string message)
        {
            var now = DateTime.Now;
            var log = new LogEntry { Time = now, Message = message, ProfileId = profileId };
            sessionLogs.Add(log);
            GeneralLogsTextBox.AppendText(now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + "\n");
            GeneralLogsTextBox.ScrollToEnd();
            // Если профиль выбран, обновляем таблицу логов
            if (selectedProfile != null && selectedProfile.Id == profileId)
            {
                LoadProfileLogs();
            }
        }
    }
}