using System;
using System.Windows.Input;
using ME.Core;
using ME.Models;

namespace ME.ViewModels
{
    public class FocusTimerViewModel : ViewModelBase
    {
        private TimeSpan _elapsed;
        private TimeSpan _countdownDuration;
        private bool _isRunning;
        private TimerMode _mode;
        private string _displayTime;

        public TimeSpan Elapsed
        {
            get => _elapsed;
            set
            {
                if (SetProperty(ref _elapsed, value))
                    UpdateDisplayTime();
            }
        }

        public TimeSpan CountdownDuration
        {
            get => _countdownDuration;
            set => SetProperty(ref _countdownDuration, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public TimerMode Mode
        {
            get => _mode;
            set
            {
                if (SetProperty(ref _mode, value))
                    UpdateDisplayTime();
            }
        }

        public string DisplayTime
        {
            get => _displayTime;
            set => SetProperty(ref _displayTime, value);
        }

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ToggleModeCommand { get; }

        private readonly Services.FocusTimerService _timerService;
        private int? _goalId;
        private int? _taskId;

        public FocusTimerViewModel(int? goalId = null, int? taskId = null)
        {
            _timerService = new Services.FocusTimerService();
            _goalId = goalId;
            _taskId = taskId;
            _mode = TimerMode.Stopwatch;
            _countdownDuration = TimeSpan.FromMinutes(25);
            _displayTime = "00:00:00";

            _timerService.Tick += OnTick;

            StartCommand = new RelayCommand(_ => Start());
            PauseCommand = new RelayCommand(_ => Pause(), _ => IsRunning);
            StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning || _elapsed.TotalSeconds > 0);
            ResetCommand = new RelayCommand(_ => Reset());
            ToggleModeCommand = new RelayCommand(_ => ToggleMode());
        }

        private void Start()
        {
            _timerService.Start(Mode, Mode == TimerMode.Countdown ? CountdownDuration : (TimeSpan?)null);
            IsRunning = true;
        }

        private void Pause()
        {
            _timerService.Pause();
            IsRunning = false;
        }

        private void Stop()
        {
            _timerService.Stop();
            IsRunning = false;
            _timerService.SaveSession(_goalId, _taskId, Mode);
        }

        private void Reset()
        {
            _timerService.Stop();
            IsRunning = false;
            Elapsed = TimeSpan.Zero;
        }

        private void ToggleMode()
        {
            Mode = Mode == TimerMode.Stopwatch ? TimerMode.Countdown : TimerMode.Stopwatch;
        }

        private void OnTick(TimeSpan elapsed)
        {
            Elapsed = elapsed;
        }

        private void UpdateDisplayTime()
        {
            if (Mode == TimerMode.Countdown)
            {
                var remaining = CountdownDuration - Elapsed;
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
                DisplayTime = remaining.ToString(@"hh\:mm\:ss");
            }
            else
            {
                DisplayTime = Elapsed.ToString(@"hh\:mm\:ss");
            }
        }
    }
}
