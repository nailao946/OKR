using System;
using System.Windows;
using System.Windows.Media;
using ME.Data;
using ME.Models;

namespace ME.Services
{
    public static class ThemeService
    {
        public static event Action<string> ThemeChanged;

        public static void Initialize()
        {
            var settings = new SettingsRepository();
            var theme = settings.GetValue(SettingsKeys.Theme, "Light");
            ApplyTheme(theme);
        }

        public static void ApplyTheme(string theme)
        {
            var isDark = theme == "Dark" || (theme == "System" && IsSystemDark());
            var app = Application.Current;
            if (app == null) return;

            var dicts = app.Resources.MergedDictionaries;

            foreach (var dict in dicts)
            {
                if (dict.Source != null && dict.Source.OriginalString.Contains("Styles.xaml"))
                {
                    if (isDark)
                    {
                        // Dark theme colors
                        dict["BackgroundColor"] = ColorFromString("#1C1C1E");
                        dict["CardColor"] = ColorFromString("#2C2C2E");
                        dict["TextColor"] = ColorFromString("#F2F2F7");
                        dict["SecondaryTextColor"] = ColorFromString("#AEAEB2");
                        dict["BorderColor"] = ColorFromString("#48484A");
                        dict["NavHoverColor"] = ColorFromString("#3A3A3C");
                        dict["NavSelectedColor"] = ColorFromString("#0A84FF");

                        dict["BackgroundBrush"] = new SolidColorBrush(ColorFromString("#1C1C1E"));
                        dict["CardBrush"] = new SolidColorBrush(ColorFromString("#2C2C2E"));
                        dict["TextBrush"] = new SolidColorBrush(ColorFromString("#F2F2F7"));
                        dict["SecondaryTextBrush"] = new SolidColorBrush(ColorFromString("#AEAEB2"));
                        dict["BorderBrush"] = new SolidColorBrush(ColorFromString("#48484A"));
                        dict["NavHoverBrush"] = new SolidColorBrush(ColorFromString("#3A3A3C"));
                        dict["NavSelectedBrush"] = new SolidColorBrush(ColorFromString("#0A84FF"));

                        dict["PrimaryBrush"] = new SolidColorBrush(ColorFromString("#0A84FF"));
                        dict["PrimaryColor"] = ColorFromString("#0A84FF");

                        dict["AccentRed"] = ColorFromString("#FF453A");
                        dict["AccentGreen"] = ColorFromString("#30D158");
                        dict["AccentBlue"] = ColorFromString("#0A84FF");
                        dict["AccentPink"] = ColorFromString("#FF375F");
                        dict["AccentGray"] = ColorFromString("#8E8E93");
                        dict["AccentYellow"] = ColorFromString("#FFD60A");

                        dict["GoalRedBrush"] = new SolidColorBrush(ColorFromString("#FF453A"));
                        dict["GoalGreenBrush"] = new SolidColorBrush(ColorFromString("#30D158"));
                        dict["GoalBlueBrush"] = new SolidColorBrush(ColorFromString("#0A84FF"));
                        dict["GoalPinkBrush"] = new SolidColorBrush(ColorFromString("#FF375F"));
                        dict["GoalGrayBrush"] = new SolidColorBrush(ColorFromString("#636366"));
                        dict["GoalYellowBrush"] = new SolidColorBrush(ColorFromString("#FFD60A"));

                        dict["AccentRedBrush"] = new SolidColorBrush(ColorFromString("#FF453A"));
                        dict["AccentGreenBrush"] = new SolidColorBrush(ColorFromString("#30D158"));
                        dict["AccentBlueBrush"] = new SolidColorBrush(ColorFromString("#0A84FF"));
                        dict["AccentPinkBrush"] = new SolidColorBrush(ColorFromString("#FF375F"));
                        dict["AccentGrayBrush"] = new SolidColorBrush(ColorFromString("#636366"));
                        dict["AccentYellowBrush"] = new SolidColorBrush(ColorFromString("#FFD60A"));
                    }
                    else
                    {
                        // Light theme colors
                        dict["BackgroundColor"] = ColorFromString("#F2F2F7");
                        dict["CardColor"] = ColorFromString("#FFFFFF");
                        dict["TextColor"] = ColorFromString("#1C1C1E");
                        dict["SecondaryTextColor"] = ColorFromString("#8E8E93");
                        dict["BorderColor"] = ColorFromString("#E5E5EA");
                        dict["NavHoverColor"] = ColorFromString("#F0F0F5");
                        dict["NavSelectedColor"] = ColorFromString("#007AFF");

                        dict["BackgroundBrush"] = new SolidColorBrush(ColorFromString("#F2F2F7"));
                        dict["CardBrush"] = new SolidColorBrush(ColorFromString("#FFFFFF"));
                        dict["TextBrush"] = new SolidColorBrush(ColorFromString("#1C1C1E"));
                        dict["SecondaryTextBrush"] = new SolidColorBrush(ColorFromString("#8E8E93"));
                        dict["BorderBrush"] = new SolidColorBrush(ColorFromString("#E5E5EA"));
                        dict["NavHoverBrush"] = new SolidColorBrush(ColorFromString("#F0F0F5"));
                        dict["NavSelectedBrush"] = new SolidColorBrush(ColorFromString("#007AFF"));

                        dict["PrimaryBrush"] = new SolidColorBrush(ColorFromString("#007AFF"));
                        dict["PrimaryColor"] = ColorFromString("#007AFF");

                        dict["AccentRed"] = ColorFromString("#FF3B30");
                        dict["AccentGreen"] = ColorFromString("#34C759");
                        dict["AccentBlue"] = ColorFromString("#007AFF");
                        dict["AccentPink"] = ColorFromString("#FF2D55");
                        dict["AccentGray"] = ColorFromString("#8E8E93");
                        dict["AccentYellow"] = ColorFromString("#FFCC00");

                        dict["GoalRedBrush"] = new SolidColorBrush(ColorFromString("#FF3B30"));
                        dict["GoalGreenBrush"] = new SolidColorBrush(ColorFromString("#34C759"));
                        dict["GoalBlueBrush"] = new SolidColorBrush(ColorFromString("#007AFF"));
                        dict["GoalPinkBrush"] = new SolidColorBrush(ColorFromString("#FF2D55"));
                        dict["GoalGrayBrush"] = new SolidColorBrush(ColorFromString("#8E8E93"));
                        dict["GoalYellowBrush"] = new SolidColorBrush(ColorFromString("#FFCC00"));

                        dict["AccentRedBrush"] = new SolidColorBrush(ColorFromString("#FF3B30"));
                        dict["AccentGreenBrush"] = new SolidColorBrush(ColorFromString("#34C759"));
                        dict["AccentBlueBrush"] = new SolidColorBrush(ColorFromString("#007AFF"));
                        dict["AccentPinkBrush"] = new SolidColorBrush(ColorFromString("#FF2D55"));
                        dict["AccentGrayBrush"] = new SolidColorBrush(ColorFromString("#8E8E93"));
                        dict["AccentYellowBrush"] = new SolidColorBrush(ColorFromString("#FFCC00"));
                    }
                    break;
                }
            }

            var settings = new SettingsRepository();
            settings.SetValue(SettingsKeys.Theme, theme);
            ThemeChanged?.Invoke(theme);
        }

        public static string GetCurrentTheme()
        {
            var settings = new SettingsRepository();
            return settings.GetValue(SettingsKeys.Theme, "Light");
        }

        public static bool IsDarkMode()
        {
            var theme = GetCurrentTheme();
            return theme == "Dark" || (theme == "System" && IsSystemDark());
        }

        private static bool IsSystemDark()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    return value is int v && v == 0;
                }
            }
            catch { }
            return false;
        }

        private static Color ColorFromString(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
