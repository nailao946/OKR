namespace ME.Models
{
    public class AppSettings
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public static class SettingsKeys
    {
        public const string Theme = "Theme";
        public const string CornerRadius = "CornerRadius";
        public const string FocusSoundEnabled = "FocusSoundEnabled";
        public const string SoundEnabled = "SoundEnabled";
        public const string AutoStart = "AutoStart";
        public const string MinimizeToTray = "MinimizeToTray";
        public const string LastBackupDate = "LastBackupDate";
        public const string DefaultView = "DefaultView";
    }

    public enum AppTheme
    {
        Light,
        Dark
    }
}
