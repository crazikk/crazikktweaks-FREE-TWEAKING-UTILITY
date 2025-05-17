using System;
using System.Drawing;
using System.Net.Http;
using System.Runtime.InteropServices;
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
            windowsPanel2.Hide();

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
            windowsPanel2.Visible = false;
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
            windowsPanel2.Hide();
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
                // ----- Windows toggles (staré už existující) -----
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

                // ----- CPU toggles (staré už existující) -----
                timersBtn.Checked = SystemSettings.IsTimersEnabled();
                cstatesBtn.Checked = SystemSettings.IsCStatesEnabled();
                eventProcessorBtn.Checked = SystemSettings.IsEventProcessorEnabled();
                fairShareBtn.Checked = SystemSettings.IsFairShareEnabled();
                coreParkingBtn.Checked = SystemSettings.IsCoreParkingEnabled();

                // ----- GPU toggles (staré už existující) -----
                energyDriverBtn.Checked = SystemSettings.IsEnergyDriverEnabled();
                telemetryBtn.Checked = SystemSettings.IsTelemetryEnabled();
                PreemptionBtn.Checked = SystemSettings.IsPreemptionEnabled();
                hdcpBtn.Checked = SystemSettings.IsHdcpEnabled();
                overlayBtn.Checked = SystemSettings.IsOverlayEnabled();
                ulpsBtn.Checked = SystemSettings.IsUlpsEnabled();
                findmydeviceBtn.CheckedChanged += findmydeviceBtn_CheckedChanged;
                settingsSyncingBtn.CheckedChanged += settingsSyncingBtn_CheckedChanged;

                // ----- NOVÉ TOGGLE: csrssPriorityBtn -----
                csrssPriorityBtn.Checked = SystemSettings.IsCsrssPriorityEnabled();

                // ----- NOVÉ TOGGLE: cortanaBtn -----
                cortanaBtn.Checked = SystemSettings.IsCortanaEnabled();

                // ----- NOVÉ TOGGLE: smartScreenBtn -----
                smartScreenBtn.Checked = SystemSettings.IsSmartScreenEnabled();

                // ----- NOVÉ TOGGLE: windowsInsiderBtn -----
                windowsInsiderBtn.Checked = SystemSettings.IsWindowsInsiderEnabled();

                // ----- NOVÉ TOGGLE: cuiSwitch2 (Biometrics) -----
                cuiSwitch2.Checked = SystemSettings.IsBiometricsEnabled();

                // ----- NEW: Browser Background Processes toggle -----
                cuiSwitch26.Checked = SystemSettings.IsBrowserBackgroundProcessesEnabled();

                // ----- NEW TOGGLE: Advertising Restriction (cuiSwitch20) -----
                cuiSwitch20.Checked = SystemSettings.IsAdvertisingEnabled();

                // ----- NEW TOGGLE: App Privacy Restriction (cuiSwitch25) -----
                cuiSwitch25.Checked = SystemSettings.IsAppPrivacyRestricted();

                // ----- NEW TOGGLE: Automatic Maintenance (cuiSwitch27) -----
                cuiSwitch27.Checked = SystemSettings.IsAutomaticMaintenanceEnabled();

                // ----- NEW TOGGLE: Xbox Game DVR -----
                gameDvrBtn.Checked = SystemSettings.IsGameDvrEnabled();

                // ----- NEW TOGGLE: SMB Session -----
                smbSessionBtn.Checked = SystemSettings.IsSmbSessionEnabled();

                // ----- NEW TOGGLE: Power Throttling -----
                powerThrottlingBtn.Checked = SystemSettings.IsPowerThrottlingEnabled();

                // ----- NEW TOGGLE: System Responsiveness -----
                systemResponsivenessBtn.Checked = SystemSettings.IsSystemResponsivenessEnabled();

                bool isFindMyDeviceDisabled = SystemSettings.IsFindMyDeviceDisabled();
                findmydeviceBtn.Checked = !isFindMyDeviceDisabled;

                bool isSettingsSyncingDisabled = SystemSettings.IsSettingsSyncingDisabled();
                settingsSyncingBtn.Checked = !isSettingsSyncingDisabled;
            }
            finally
            {
                isInitializingSettings = false;
            }

            // GPU detection (pro staré GPU volby)
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

        // ===================================================================
        // =========== NOVÉ HANDLERY pro nová tlačítka/přepínače =============
        // ===================================================================

        /// <summary>
        /// Handler pro zvýšenou prioritu csrss.exe (CpuPriority + IoPriority).
        /// </summary>
        private void csrssPriorityBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = csrssPriorityBtn.Checked;
            SystemSettings.SetCsrssPriorityEnabled(desiredState);
            bool actualState = SystemSettings.IsCsrssPriorityEnabled();
            if (actualState != desiredState)
            {
                csrssPriorityBtn.CheckedChanged -= csrssPriorityBtn_CheckedChanged;
                csrssPriorityBtn.Checked = actualState;
                csrssPriorityBtn.CheckedChanged += csrssPriorityBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for csrss.exe priority.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler pro Cortanu (AllowCortana atd.).
        /// </summary>
        private void cortanaBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = cortanaBtn.Checked;
            SystemSettings.SetCortanaEnabled(desiredState);
            bool actualState = SystemSettings.IsCortanaEnabled();
            if (actualState != desiredState)
            {
                cortanaBtn.CheckedChanged -= cortanaBtn_CheckedChanged;
                cortanaBtn.Checked = actualState;
                cortanaBtn.CheckedChanged += cortanaBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Cortana.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler pro Windows SmartScreen.
        /// </summary>
        private void smartScreenBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = smartScreenBtn.Checked;
            SystemSettings.SetSmartScreenEnabled(desiredState);
            bool actualState = SystemSettings.IsSmartScreenEnabled();
            if (actualState != desiredState)
            {
                smartScreenBtn.CheckedChanged -= smartScreenBtn_CheckedChanged;
                smartScreenBtn.Checked = actualState;
                smartScreenBtn.CheckedChanged += smartScreenBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for SmartScreen.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler pro Windows Insider (AllowExperimentation).
        /// </summary>
        private void windowsInsiderBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = windowsInsiderBtn.Checked;
            SystemSettings.SetWindowsInsiderEnabled(desiredState);
            bool actualState = SystemSettings.IsWindowsInsiderEnabled();
            if (actualState != desiredState)
            {
                windowsInsiderBtn.CheckedChanged -= windowsInsiderBtn_CheckedChanged;
                windowsInsiderBtn.Checked = actualState;
                windowsInsiderBtn.CheckedChanged += windowsInsiderBtn_CheckedChanged;

                MessageBox.Show("Failed to change setting for Windows Insider.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler pro Biometrics (cuiSwitch2).
        /// </summary>
        private void cuiSwitch2_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = cuiSwitch2.Checked;
            SystemSettings.SetBiometricsEnabled(desiredState);
            bool actualState = SystemSettings.IsBiometricsEnabled();
            if (actualState != desiredState)
            {
                cuiSwitch2.CheckedChanged -= cuiSwitch2_CheckedChanged;
                cuiSwitch2.Checked = actualState;
                cuiSwitch2.CheckedChanged += cuiSwitch2_CheckedChanged;

                MessageBox.Show("Failed to change setting for Biometrics.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================================================================
        // ============= Ostatní stávající funkce a tlačítka =================
        // ===================================================================

        /// <summary>
        /// Example placeholder button (cuiButton2) - displays a "Coming soon" message.
        /// </summary>
        private void cuiButton2_Click(object sender, EventArgs e)
        {
            windowsPanel2.Show();
            windowsPanel.Hide();
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
            string currentVersion = "1.7.0";

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

        // ===================================================================
        // =========== NEW CheckedChanged handler for Browser Background Processes =====
        private void cuiSwitch26_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = cuiSwitch26.Checked;
            SystemSettings.SetBrowserBackgroundProcessesEnabled(desiredState);
            bool actualState = SystemSettings.IsBrowserBackgroundProcessesEnabled();
            if (actualState != desiredState)
            {
                cuiSwitch26.CheckedChanged -= cuiSwitch26_CheckedChanged;
                cuiSwitch26.Checked = actualState;
                cuiSwitch26.CheckedChanged += cuiSwitch26_CheckedChanged;
                MessageBox.Show("Failed to change setting for Browser Background Processes.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // <summary>
        /// Handler for the Advertising toggle (cuiSwitch20).
        /// When checked, advertising is restricted (DisabledByGroupPolicy=1); when unchecked, enabled.
        /// </summary>
        private void cuiSwitch20_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = cuiSwitch20.Checked; // true = advertising enabled
            SystemSettings.SetAdvertisingEnabled(desiredState);
            bool actualState = SystemSettings.IsAdvertisingEnabled();
            if (actualState != desiredState)
            {
                cuiSwitch20.CheckedChanged -= cuiSwitch20_CheckedChanged;
                cuiSwitch20.Checked = actualState;
                cuiSwitch20.CheckedChanged += cuiSwitch20_CheckedChanged;
                MessageBox.Show("Failed to change setting for Advertising.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void cuiSwitch25_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = cuiSwitch25.Checked; // true means NOT restricted
            SystemSettings.SetAppPrivacyRestricted(desiredState);
            bool actualState = SystemSettings.IsAppPrivacyRestricted();
            if (actualState != desiredState)
            {
                cuiSwitch25.CheckedChanged -= cuiSwitch25_CheckedChanged;
                cuiSwitch25.Checked = actualState;
                cuiSwitch25.CheckedChanged += cuiSwitch25_CheckedChanged;
                MessageBox.Show("Failed to change setting for App Privacy Restriction.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handler for the Automatic Maintenance toggle (cuiSwitch27).
        /// When checked, Automatic Maintenance is enabled (MaintenanceDisabled = 0);
        /// when unchecked, it is disabled (MaintenanceDisabled = 1).
        /// </summary>
        private void cuiSwitch27_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = cuiSwitch27.Checked; // true = enabled
            SystemSettings.SetAutomaticMaintenanceEnabled(desiredState);
            bool actualState = SystemSettings.IsAutomaticMaintenanceEnabled();
            if (actualState != desiredState)
            {
                cuiSwitch27.CheckedChanged -= cuiSwitch27_CheckedChanged;
                cuiSwitch27.Checked = actualState;
                cuiSwitch27.CheckedChanged += cuiSwitch27_CheckedChanged;
                MessageBox.Show("Failed to change setting for Automatic Maintenance.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void cuiButton1_Click(object sender, EventArgs e)
        {

        }

        private void cuiButton2_Click_1(object sender, EventArgs e)
        {
            windowsPanel.Show();
            windowsPanel2.Hide();
        }

        private void findmydeviceBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings)
                return;

            bool desiredState = findmydeviceBtn.Checked;
            SystemSettings.SetFindMyDeviceDisabled(!desiredState);

            bool actualState = !SystemSettings.IsFindMyDeviceDisabled();
            if (actualState != desiredState)
            {
                findmydeviceBtn.CheckedChanged -= findmydeviceBtn_CheckedChanged;
                findmydeviceBtn.Checked = actualState;
                findmydeviceBtn.CheckedChanged += findmydeviceBtn_CheckedChanged;

                MessageBox.Show("Failed to apply Find My Device settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void settingsSyncingBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings)
                return;

            bool desiredState = settingsSyncingBtn.Checked;
            SystemSettings.SetSettingsSyncingDisabled(!desiredState);

            bool actualState = !SystemSettings.IsSettingsSyncingDisabled();
            if (actualState != desiredState)
            {
                settingsSyncingBtn.CheckedChanged -= settingsSyncingBtn_CheckedChanged;
                settingsSyncingBtn.Checked = actualState;
                settingsSyncingBtn.CheckedChanged += settingsSyncingBtn_CheckedChanged;

                MessageBox.Show("Failed to apply Settings Syncing settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void gameDvrBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = gameDvrBtn.Checked;
            SystemSettings.SetGameDvrEnabled(desiredState);
            bool actualState = SystemSettings.IsGameDvrEnabled();
            if (actualState != desiredState)
            {
                gameDvrBtn.CheckedChanged -= gameDvrBtn_CheckedChanged;
                gameDvrBtn.Checked = actualState;
                gameDvrBtn.CheckedChanged += gameDvrBtn_CheckedChanged;
                MessageBox.Show("Failed to change setting for Xbox Game DVR.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void smbSessionBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = smbSessionBtn.Checked;
            SystemSettings.SetSmbSessionEnabled(desiredState);
            bool actualState = SystemSettings.IsSmbSessionEnabled();
            if (actualState != desiredState)
            {
                smbSessionBtn.CheckedChanged -= smbSessionBtn_CheckedChanged;
                smbSessionBtn.Checked = actualState;
                smbSessionBtn.CheckedChanged += smbSessionBtn_CheckedChanged;
                MessageBox.Show("Failed to change setting for SMB Session.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void powerThrottlingBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = powerThrottlingBtn.Checked;
            SystemSettings.SetPowerThrottlingEnabled(desiredState);
            bool actualState = SystemSettings.IsPowerThrottlingEnabled();
            if (actualState != desiredState)
            {
                powerThrottlingBtn.CheckedChanged -= powerThrottlingBtn_CheckedChanged;
                powerThrottlingBtn.Checked = actualState;
                powerThrottlingBtn.CheckedChanged += powerThrottlingBtn_CheckedChanged;
                MessageBox.Show("Failed to change setting for Power Throttling.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void systemResponsivenessBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingSettings) return;
            bool desiredState = systemResponsivenessBtn.Checked;
            SystemSettings.SetSystemResponsivenessEnabled(desiredState);
            bool actualState = SystemSettings.IsSystemResponsivenessEnabled();
            if (actualState != desiredState)
            {
                systemResponsivenessBtn.CheckedChanged -= systemResponsivenessBtn_CheckedChanged;
                systemResponsivenessBtn.Checked = actualState;
                systemResponsivenessBtn.CheckedChanged += systemResponsivenessBtn_CheckedChanged;
                MessageBox.Show("Failed to change setting for System Responsiveness.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cuiButton3_Click(object sender, EventArgs e)
        {
            SystemSettings.ClearWindowsTemp();
        }

        private void cuiButton4_Click(object sender, EventArgs e)
        {
            RecycleBinManager.ClearRecycleBin();
            MessageBox.Show("Recycle bin has been sucessfully cleared.", "crazikktweaks", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static class RecycleBinManager
        {
            [DllImport("Shell32.dll")]
            private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

            [Flags]
            private enum RecycleFlags : uint
            {
                SHERB_NOCONFIRMATION = 0x00000001,
                SHERB_NOPROGRESSUI = 0x00000002,
                SHERB_NOSOUND = 0x00000004
            }

            public static void ClearRecycleBin()
            {
                try
                {
                    // Set the Recycle Bin path to null to clear all bins
                    const string recycleBinPath = null;

                    // Call the SHEmptyRecycleBin function with appropriate flags
                    SHEmptyRecycleBin(IntPtr.Zero, recycleBinPath, RecycleFlags.SHERB_NOCONFIRMATION | RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }


    }
}
