// MainWindow.xaml.cs
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace BackupManager
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Путь к файлу с профилями (JSON).
        /// </summary>
        private const string ProfilesFile = "profiles.json";

        /// <summary>
        /// Путь к файлу логов (текстовый).
        /// </summary>
        private const string LogsFile = "logs.txt";

        /// <summary>
        /// Список профилей (observable для binding).
        /// </summary>
        private ObservableCollection<Profile> profiles = new ObservableCollection<Profile>();

        /// <summary>
        /// Текущий выбранный профиль.
        /// </summary>
        private Profile selectedProfile;

        /// <summary>
        /// Список логов сессии (все логи после запуска, с опциональным ProfileName).
        /// </summary>
        private ObservableCollection<LogEntry> sessionLogs = new ObservableCollection<LogEntry>();

        /// <summary>
        /// Конструктор главного окна.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            // Загрузка профилей из JSON.
            LoadProfiles();
            // Привязка списка профилей к DataGrid.
            ProfilesDataGrid.ItemsSource = profiles;
            // Загрузка логов из файла в TextBox (только для отображения, сессионные в памяти).
            if (File.Exists(LogsFile))
            {
                GeneralLogsTextBox.Text = File.ReadAllText(LogsFile);
                GeneralLogsTextBox.ScrollToEnd();
            }
        }

        /// <summary>
        /// Загрузка профилей из JSON-файла.
        /// </summary>
        private void LoadProfiles()
        {
            profiles.Clear();
            if (File.Exists(ProfilesFile))
            {
                string json = File.ReadAllText(ProfilesFile);
                var loaded = JsonConvert.DeserializeObject<ObservableCollection<Profile>>(json);
                foreach (var profile in loaded)
                {
                    profiles.Add(profile);
                }
            }
        }

        /// <summary>
        /// Сохранение профилей в JSON-файл.
        /// </summary>
        private void SaveProfiles()
        {
            string json = JsonConvert.SerializeObject(profiles);
            File.WriteAllText(ProfilesFile, json);
        }

        /// <summary>
        /// Обработчик изменения выбора профиля.
        /// </summary>
        private void ProfilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProfile = ProfilesDataGrid.SelectedItem as Profile;
            if (selectedProfile != null)
            {
                LoadBackups();
            }
            else
            {
                BackupsDataGrid.ItemsSource = null;
            }
        }

        /// <summary>
        /// Загрузка бэкапов для выбранного профиля (сканирование папки).
        /// </summary>
        private void LoadBackups()
        {
            var backups = new ObservableCollection<Backup>();
            if (Directory.Exists(selectedProfile.BackupPath))
            {
                foreach (var folder in Directory.GetDirectories(selectedProfile.BackupPath))
                {
                    string folderName = Path.GetFileName(folder);
                    string metadataPath = Path.Combine(folder, "metadata.json");
                    string displayName = folderName;
                    DateTime created = DateTime.ParseExact(folderName, "yyyy-MM-dd_HHmmss", null);

                    // Загрузка метаданных, если существуют.
                    if (File.Exists(metadataPath))
                    {
                        string json = File.ReadAllText(metadataPath);
                        var metadata = JsonConvert.DeserializeObject<dynamic>(json);
                        displayName = metadata.DisplayName ?? folderName;
                    }

                    backups.Add(new Backup
                    {
                        DisplayName = displayName,
                        FolderName = folderName,
                        Created = created
                    });
                }
                // Сортировка по дате DESC.
                backups = new ObservableCollection<Backup>(backups.OrderByDescending(b => b.Created));
            }
            BackupsDataGrid.ItemsSource = backups;
        }

        /// <summary>
        /// Обработчик кнопки добавления профиля.
        /// </summary>
        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ProfileEditorWindow();
            if (editor.ShowDialog() == true && ValidateProfile(editor.Profile))
            {
                profiles.Add(editor.Profile);
                SaveProfiles();
                LogGeneral("Создан профиль " + editor.Profile.Name);
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
            if (editor.ShowDialog() == true && ValidateProfile(editor.Profile))
            {
                // Обновление в коллекции (binding обновит UI).
                profile.Name = editor.Profile.Name;
                profile.SourcePath = editor.Profile.SourcePath;
                profile.BackupPath = editor.Profile.BackupPath;
                SaveProfiles();
                LogGeneral("Профиль отредактирован " + profile.Name);
            }
        }

        /// <summary>
        /// Валидация профиля: поля заполнены, пути существуют, не совпадают.
        /// </summary>
        /// <param name="profile">Профиль для проверки.</param>
        /// <returns>True, если валиден.</returns>
        private bool ValidateProfile(Profile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Name) || string.IsNullOrWhiteSpace(profile.SourcePath) || string.IsNullOrWhiteSpace(profile.BackupPath))
            {
                MessageBox.Show("Все поля должны быть заполнены.");
                return false;
            }
            if (!Directory.Exists(profile.SourcePath))
            {
                MessageBox.Show("Исходная папка не существует.");
                return false;
            }
            if (!Directory.Exists(profile.BackupPath))
            {
                MessageBox.Show("Папка бэкапов не существует.");
                return false;
            }
            if (string.Equals(profile.SourcePath, profile.BackupPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Пути к исходной папке и бэкапам не должны совпадать.");
                return false;
            }
            return true;
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
            // Удаление связанных бэкапов (папок).
            if (Directory.Exists(profile.BackupPath))
            {
                foreach (var dir in Directory.GetDirectories(profile.BackupPath))
                {
                    Directory.Delete(dir, true);
                }
            }
            profiles.Remove(profile);
            SaveProfiles();
            LogGeneral("Профиль удален " + profile.Name);
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
                // Копирование папки.
                CopyDirectory(selectedProfile.SourcePath, backupDir, true);
                // Создание metadata.json.
                string metadataPath = Path.Combine(backupDir, "metadata.json");
                var metadata = new { DisplayName = displayName };
                File.WriteAllText(metadataPath, JsonConvert.SerializeObject(metadata));
                LoadBackups();
                LogGeneral("Создан бэкап для " + selectedProfile.Name);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании бэкапа: " + ex.Message);
                LogGeneral("Ошибка: " + ex.Message);
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
        /// Обработчик окончания редактирования ячейки (для переименования DisplayName).
        /// </summary>
        private void BackupsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var backup = e.Row.Item as Backup;
                var textBox = e.EditingElement as TextBox;
                string newName = textBox.Text;
                string oldName = backup.DisplayName;
                if (newName != oldName && !string.IsNullOrWhiteSpace(newName))
                {
                    string backupDir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);
                    string metadataPath = Path.Combine(backupDir, "metadata.json");
                    var metadata = new { DisplayName = newName };
                    File.WriteAllText(metadataPath, JsonConvert.SerializeObject(metadata));
                    backup.DisplayName = newName;
                    LogGeneral($"Бэкап '{oldName}' переименован в '{newName}' для " + selectedProfile.Name);
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки восстановления бэкапа (чистое восстановление).
        /// </summary>
        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var backup = button.Tag as Backup;
            string backupDir = Path.Combine(selectedProfile.BackupPath, backup.FolderName);
            try
            {
                // Чистка исходной папки перед восстановлением.
                foreach (var file in Directory.GetFiles(selectedProfile.SourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                }
                foreach (var dir in Directory.GetDirectories(selectedProfile.SourcePath, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                {
                    Directory.Delete(dir);
                }
                // Копирование из бэкапа.
                CopyDirectory(backupDir, selectedProfile.SourcePath, true);
                LogGeneral("Восстановлен бэкап " + backup.DisplayName + " для " + selectedProfile.Name);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка восстановления: " + ex.Message);
                LogGeneral("Ошибка: " + ex.Message);
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
            LoadBackups();
            LogGeneral("Удален бэкап " + backup.DisplayName + " для " + selectedProfile.Name);
        }

        /// <summary>
        /// Логирование события (все в одном методе, так как без вкладки логов).
        /// Добавляет в сессионные логи, в TextBox и append в файл.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private void LogGeneral(string message)
        {
            var now = DateTime.Now;
            var log = new LogEntry { Time = now, Message = message, ProfileName = selectedProfile?.Name };
            sessionLogs.Add(log);
            string logLine = now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + "\n";
            GeneralLogsTextBox.AppendText(logLine);
            GeneralLogsTextBox.ScrollToEnd();
            // Append в файл.
            File.AppendAllText(LogsFile, logLine);
        }
    }
}