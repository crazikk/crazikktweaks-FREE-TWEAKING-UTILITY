using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace freetweaks_v1._3
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!IsAdministrator())
            {
                // Restart the application as administrator
                var processInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas" // This ensures the application is run as administrator
                };

                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start as administrator: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return; // Exit the current instance
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new panel_core());
        }

        static bool IsAdministrator()
        {
            // Check if the current user is an administrator
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
