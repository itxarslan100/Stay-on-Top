using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PinToTop
{
    /// <summary>
    /// Adds/removes PinToTop from the current user's "Run at startup" registry key,
    /// the same mechanism the Windows Settings > Startup Apps page reads from.
    /// </summary>
    internal static class StartupHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "PinToTop";

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    return key != null && key.GetValue(ValueName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return;

                    if (enabled)
                    {
                        string exePath = Application.ExecutablePath;
                        key.SetValue(ValueName, "\"" + exePath + "\"");
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not update the Windows startup setting:\n" + ex.Message,
                    "Pin To Top", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
