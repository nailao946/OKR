using System;
using System.Windows;
using ME.Services;

namespace ME
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Data.DatabaseHelper.Initialize();
            ThemeService.Initialize();
        }
    }
}
