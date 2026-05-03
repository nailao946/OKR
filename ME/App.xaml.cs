using System;
using System.Windows;

namespace ME
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Data.DatabaseHelper.Initialize();
        }
    }
}
