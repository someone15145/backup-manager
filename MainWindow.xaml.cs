using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.IO.Compression;
using System.Diagnostics;
using System.Text.Json; // Для сериализации в JSON.

// Это главное окно приложения. Здесь вся основная логика: таблицы, кнопки, таймер, бэкапы и теперь лог.
namespace BackupManager
{
    public partial class MainWindow : Window
    {
        private List<Profile> profiles; // Список всех профилей.
        private Profile selectedProfile; // Текущий выбранный профиль.
        private Timer autoBackupTimer; // Таймер для автоматических бэкапов (каждые 30 минут).
        private int selectedProfileIndex = -1; // Индекс выбранного профиля (для сохранения после обновлений).

        public MainWindow()
        {
            InitializeComponent();
            LoadData(); // Загружаем данные при запуске.
            SetupTimer(); // Настраиваем таймер.
            LogMessage("Приложение запущено."); // Первое сообщение в лог.
        }

        // Статический метод для логирования из других окон (например, AddEditProfileWindow).
        public static void LogMessage(string message)
        {
            // Находим главное окно (Application.Current.MainWindow) и вызываем его метод Log.
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.Log(message);
            }
        }

        // Метод для добавления сообщения в лог (в нижней TextBox).
        private void Log(string message)
        {
            // Формат: дата-время до секунд - сообщение.
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
            LogTextBox.AppendText(logEntry); // Добавляем текст.
            LogTextBox.ScrollToEnd(); // Прокручиваем вниз.
        }

        // Загрузка данных из JSON.
        private void LoadData()
        {
            profiles = DataManager.LoadProfiles() ?? new List<Profile>(); // Если нет файла, пустой список.
            profiles = profiles.OrderBy(p => p.Name).ToList(); // Сортируем по имени (A-Z).
            ProfilesGrid.ItemsSource = profiles; // Привязываем к таблице профилей.
        }

        // Сохранение данных в JSON и обновление UI.
        private void SaveData(bool refreshBackupsOnly = false)
        {
            DataManager.SaveProfiles(profiles); // Сохраняем в файл.
            if (refreshBackupsOnly)
            {
                // Обновляем только таблицу бэкапов.
                if (selectedProfile != null)
                {
                    var sortedBackups = selectedProfile.Backups.OrderByDescending(b => b.CreationDate).ToList();
                    BackupsGrid.ItemsSource = sortedBackups;
                }
            }
            else
            {
                // Полная перезагрузка данных.
                LoadData();
                // Восстанавливаем выделение по индексу.
                if (selectedProfileIndex >= 0 && selectedProfileIndex < profiles.Count)
                {
                    ProfilesGrid.SelectedIndex = selectedProfileIndex;
                }
            }
        }

        // Настройка таймера для авто-бэкапов.
        private void SetupTimer()
        {
            autoBackupTimer = new Timer(1800000); // 30 минут в миллисекундах.
            autoBackupTimer.Elapsed += AutoBackupTimer_Elapsed;
            autoBackupTimer.Start();
        }

        // Событие таймера: проверяем изменения и создаём бэкап.
        private void AutoBackupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (selectedProfile == null) return;

