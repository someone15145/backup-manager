using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.IO.Compression;
using System.Diagnostics;
using System.Text.Json.Serialization; // Не нужно, но на всякий случай для JSON (уже встроено).
using System.Text.Json; // Для опций сериализации, если нужно исключить вычисляемые свойства.

// Это код для главного окна. Здесь вся логика приложения.
namespace BackupManager
{
    public partial class MainWindow : Window
    {
        private List<Profile> profiles; // Список всех профилей.
        private Profile selectedProfile; // Выбранный профиль.
        private Timer autoBackupTimer; // Таймер для авто-бэкапов (каждые 30 мин).
        private int selectedProfileIndex = -1; // Для сохранения индекса выделения после обновлений.

        public MainWindow()
        {
            InitializeComponent();
            LoadData(); // Загружаем данные из JSON при запуске.
            SetupTimer(); // Настраиваем таймер.
        }

        // Загрузка данных из JSON.
        private void LoadData()
        {
            profiles = DataManager.LoadProfiles() ?? new List<Profile>(); // Если файла нет, создаём пустой список.
            // Сортируем профили по имени (ascending).
            profiles = profiles.OrderBy(p => p.Name).ToList();
            ProfilesGrid.ItemsSource = profiles; // Привязываем к таблице.
        }

        // Сохранение данных в JSON и обновление UI без сброса выделения.
        private void SaveData(bool refreshBackupsOnly = false)
        {
            // Опции для JSON: исключаем вычисляемые свойства вроде IsArchivedString, чтобы не сериализовать их (опционально, но чище).
            var options = new JsonSerializerOptions { WriteIndented = true };
            // Если нужно исключить IsArchivedString, добавь: options.IgnoreNullValues = true; но здесь оно не null.

            DataManager.SaveProfiles(profiles); // Сохраняем в файл (метод уже использует JsonSerializer).
            if (refreshBackupsOnly)
            {
                // Обновляем только таблицу бэкапов, сохраняя выделение профиля.
                if (selectedProfile != null)
                {
                    var sortedBackups = selectedProfile.Backups.OrderByDescending(b => b.CreationDate).ToList();
                    BackupsGrid.ItemsSource = sortedBackups;
                }
            }
            else
            {
                // Полная перезагрузка (для профилей).
                LoadData();
                // Восстанавливаем выделение, если был индекс.
                if (selectedProfileIndex >= 0 && selectedProfileIndex < profiles.Count)
                {
                    ProfilesGrid.SelectedIndex = selectedProfileIndex;
                }
            }
        }

        // Настройка таймера для авто-бэкапов.
        private void SetupTimer()
        {
            autoBackupTimer = new Timer(1800000); // 30 минут = 1 800 000 мс.
            autoBackupTimer.Elapsed += AutoBackupTimer_Elapsed;
            autoBackupTimer.Start();
        }

        // Событие таймера: проверяем изменения и создаём бэкап, если нужно.
        private void AutoBackupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (selectedProfile == null) return; // Если ничего не выбрано, пропускаем.

            Dispatcher.Invoke(() => // Выполняем в UI-потоке.
            {
                if (HasFilesChanged(selectedProfile))
                {
                    CreateBackup(false); // Создаём бэкап без архивирования (для авто).
                    MessageBox.Show("Авто-бэкап создан для профиля: " + selectedProfile.Name);
                }
            });
        }

        // Проверка, изменились ли файлы в оригинальной папке после последнего бэкапа.
        private bool HasFilesChanged(Profile profile)
        {
            if (!Directory.Exists(profile.OriginalPath)) return false;

            DateTime lastBackupTime = profile.LastBackupTime;
            var files = Directory.GetFiles(profile.OriginalPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (File.GetLastWriteTime(file) > lastBackupTime)
                    return true; // Если хоть один файл изменился, возвращаем true.
            }
            return false;
        }

