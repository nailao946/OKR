using System.Collections.Generic;
using System.Linq;
using ME.Models;

namespace ME.Data
{
    public class SettingsRepository
    {
        private const string FileName = "settings";

        public string GetValue(string key, string defaultValue = "")
        {
            var settings = JsonStore.Load<AppSettings>(FileName);
            return settings.FirstOrDefault(s => s.Key == key)?.Value ?? defaultValue;
        }

        public void SetValue(string key, string value)
        {
            var settings = JsonStore.Load<AppSettings>(FileName);
            var existing = settings.FirstOrDefault(s => s.Key == key);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                settings.Add(new AppSettings { Key = key, Value = value });
            }
            JsonStore.Save(FileName, settings);
        }
    }
}