            Dispatcher.Invoke(() => // Выполняем в UI-потоке (таймер в другом потоке).
            {
                if (HasFilesChanged(selectedProfile))
                {
                    CreateBackup(false); // Авто-бэкап без архивации.
                    Log("Авто-бэкап создан для профиля: " + selectedProfile.Name); // В лог вместо MessageBox.
                }
            });
        }

        // Проверка, изменились ли файлы в оригинальной папке.
        private bool HasFilesChanged(Profile profile)
        {
            if (!Directory.Exists(profile.OriginalPath)) return false;

            DateTime lastBackupTime = profile.LastBackupTime;
            var files = Directory.GetFiles(profile.OriginalPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (File.GetLastWriteTime(file) > lastBackupTime)
                    return true; // Если файл изменился, нужно бэкап.
            }
            return false;
        }

        // Выбор профиля в таблице.
        private void ProfilesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProfile = ProfilesGrid.SelectedItem as Profile;
            selectedProfileIndex = ProfilesGrid.SelectedIndex;
            if (selectedProfile != null)
            {
                var sortedBackups = selectedProfile.Backups.OrderByDescending(b => b.CreationDate).ToList();
                BackupsGrid.ItemsSource = sortedBackups;
                CreateBackupButton.IsEnabled = true;
            }
            else
            {
                BackupsGrid.ItemsSource = null;
                CreateBackupButton.IsEnabled = false;
            }
        }

        // Кнопка "Добавить профиль".
        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddEditProfileWindow(null);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (window.ShowDialog() == true)
            {
                profiles.Add(window.Profile);
                SaveData();
                Log("Добавлен новый профиль: " + window.Profile.Name);
            }
        }

        // Иконка "Изменить профиль".
        private void EditProfileIcon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            selectedProfile = button.DataContext as Profile;
            if (selectedProfile == null) return;
            var window = new AddEditProfileWindow(selectedProfile);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (window.ShowDialog() == true)
            {
                SaveData();
                Log("Изменён профиль: " + selectedProfile.Name);
            }
        }

        // Иконка "Удалить профиль".
        private void DeleteProfileIcon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profileToDelete = button.DataContext as Profile;
            if (profileToDelete == null) return;
            profiles.Remove(profileToDelete);
            SaveData();
            Log("Удалён профиль: " + profileToDelete.Name);
        }

        // Кнопка "Создать бэкап".
        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            CreateBackup(ArchiveCheckbox.IsChecked == true);
        }

        // Логика создания бэкапа.
        private void CreateBackup(bool archive)
        {
            if (selectedProfile == null) return;

            try
            {
                if (!Directory.Exists(selectedProfile.OriginalPath) || !Directory.Exists(selectedProfile.BackupPath))
                {
                    Log("Ошибка: Неверный путь к папкам!"); // В лог вместо MessageBox.
                    return;
                }

                string backupName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFullPath = Path.Combine(selectedProfile.BackupPath, backupName);

                if (archive)
                {
                    ZipFile.CreateFromDirectory(selectedProfile.OriginalPath, backupFullPath + ".zip");
                    backupFullPath += ".zip";
                }
                else
                {
                    Directory.CreateDirectory(backupFullPath);
                    CopyDirectory(selectedProfile.OriginalPath, backupFullPath);
                }

                var backup = new Backup
                {
                    Name = backupName,
                    CreationDate = DateTime.Now,
                    Path = backupFullPath,
                    Size = CalculateSize(backupFullPath),
                    IsArchived = archive
                };

                selectedProfile.Backups.Add(backup);
                selectedProfile.LastBackupTime = DateTime.Now;
                SaveData(true);
                Log("Создан бэкап: " + backup.Name + " для профиля " + selectedProfile.Name);
            }
            catch (Exception ex)
            {
                Log("Ошибка создания бэкапа: " + ex.Message);
            }
        }

        // Рекурсивное копирование папки.
        private void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }
            foreach (string dir in Directory.GetDirectories(source))
            {
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
            }
        }

        // Расчёт размера (в МБ, с запятой для русской локали).
        private string CalculateSize(string path)
        {
            long size = 0;
            if (File.Exists(path))
            {
                size = new FileInfo(path).Length;
            }
            else if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                size = files.Sum(f => new FileInfo(f).Length);
            }
            return (size / 1024 / 1024).ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("ru-RU")) + " MB";
        }

        // Иконка "Удалить бэкап".
        private void DeleteBackupIcon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedBackup = button.DataContext as Backup;
            if (selectedBackup == null) return;

            if (File.Exists(selectedBackup.Path))
                File.Delete(selectedBackup.Path);
            else if (Directory.Exists(selectedBackup.Path))
                Directory.Delete(selectedBackup.Path, true);

            selectedProfile.Backups.Remove(selectedBackup);
            SaveData(true);
            Log("Удалён бэкап: " + selectedBackup.Name);
        }

        // Кнопка "Открыть папку" (для выбранного бэкапа).
        private void OpenBackupFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBackup = BackupsGrid.SelectedItem as Backup;
            if (selectedBackup != null)
            {
                Process.Start("explorer.exe", Path.GetDirectoryName(selectedBackup.Path));
                Log("Открыта папка бэкапа: " + selectedBackup.Name);
            }
        }

        // Иконка "Открыть оригинальную папку".
        private void OpenOriginalFolder_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button.DataContext as Profile;
            if (profile != null && Directory.Exists(profile.OriginalPath))
            {
                Process.Start("explorer.exe", profile.OriginalPath);
                Log("Открыта оригинальная папка для профиля: " + profile.Name);
            }
        }

        // Окончание редактирования ячейки (для переименования бэкапа).
        private void BackupsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var editedCell = e.EditingElement as TextBox;
                if (editedCell != null)
                {
                    var backup = e.Row.Item as Backup;
                    if (backup != null)
                    {
                        backup.Name = editedCell.Text; // Обновляем имя.
                        Log("Переименован бэкап: " + backup.Name);
                    }
                }
            }
            SaveData(true);
        }

        // Иконка "Восстановить бэкап".
        private void RestoreBackupIcon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var backup = button.DataContext as Backup;
            if (backup == null || selectedProfile == null) return;

            if (!Directory.Exists(selectedProfile.OriginalPath))
            {
                Log("Ошибка: Оригинальная папка не существует!");
                return;
            }

            // Подтверждение оставляем в MessageBox, так как это интерактивный вопрос.
            var result = MessageBox.Show($"Восстановить из бэкапа '{backup.Name}'?\nЭто заменит все файлы в оригинальной папке '{selectedProfile.OriginalPath}'.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                string tempExtractPath = null;
                string sourcePath = backup.Path;

                if (backup.IsArchived)
                {
                    tempExtractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    ZipFile.ExtractToDirectory(sourcePath, tempExtractPath);
                    sourcePath = tempExtractPath;
                }

                // Очищаем оригинальную папку.
                var originalFiles = Directory.GetFiles(selectedProfile.OriginalPath);
                var originalDirs = Directory.GetDirectories(selectedProfile.OriginalPath);
                foreach (var file in originalFiles) File.Delete(file);
                foreach (var dir in originalDirs) Directory.Delete(dir, true);

                // Копируем из бэкапа.
                CopyDirectory(sourcePath, selectedProfile.OriginalPath);

                if (backup.IsArchived && Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }

                Log("Восстановление завершено из бэкапа: " + backup.Name);
                selectedProfile.LastBackupTime = DateTime.Now;
                SaveData(true);
            }
            catch (Exception ex)
            {
                Log("Ошибка восстановления: " + ex.Message + "\nПроверьте права доступа.");
            }
        }
    }
}