        // Выбор профиля в таблице.
        private void ProfilesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProfile = ProfilesGrid.SelectedItem as Profile;
            selectedProfileIndex = ProfilesGrid.SelectedIndex; // Сохраняем индекс для будущего восстановления.
            if (selectedProfile != null)
            {
                // Сортируем бэкапы по дате descending (новые сверху).
                var sortedBackups = selectedProfile.Backups.OrderByDescending(b => b.CreationDate).ToList();
                BackupsGrid.ItemsSource = sortedBackups;
                CreateBackupButton.IsEnabled = true; // Активируем кнопку создания.
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
            var window = new AddEditProfileWindow(null); // Новое окно для добавления.
            window.Owner = this; // Устанавливаем главное окно как владельца.
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner; // Центрируем по владельцу.
            if (window.ShowDialog() == true)
            {
                profiles.Add(window.Profile);
                SaveData(); // Полная перезагрузка, выделение сбросится (но это ок, новый профиль).
            }
        }

        // Иконка "Изменить профиль" в таблице.
        private void EditProfileIcon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            selectedProfile = button.DataContext as Profile;
            if (selectedProfile == null) return;
            var window = new AddEditProfileWindow(selectedProfile); // Окно для изменения.
            window.Owner = this; // Устанавливаем главное окно как владельца.
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner; // Центрируем по владельцу.
            if (window.ShowDialog() == true)
            {
                SaveData(); // Полная перезагрузка, но индекс сохранится.
            }
        }

        // Иконка "Удалить профиль" в таблице.
        private void DeleteProfileIcon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profileToDelete = button.DataContext as Profile;
            if (profileToDelete == null) return;
            profiles.Remove(profileToDelete);
            SaveData(); // Полная перезагрузка, выделение перейдёт на следующий.
        }

        // Кнопка "Создать бэкап".
        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            CreateBackup(ArchiveCheckbox.IsChecked == true); // С архивированием, если галочка.
        }

        // Логика создания бэкапа (ручного или авто).
        private void CreateBackup(bool archive)
        {
            if (selectedProfile == null) return;

            try
            {
                if (!Directory.Exists(selectedProfile.OriginalPath) || !Directory.Exists(selectedProfile.BackupPath))
                {
                    MessageBox.Show("Неверный путь к папкам!");
                    return;
                }

                string backupName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"); // Формат имени с секундами.
                string backupFullPath = Path.Combine(selectedProfile.BackupPath, backupName);

                // Копируем или архивируем.
                if (archive)
                {
                    ZipFile.CreateFromDirectory(selectedProfile.OriginalPath, backupFullPath + ".zip"); // Архивируем в ZIP.
                    backupFullPath += ".zip"; // Меняем путь на ZIP.
                }
                else
                {
                    Directory.CreateDirectory(backupFullPath);
                    CopyDirectory(selectedProfile.OriginalPath, backupFullPath); // Копируем папку.
                }

                // Создаём объект бэкапа.
                var backup = new Backup
                {
                    Name = backupName,
                    CreationDate = DateTime.Now,
                    Path = backupFullPath,
                    Size = CalculateSize(backupFullPath), // Рассчитываем размер.
                    IsArchived = archive // Устанавливаем флаг архивирования.
                };

                selectedProfile.Backups.Add(backup);
                selectedProfile.LastBackupTime = DateTime.Now; // Обновляем время последнего бэкапа.
                SaveData(true); // Обновляем только бэкапы, без сброса выделения профиля.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
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

        // Расчёт размера папки или ZIP (в МБ). Обратите внимание: в твоём JSON размер "95,00 MB" — это из-за локали (запятая вместо точки). Если нужно, можно форматировать с CultureInfo.
        private string CalculateSize(string path)
        {
            long size = 0;
            if (File.Exists(path)) // Если ZIP.
            {
                size = new FileInfo(path).Length;
            }
            else if (Directory.Exists(path)) // Если папка.
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                size = files.Sum(f => new FileInfo(f).Length);
            }
            // Форматируем с запятой для русской локали (опционально).
            return (size / 1024 / 1024).ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("ru-RU")) + " MB";
        }

        // Иконка "Удалить бэкап" в таблице.
        private void DeleteBackupIcon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedBackup = button.DataContext as Backup;
            if (selectedBackup == null) return;

            // Удаляем файл/папку.
            if (File.Exists(selectedBackup.Path))
                File.Delete(selectedBackup.Path);
            else if (Directory.Exists(selectedBackup.Path))
                Directory.Delete(selectedBackup.Path, true);

            selectedProfile.Backups.Remove(selectedBackup);
            SaveData(true); // Обновляем только бэкапы.
        }

        // Кнопка "Открыть папку" над таблицей бэкапов (открывает папку выбранного бэкапа).
        private void OpenBackupFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBackup = BackupsGrid.SelectedItem as Backup;
            if (selectedBackup != null)
            {
                Process.Start("explorer.exe", Path.GetDirectoryName(selectedBackup.Path)); // Открываем родительскую папку.
            }
        }

        // Иконка "Открыть оригинальную папку" в таблице профилей.
        private void OpenOriginalFolder_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button.DataContext as Profile;
            if (profile != null && Directory.Exists(profile.OriginalPath))
            {
                Process.Start("explorer.exe", profile.OriginalPath);
            }
        }

        // Переименование имени бэкапа (при окончании редактирования ячейки). ИСПРАВЛЕНИЕ БАГА: Ручное обновление значения из TextBox в объект Backup.
        private void BackupsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit) // Только если редактирование подтверждено (Enter или потеря фокуса).
            {
                // Получаем ячейку и элемент редактирования (TextBox для столбца "Имя").
                var editedCell = e.EditingElement as TextBox;
                if (editedCell != null)
                {
                    // Получаем объект Backup из строки DataGrid.
                    var backup = e.Row.Item as Backup;
                    if (backup != null)
                    {
                        // Ручное присвоение нового имени из TextBox в объект (это обновит модель и вызовет INotifyPropertyChanged).
                        backup.Name = editedCell.Text;
                        // Теперь UI обновится автоматически, а SaveData сохранит в JSON.
                    }
                }
            }
            SaveData(true); // Сохраняем изменения в JSON и обновляем таблицу.
        }

        // Новый метод: иконка "Восстановить" в таблице бэкапов. Выполняет полную замену оригинальной папки содержимым бэкапа.
        private void RestoreBackupIcon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var backup = button.DataContext as Backup;
            if (backup == null || selectedProfile == null) return;

            // Проверяем существование оригинальной папки.
            if (!Directory.Exists(selectedProfile.OriginalPath))
            {
                MessageBox.Show("Оригинальная папка не существует!");
                return;
            }

            // Подтверждение от пользователя, чтобы избежать случайной потери данных.
            var result = MessageBox.Show($"Восстановить из бэкапа '{backup.Name}'?\nЭто заменит все файлы в оригинальной папке '{selectedProfile.OriginalPath}'.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                string tempExtractPath = null; // Временная папка для извлечения ZIP, если нужно.
                string sourcePath = backup.Path; // Путь к бэкапу (папка или ZIP).

                // Если бэкап — ZIP, извлекаем его во временную папку.
                if (backup.IsArchived)
                {
                    tempExtractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); // Создаём уникальную временную папку.
                    ZipFile.ExtractToDirectory(sourcePath, tempExtractPath); // Извлекаем ZIP.
                    sourcePath = tempExtractPath; // Теперь копируем из временной папки.
                }

                // Очищаем оригинальную папку (удаляем всё внутри, но не саму папку).
                var originalFiles = Directory.GetFiles(selectedProfile.OriginalPath);
                var originalDirs = Directory.GetDirectories(selectedProfile.OriginalPath);
                foreach (var file in originalFiles)
                {
                    File.Delete(file); // Удаляем файлы.
                }
                foreach (var dir in originalDirs)
                {
                    Directory.Delete(dir, true); // Рекурсивно удаляем подпапки.
                }

                // Копируем содержимое бэкапа в оригинальную папку.
                CopyDirectory(sourcePath, selectedProfile.OriginalPath);

                // Очищаем временную папку, если использовали ZIP.
                if (backup.IsArchived && Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true); // Удаляем временные файлы.
                }

                MessageBox.Show($"Восстановление завершено из бэкапа '{backup.Name}'!");
                selectedProfile.LastBackupTime = DateTime.Now; // Обновляем время последнего "бэкапа" (для авто-проверки изменений).
                SaveData(true); // Обновляем таблицу бэкапов (на всякий случай).
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка восстановления: " + ex.Message + "\nПроверьте права доступа к папкам.");
            }
        }
    }
}