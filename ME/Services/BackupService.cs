using System;
using System.IO;
using ME.Data;

namespace ME.Services
{
    public class BackupService
    {
        public string CreateBackup()
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ME", "Backups");

            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            var fileName = $"me_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var backupPath = Path.Combine(backupDir, fileName);

            DatabaseHelper.BackupDatabase(backupPath);
            return backupPath;
        }

        public string[] GetBackups()
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ME", "Backups");

            if (!Directory.Exists(backupDir))
                return new string[0];

            return Directory.GetFiles(backupDir, "*.db");
        }

        public void RestoreBackup(string backupPath)
        {
            if (File.Exists(backupPath))
                DatabaseHelper.RestoreDatabase(backupPath);
        }
    }
}
