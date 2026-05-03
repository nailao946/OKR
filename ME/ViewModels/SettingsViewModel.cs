using System.Windows.Input;
using ME.Core;
using ME.Models;

namespace ME.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private AppTheme _theme;
        private bool _focusSoundEnabled;
        private bool _autoStart;
        private bool _minimizeToTray;
        private double _cornerRadius;

        public AppTheme Theme
        {
            get => _theme;
            set
            {
                if (SetProperty(ref _theme, value))
                    _settingsRepo.SetValue(SettingsKeys.Theme, value.ToString());
            }
        }

        public bool FocusSoundEnabled
        {
            get => _focusSoundEnabled;
            set
            {
                if (SetProperty(ref _focusSoundEnabled, value))
                    _settingsRepo.SetValue(SettingsKeys.FocusSoundEnabled, value.ToString());
            }
        }

        public bool AutoStart
        {
            get => _autoStart;
            set
            {
                if (SetProperty(ref _autoStart, value))
                    _settingsRepo.SetValue(SettingsKeys.AutoStart, value.ToString());
            }
        }

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set
            {
                if (SetProperty(ref _minimizeToTray, value))
                    _settingsRepo.SetValue(SettingsKeys.MinimizeToTray, value.ToString());
            }
        }

        public double CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (SetProperty(ref _cornerRadius, value))
                    _settingsRepo.SetValue(SettingsKeys.CornerRadius, value.ToString());
            }
        }

        public ICommand CreateBackupCommand { get; }

        private readonly Data.SettingsRepository _settingsRepo;
        private readonly Services.BackupService _backupService;

        public SettingsViewModel()
        {
            _settingsRepo = new Data.SettingsRepository();
            _backupService = new Services.BackupService();

            LoadSettings();

            CreateBackupCommand = new RelayCommand(_ => CreateBackup());
        }

        private void LoadSettings()
        {
            var themeStr = _settingsRepo.GetValue(SettingsKeys.Theme, "Light");
            Theme = themeStr == "Dark" ? AppTheme.Dark : AppTheme.Light;
            FocusSoundEnabled = _settingsRepo.GetValue(SettingsKeys.FocusSoundEnabled, "True") == "True";
            AutoStart = _settingsRepo.GetValue(SettingsKeys.AutoStart, "False") == "True";
            MinimizeToTray = _settingsRepo.GetValue(SettingsKeys.MinimizeToTray, "True") == "True";
            CornerRadius = double.Parse(_settingsRepo.GetValue(SettingsKeys.CornerRadius, "8"));
        }

        private void CreateBackup()
        {
            _backupService.CreateBackup();
        }
    }
}
