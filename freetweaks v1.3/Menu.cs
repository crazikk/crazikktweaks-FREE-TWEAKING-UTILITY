using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;

namespace freetweaks_v1._3
{
    /// <summary>
    /// Main form (corePanel) for the crazikktweaks utility.
    /// Handles UI, toggle buttons, and panel navigation.
    /// </summary>
    public partial class panel_core : Form
    {
        private readonly Color _defaultBackColor = Color.FromArgb(13, 13, 12);
        private readonly Color _defaultForeColor = Color.White;
        private readonly Color _activeBackColor = Color.PowderBlue;
        private readonly Color _activeForeColor = Color.FromArgb(13, 13, 12);

        /// <summary>
        /// Prevents repeated toggling when loading initial settings from registry.
        /// </summary>
        private bool isInitializingSettings = false;

        /// <summary>
        /// Constructor: Initializes the form, hides unused panels, sets up the home panel as default.
        /// </summary>
        public panel_core()
        {
            InitializeComponent();
            username.Text = Environment.UserName;
            welcomeUsrLabel.Text = Environment.UserName;

            // Hide unnecessary panels
            windowsPanel.Hide();
            cpuPanel.Hide();
            gpuPanel.Hide();

            // Set "HomeBtn" as active button
            SetActiveButton(HomeBtn);
        }

        /// <summary>
        /// Closes the entire application (on button click).
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Shows the requested panel and hides the others.
        /// </summary>
        private void ShowPanel(Panel panelToShow)
        {
            gpuPanel.Visible = false;
            cpuPanel.Visible = false;
            windowsPanel.Visible = false;

            panelToShow.Visible = true;
        }

        /// <summary>
        /// Event handler for GPU button click.
        /// Shows the GPU panel and initializes settings.
        /// </summary>
        private void gpuBtn_Click(object sender, EventArgs e)
        {
            ShowPanel(gpuPanel);
            ResetAllSideButtons();
            SetActiveButton(gpuBtn);

            // Load registry states, including GPU toggles
            InitializeAllSettings();
        }

        /// <summary>
        /// Event handler for CPU button click.
        /// Shows the CPU panel and initializes settings.
        /// </summary>
        private void cpubtn_Click(object sender, EventArgs e)
        {
            ShowPanel(cpuPanel);
            ResetAllSideButtons();
            SetActiveButton(cpubtn);

            // Load registry states (Windows, CPU, GPU, etc.)
            InitializeAllSettings();
        }

        /// <summary>
        /// Event handler for Windows button click.
        /// Shows the Windows panel and initializes settings.
        /// </summary>
        private void WindowsBtn_Click(object sender, EventArgs e)
        {
            ShowPanel(windowsPanel);
            ResetAllSideButtons();
            SetActiveButton(WindowsBtn);

            InitializeAllSettings();
        }

        /// <summary>
        /// Event handler for Home button click.
        /// Hides other panels, shows the "home" view.
        /// </summary>
        private void HomeBtn_Click(object sender, EventArgs e)
        {
            windowsPanel.Hide();
            cpuPanel.Hide();
            gpuPanel.Hide();

            ResetAllSideButtons();
            SetActiveButton(HomeBtn);
        }

        /// <summary>
        /// Resets the appearance of all side buttons.
        /// </summary>
        private void ResetAllSideButtons()
        {
            ResetButtonAppearance(gpuBtn);
            ResetButtonAppearance(cpubtn);
            ResetButtonAppearance(WindowsBtn);
            ResetButtonAppearance(HomeBtn);
        }

        /// <summary>
        /// Restores a button to its default (non-active) appearance.
        /// </summary>
        private void ResetButtonAppearance(Button btn)
        {
            btn.BackColor = _defaultBackColor;
            btn.ForeColor = _defaultForeColor;
        }

        /// <summary>
        /// Sets a specific button to "active" appearance.
        /// </summary>
        private void SetActiveButton(Button btn)
        {
            btn.BackColor = _activeBackColor;
            btn.ForeColor = _activeForeColor;
        }

