using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Media;
using ME.Data;

namespace ME.Services
{
    public static class SoundService
    {
        private static MediaPlayer _player;
        private static string _soundPath;

        static SoundService()
        {
            // Create a simple notification sound programmatically
            CreateNotificationSound();
        }

        public static bool IsEnabled()
        {
            var repo = new SettingsRepository();
            var setting = repo.GetValue(Models.SettingsKeys.SoundEnabled, "True");
            return setting == "True";
        }

        public static void SetEnabled(bool enabled)
        {
            var repo = new SettingsRepository();
            repo.SetValue(Models.SettingsKeys.SoundEnabled, enabled ? "True" : "False");
        }

        public static void PlayCompletionSound()
        {
            if (!IsEnabled()) return;

            try
            {
                // Use system beep as a simple notification
                SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Fallback: try MediaPlayer
                try
                {
                    if (_player != null)
                    {
                        _player.Stop();
                        _player.Play();
                    }
                }
                catch { }
            }
        }

        private static void CreateNotificationSound()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var soundDir = Path.Combine(appData, "ME", "Sounds");
                if (!Directory.Exists(soundDir))
                    Directory.CreateDirectory(soundDir);

                _soundPath = Path.Combine(soundDir, "notification.wav");

                if (!File.Exists(_soundPath))
                {
                    // Create a simple sine wave WAV file (bubble sound)
                    CreateBubbleWav(_soundPath);
                }

                _player = new MediaPlayer();
                _player.Open(new Uri(_soundPath));
                _player.Volume = 0.3;
            }
            catch
            {
                // Silently fail - sound is optional
            }
        }

        private static void CreateBubbleWav(string path)
        {
            // Create a simple bubble-like sound
            int sampleRate = 44100;
            double duration = 0.15;
            int samples = (int)(sampleRate * duration);

            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                // WAV header
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + samples * 2);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(samples * 2);

                // Generate bubble sound (frequency sweep)
                for (int i = 0; i < samples; i++)
                {
                    double t = (double)i / sampleRate;
                    double freq = 800 + 400 * (1 - t / duration); // Sweep from 1200Hz to 800Hz
                    double amplitude = 0.3 * (1 - t / duration); // Fade out
                    double sample = amplitude * Math.Sin(2 * Math.PI * freq * t);
                    short pcm = (short)(sample * short.MaxValue);
                    bw.Write(pcm);
                }
            }
        }
    }
}
