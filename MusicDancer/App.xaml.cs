using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
namespace MusicDancer;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Windows chooses the graphics adapter when a process is created. Set the
        // preference before opening a window and relaunch once if it was missing.
        if (EnsureDedicatedGraphics()) { Shutdown(); return; }
        base.OnStartup(e);
        MainWindow = new MainWindow(); MainWindow.Show();
    }
    private static bool EnsureDedicatedGraphics()
    {
        string? executable=Environment.ProcessPath;
        if(executable==null||!executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(executable).Equals("dotnet.exe",StringComparison.OrdinalIgnoreCase))return false;
        try {
            using var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences");
            if(key.GetValue(executable) is string value && value.Contains("GpuPreference=2;",StringComparison.OrdinalIgnoreCase))return false;
            key.SetValue(executable,"GpuPreference=2;",RegistryValueKind.String);
            var start=new ProcessStartInfo(executable){UseShellExecute=false,WorkingDirectory=AppContext.BaseDirectory};
            foreach(string argument in Environment.GetCommandLineArgs().Skip(1))start.ArgumentList.Add(argument);
            return Process.Start(start)!=null;
        }
        catch(Exception ex) when(ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or System.ComponentModel.Win32Exception) { return false; }
    }
}