        /// <summary>
        /// Loads the registry states of all toggles (Windows, CPU, GPU, etc.).
        /// Also detects GPU brand (Nvidia vs AMD) and hides/shows relevant controls.
        /// </summary>
        private void InitializeAllSettings()
        {
            isInitializingSettings = true;
            try
            {
                // ----- Windows toggles -----
                diagnosticsBtn.Checked = SystemSettings.IsDiagnosticsEnabled();
                animationsBtn.Checked = SystemSettings.IsAnimationsEnabled();
                keyboardBtn.Checked = SystemSettings.IsKeyboardEnabled();
                mouseBtn.Checked = SystemSettings.IsMouseEnabled();
                reportingBtn.Checked = SystemSettings.IsReportingEnabled();
                bluetoothBtn.Checked = SystemSettings.IsBluetoothEnabled();
                bgappsBtn.Checked = SystemSettings.IsBgappsEnabled();
                transparencyBtn.Checked = SystemSettings.IsTransparencyEnabled();
                gamemodeBtn.Checked = SystemSettings.IsGamemodeEnabled();
                notificationsBtn.Checked = SystemSettings.IsNotificationSettingsEnabled();

                // ----- CPU toggles -----
                timersBtn.Checked = SystemSettings.IsTimersEnabled();
                cstatesBtn.Checked = SystemSettings.IsCStatesEnabled();
                eventProcessorBtn.Checked = SystemSettings.IsEventProcessorEnabled();
                fairShareBtn.Checked = SystemSettings.IsFairShareEnabled();
                coreParkingBtn.Checked = SystemSettings.IsCoreParkingEnabled();

                // ----- GPU toggles (Nvidia / AMD) -----
                energyDriverBtn.Checked = SystemSettings.IsEnergyDriverEnabled();
                telemetryBtn.Checked = SystemSettings.IsTelemetryEnabled();
                PreemptionBtn.Checked = SystemSettings.IsPreemptionEnabled();
                hdcpBtn.Checked = SystemSettings.IsHdcpEnabled();
                overlayBtn.Checked = SystemSettings.IsOverlayEnabled();
                ulpsBtn.Checked = SystemSettings.IsUlpsEnabled();
            }
            finally
            {
                isInitializingSettings = false;
            }

            // GPU detection
            bool isNvidia = SystemSettings.HasNvidiaGpu();
            bool isAmd = SystemSettings.HasAmdGpu();

            // Hide or show toggles for Nvidia
            energyDriverBtn.Visible = isNvidia;
            telemetryBtn.Visible = isNvidia;
            PreemptionBtn.Visible = isNvidia;
            energyDriverLabel.Visible = isNvidia;
            telemetryLabel.Visible = isNvidia;
            PreemptionLabel.Visible = isNvidia;

            // Hide or show toggles for AMD
            hdcpBtn.Visible = isAmd;
            overlayBtn.Visible = isAmd;
            ulpsBtn.Visible = isAmd;
            hdcpLabel.Visible = isAmd;
            ulpsLabel.Visible = isAmd;
            overlayLabel.Visible = isAmd;
        }

