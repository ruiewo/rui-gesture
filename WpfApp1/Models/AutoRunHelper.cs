namespace RuiGesture.Models;

using System;
using System.Reflection;

public static class AutoRunHelper
{
    private static Microsoft.Win32.RegistryKey GetRegistryKey()
        => Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);


    private static bool AutoRun
    {
        get
        {
            var registryKey = GetRegistryKey();

            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                var res = registryKey.GetValue(assembly.GetName().Name);
                return res != null &&
                       (string)res == assembly.Location;
            }
            finally
            {
                registryKey.Close();
            }
        }
        set
        {
            var registryKey = GetRegistryKey();
            var assembly = Assembly.GetExecutingAssembly();
            var appName = assembly.GetName().Name;

            try
            {
                if (value)
                {
                    registryKey.SetValue(appName, assembly.Location);
                    Verbose.Print("Autorun was set to true");
                    return;
                }

                if (registryKey.GetValue(appName) == null)
                {
                    return;
                }

                try
                {
                    registryKey.DeleteValue(appName);
                    Verbose.Print("Autorun was set to false");
                }
                catch (ArgumentException ex)
                {
                    Verbose.Error($"An exception was thrown while writing registory value: {ex.ToString()}");
                }
            }
            finally
            {
                registryKey.Close();
            }
        }
    }
}