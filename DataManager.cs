using System.Collections.Generic;
using System.IO;
using System.Text.Json;

// Класс для работы с JSON-файлом.
namespace BackupManager
{
    public static class DataManager
    {
        private static readonly string filePath = "profiles.json"; // Файл в директории приложения.

        // Сохранение списка профилей в JSON.
        public static void SaveProfiles(List<Profile> profiles)
        {
            string json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        // Загрузка списка профилей из JSON.
        public static List<Profile> LoadProfiles()
        {
            if (!File.Exists(filePath)) return null;
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<Profile>>(json);
        }
    }
}