        // ===================================================================
        // =========== Handlers for older Windows toggles ====================
        // ===================================================================
        /// <summary>
        /// Handler for the Diagnostics toggle checkbox.
        /// </summary>
        private void diagnosticsBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = diagnosticsBtn.Checked;
            SystemSettings.SetDiagnosticsEnabled(desiredState);
            bool actualState = SystemSettings.IsDiagnosticsEnabled();
            if (actualState != desiredState)
            {
                diagnosticsBtn.CheckedChanged -= diagnosticsBtn_CheckedChanged;
                diagnosticsBtn.Checked = actualState;
                diagnosticsBtn.CheckedChanged += diagnosticsBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Diagnostics.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Animations toggle checkbox.
        /// </summary>
        private void animationsBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = animationsBtn.Checked;
            SystemSettings.SetAnimationsEnabled(desiredState);
            bool actualState = SystemSettings.IsAnimationsEnabled();
            if (actualState != desiredState)
            {
                animationsBtn.CheckedChanged -= animationsBtn_CheckedChanged;
                animationsBtn.Checked = actualState;
                animationsBtn.CheckedChanged += animationsBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Animations.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Keyboard toggle checkbox.
        /// </summary>
        private void keyboardBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = keyboardBtn.Checked;
            SystemSettings.SetKeyboardEnabled(desiredState);
            bool actualState = SystemSettings.IsKeyboardEnabled();
            if (actualState != desiredState)
            {
                keyboardBtn.CheckedChanged -= keyboardBtn_CheckedChanged;
                keyboardBtn.Checked = actualState;
                keyboardBtn.CheckedChanged += keyboardBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Keyboard.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Mouse toggle checkbox.
        /// </summary>
        private void mouseBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = mouseBtn.Checked;
            SystemSettings.SetMouseEnabled(desiredState);
            bool actualState = SystemSettings.IsMouseEnabled();
            if (actualState != desiredState)
            {
                mouseBtn.CheckedChanged -= mouseBtn_CheckedChanged;
                mouseBtn.Checked = actualState;
                mouseBtn.CheckedChanged += mouseBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Mouse.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Reporting toggle checkbox.
        /// </summary>
        private void reportingBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = reportingBtn.Checked;
            SystemSettings.SetReportingEnabled(desiredState);
            bool actualState = SystemSettings.IsReportingEnabled();
            if (actualState != desiredState)
            {
                reportingBtn.CheckedChanged -= reportingBtn_CheckedChanged;
                reportingBtn.Checked = actualState;
                reportingBtn.CheckedChanged += reportingBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Reporting.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Bluetooth toggle checkbox.
        /// </summary>
        private void bluetoothBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = bluetoothBtn.Checked;
            SystemSettings.SetBluetoothEnabled(desiredState);
            bool actualState = SystemSettings.IsBluetoothEnabled();
            if (actualState != desiredState)
            {
                bluetoothBtn.CheckedChanged -= bluetoothBtn_CheckedChanged;
                bluetoothBtn.Checked = actualState;
                bluetoothBtn.CheckedChanged += bluetoothBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Bluetooth.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Background Apps toggle checkbox.
        /// </summary>
        private void bgappsBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = bgappsBtn.Checked;
            SystemSettings.SetBgappsEnabled(desiredState);
            bool actualState = SystemSettings.IsBgappsEnabled();
            if (actualState != desiredState)
            {
                bgappsBtn.CheckedChanged -= bgappsBtn_CheckedChanged;
                bgappsBtn.Checked = actualState;
                bgappsBtn.CheckedChanged += bgappsBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Background Apps.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Transparency toggle checkbox.
        /// </summary>
        private void transparencyBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = transparencyBtn.Checked;
            SystemSettings.SetTransparencyEnabled(desiredState);
            bool actualState = SystemSettings.IsTransparencyEnabled();
            if (actualState != desiredState)
            {
                transparencyBtn.CheckedChanged -= transparencyBtn_CheckedChanged;
                transparencyBtn.Checked = actualState;
                transparencyBtn.CheckedChanged += transparencyBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Transparency.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Game Mode toggle checkbox.
        /// </summary>
        private void gamemodeBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = gamemodeBtn.Checked;
            SystemSettings.SetGamemodeEnabled(desiredState);
            bool actualState = SystemSettings.IsGamemodeEnabled();
            if (actualState != desiredState)
            {
                gamemodeBtn.CheckedChanged -= gamemodeBtn_CheckedChanged;
                gamemodeBtn.Checked = actualState;
                gamemodeBtn.CheckedChanged += gamemodeBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Game Mode.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Notifications toggle checkbox.
        /// </summary>
        private void notificationsBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = notificationsBtn.Checked;
            SystemSettings.SetNotificationSettingsEnabled(desiredState);
            bool actualState = SystemSettings.IsNotificationSettingsEnabled();
            if (actualState != desiredState)
            {
                notificationsBtn.CheckedChanged -= notificationsBtn_CheckedChanged;
                notificationsBtn.Checked = actualState;
                notificationsBtn.CheckedChanged += notificationsBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Notifications.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================================================================
        // ============= CPU toggles => CheckedChanged handlers =============
        // ===================================================================
        /// <summary>
        /// Handler for the Timers toggle checkbox.
        /// </summary>
        private void timersBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = timersBtn.Checked;
            SystemSettings.SetTimersEnabled(desiredState);
            bool actualState = SystemSettings.IsTimersEnabled();
            if (actualState != desiredState)
            {
                timersBtn.CheckedChanged -= timersBtn_CheckedChanged;
                timersBtn.Checked = actualState;
                timersBtn.CheckedChanged += timersBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Timers.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the C-States toggle checkbox.
        /// </summary>
        private void cstatesBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = cstatesBtn.Checked;
            SystemSettings.SetCStatesEnabled(desiredState);
            bool actualState = SystemSettings.IsCStatesEnabled();
            if (actualState != desiredState)
            {
                cstatesBtn.CheckedChanged -= cstatesBtn_CheckedChanged;
                cstatesBtn.Checked = actualState;
                cstatesBtn.CheckedChanged += cstatesBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for C-States.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the EventProcessor toggle checkbox.
        /// </summary>
        private void eventProcessorBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = eventProcessorBtn.Checked;
            SystemSettings.SetEventProcessorEnabled(desiredState);
            bool actualState = SystemSettings.IsEventProcessorEnabled();
            if (actualState != desiredState)
            {
                eventProcessorBtn.CheckedChanged -= eventProcessorBtn_CheckedChanged;
                eventProcessorBtn.Checked = actualState;
                eventProcessorBtn.CheckedChanged += eventProcessorBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for EventProcessor.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the FairShare toggle checkbox.
        /// </summary>
        private void fairShareBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = fairShareBtn.Checked;
            SystemSettings.SetFairShareEnabled(desiredState);
            bool actualState = SystemSettings.IsFairShareEnabled();
            if (actualState != desiredState)
            {
                fairShareBtn.CheckedChanged -= fairShareBtn_CheckedChanged;
                fairShareBtn.Checked = actualState;
                fairShareBtn.CheckedChanged += fairShareBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for FairShare.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the CoreParking toggle checkbox.
        /// </summary>
        private void coreParkingBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = coreParkingBtn.Checked;
            SystemSettings.SetCoreParkingEnabled(desiredState);
            bool actualState = SystemSettings.IsCoreParkingEnabled();
            if (actualState != desiredState)
            {
                coreParkingBtn.CheckedChanged -= coreParkingBtn_CheckedChanged;
                coreParkingBtn.Checked = actualState;
                coreParkingBtn.CheckedChanged += coreParkingBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for CoreParking.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================================================================
        // ===== GPU toggles (Nvidia/AMD) => CheckedChanged handlers =========
        // ===================================================================
        /// <summary>
        /// Handler for the Nvidia EnergyDriver toggle checkbox.
        /// </summary>
        private void energyDriverBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = energyDriverBtn.Checked;
            SystemSettings.SetEnergyDriverEnabled(desiredState);
            bool actualState = SystemSettings.IsEnergyDriverEnabled();
            if (actualState != desiredState)
            {
                energyDriverBtn.CheckedChanged -= energyDriverBtn_CheckedChanged;
                energyDriverBtn.Checked = actualState;
                energyDriverBtn.CheckedChanged += energyDriverBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for EnergyDriver.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Nvidia Telemetry toggle checkbox.
        /// </summary>
        private void telemetryBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = telemetryBtn.Checked;
            SystemSettings.SetTelemetryEnabled(desiredState);
            bool actualState = SystemSettings.IsTelemetryEnabled();
            if (actualState != desiredState)
            {
                telemetryBtn.CheckedChanged -= telemetryBtn_CheckedChanged;
                telemetryBtn.Checked = actualState;
                telemetryBtn.CheckedChanged += telemetryBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Telemetry.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Nvidia Preemption toggle checkbox.
        /// </summary>
        private void PreemptionBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = PreemptionBtn.Checked;
            SystemSettings.SetPreemptionEnabled(desiredState);
            bool actualState = SystemSettings.IsPreemptionEnabled();
            if (actualState != desiredState)
            {
                PreemptionBtn.CheckedChanged -= PreemptionBtn_CheckedChanged;
                PreemptionBtn.Checked = actualState;
                PreemptionBtn.CheckedChanged += PreemptionBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Preemption.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the AMD HDCP toggle checkbox.
        /// </summary>
        private void hdcpBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = hdcpBtn.Checked;
            SystemSettings.SetHdcpEnabled(desiredState);
            bool actualState = SystemSettings.IsHdcpEnabled();
            if (actualState != desiredState)
            {
                hdcpBtn.CheckedChanged -= hdcpBtn_CheckedChanged;
                hdcpBtn.Checked = actualState;
                hdcpBtn.CheckedChanged += hdcpBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for HDCP.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the AMD Overlay toggle checkbox.
        /// </summary>
        private void overlayBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = overlayBtn.Checked;
            SystemSettings.SetOverlayEnabled(desiredState);
            bool actualState = SystemSettings.IsOverlayEnabled();
            if (actualState != desiredState)
            {
                overlayBtn.CheckedChanged -= overlayBtn_CheckedChanged;
                overlayBtn.Checked = actualState;
                overlayBtn.CheckedChanged += overlayBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Overlay.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the AMD ULPS toggle checkbox.
        /// </summary>
        private void ulpsBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = ulpsBtn.Checked;
            SystemSettings.SetUlpsEnabled(desiredState);
            bool actualState = SystemSettings.IsUlpsEnabled();
            if (actualState != desiredState)
            {
                ulpsBtn.CheckedChanged -= ulpsBtn_CheckedChanged;
                ulpsBtn.Checked = actualState;
                ulpsBtn.CheckedChanged += ulpsBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for ULPS.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Example placeholder button (cuiButton2) - displays a "Coming soon" message.
        /// </summary>
        private void cuiButton2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Coming soon", "crazikktweaks",
                            MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        /// <summary>
        /// Creates a restore point (using PowerShell) on drive C:.
        /// Removes certain registry keys and sets frequency to 0.
        /// </summary>
        private void restorePointBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // 1) Enable Computer Restore on C:
                using (var pEnable = new System.Diagnostics.Process())
                {
                    pEnable.StartInfo.FileName = "powershell";
                    pEnable.StartInfo.Arguments = "-NoProfile -Command \"Enable-ComputerRestore -Drive 'C:'\"";
                    pEnable.StartInfo.Verb = "runas";
                    pEnable.StartInfo.CreateNoWindow = true;
                    pEnable.StartInfo.UseShellExecute = true;
                    pEnable.Start();
                    pEnable.WaitForExit();
                }

                // 2) Registry edits for SystemRestore
                using (var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(
                                         Microsoft.Win32.RegistryHive.LocalMachine,
                                         Microsoft.Win32.RegistryView.Registry64))
                {
                    using (var srKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", writable: true))
                    {
                        if (srKey != null)
                        {
                            srKey.DeleteValue("RPSessionInterval", false);
                            srKey.DeleteValue("DisableConfig", false);
                            srKey.SetValue("SystemRestorePointCreationFrequency", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                    }
                }

                // 3) Create restore point with description
                using (var pCheckpoint = new System.Diagnostics.Process())
                {
                    pCheckpoint.StartInfo.FileName = "powershell";
                    pCheckpoint.StartInfo.Arguments = "-NoProfile -Command \"Checkpoint-Computer -Description 'crazikktweaks FREE Utility RESTORE POINT'\"";
                    pCheckpoint.StartInfo.Verb = "runas";
                    pCheckpoint.StartInfo.CreateNoWindow = true;
                    pCheckpoint.StartInfo.UseShellExecute = true;
                    pCheckpoint.Start();
                    pCheckpoint.WaitForExit();
                }

                // 4) Inform user
                MessageBox.Show("Restore point created successfully.",
                                "crazikktweaks",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while creating restore point:\n" + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Opens a YouTube link with tutorial videos in the default browser.
        /// </summary>
        private void tutorialBtn_Click(object sender, EventArgs e)
        {
            string tutorialUrl = "https://www.youtube.com/@crazikktweaksUS";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tutorialUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Async handler for checking updates.
        /// Fetches JSON from a URL, compares versions, and optionally redirects user to Discord.
        /// </summary>
        private async void checkForUpdatesBtn_Click(object sender, EventArgs e)
        {
            string currentVersion = "1.3.0";

            // JSON endpoint with "latestVersion"
            string versionUrl = "https://crazikktweaks.com/free.json";

            // Discord link if there's a newer version
            string discordLink = "https://discord.gg/crazikktweaks";

            try
            {
                using (var client = new HttpClient())
                {
                    // Download JSON
                    string jsonString = await client.GetStringAsync(versionUrl);

                    // Parse JSON
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var versionData = JsonSerializer.Deserialize<LatestVerOnly>(jsonString, options);

                    if (versionData == null || string.IsNullOrEmpty(versionData.latestVersion))
                    {
                        MessageBox.Show("Unable to parse the latest version info from JSON.",
                                        "Check Updates",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                        return;
                    }

                    // Compare versions
                    if (IsNewerVersion(versionData.latestVersion, currentVersion))
                    {
                        // Newer version is available
                        DialogResult dr = MessageBox.Show(
                            $"A new version ({versionData.latestVersion}) is available!\nYour version: {currentVersion}\n\nDo you want to open the Discord link?",
                            "Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (dr == DialogResult.Yes)
                        {
                            // Open the Discord invite
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = discordLink,
                                UseShellExecute = true
                            });
                        }
                    }
                    else
                    {
                        // Same or newer locally
                        MessageBox.Show($"You are up to date!\nCurrent version: {currentVersion}",
                                        "Check Updates",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking for updates:\n" + ex.Message,
                                "Check Updates",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Simple class for deserializing JSON with "latestVersion".
        /// </summary>
        public class LatestVerOnly
        {
            public string latestVersion { get; set; }
        }

        /// <summary>
        /// Compares two version strings and returns true if onlineVersion is newer than localVersion.
        /// </summary>
        private bool IsNewerVersion(string onlineVersion, string localVersion)
        {
            try
            {
                Version verOnline = new Version(onlineVersion);
                Version verLocal = new Version(localVersion);
                return verOnline.CompareTo(verLocal) > 0;
            }
            catch
            {
                // If parse fails, assume no newer version.
                return false;
            }
        }

        /// <summary>
        /// Opens the main website of crazikktweaks in a default browser (logo/picture click).
        /// </summary>
        private void cuiPictureBox1_Click(object sender, EventArgs e)
        {
            string tutorialUrl = "https://www.crazikktweaks.com";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tutorialUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
    }
}
