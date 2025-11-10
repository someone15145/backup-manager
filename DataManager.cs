using System.Collections.Generic;
using System.IO;
using System.Text.Json;

// Этот класс отвечает за сохранение и загрузку данных в JSON-файл.
// Он статический, чтобы его можно было вызывать из любого места без создания объекта.
namespace BackupManager
{
    public static class DataManager
    {
        private static readonly string filePath = "profiles.json"; // Путь к файлу (в папке приложения, например bin/Debug).

        // Сохраняет список профилей в JSON-файл.
        // Использует JsonSerializer для преобразования объектов в текст.
        public static void SaveProfiles(List<Profile> profiles)
        {
            string json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true }); // WriteIndented делает JSON читаемым (с отступами).
            File.WriteAllText(filePath, json); // Записываем в файл.
        }

        // Загружает список профилей из JSON-файла.
        // Если файла нет, возвращает null (в MainWindow это обработано как пустой список).
        public static List<Profile> LoadProfiles()
        {
            if (!File.Exists(filePath)) return null; // Нет файла — возвращаем null.
            string json = File.ReadAllText(filePath); // Читаем текст.
            return JsonSerializer.Deserialize<List<Profile>>(json); // Преобразуем обратно в объекты.
        }
    }
}