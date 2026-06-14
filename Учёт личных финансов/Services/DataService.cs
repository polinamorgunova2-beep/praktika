using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FinanceTracker.Services
{
    // Простое файловое хранилище: каждая сущность лежит в отдельном json.
    // По умолчанию — в папке AppData\FinanceTracker.
    public class DataService
    {
        private readonly string folder;

        public DataService()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FinanceTracker"))
        {
        }

        public DataService(string folder)
        {
            this.folder = folder;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        public string DataFolder
        {
            get { return folder; }
        }

        private string PathFor(string name)
        {
            return Path.Combine(folder, name + ".json");
        }

        public List<T> Load<T>(string name)
        {
            string path = PathFor(name);
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            return JsonConvert.DeserializeObject<List<T>>(json);
        }

        public void Save<T>(string name, List<T> data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(PathFor(name), json);
        }
    }
}
