using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal; // pro práci s SID
using System.Threading.Tasks;
using System.Windows.Forms;

namespace freetweaks_v1._3
{
    /// <summary>
    /// Class that handles reading/writing system (registry) settings 
    /// for the "crazikktweaks" utility.
    /// Provides toggles for Windows, CPU, GPU (Nvidia/AMD), etc.
    /// </summary>
    public static class SystemSettings
    {
        private const string StartValueName = "Start";

        /// <summary>
        /// Represents a single registry-based setting: 
        /// which hive, path, value name, the data for enabled/disabled, etc.
        /// </summary>
        private class RegistrySetting
        {
            public RegistryHive Hive { get; set; }
            public string RegistryPath { get; set; }
            public string ValueName { get; set; }
            public object DisabledValue { get; set; }
            public object EnabledValue { get; set; }
            public object DefaultValue { get; set; }
            public RegistryValueKind ValueKind { get; set; }
        }

        // -------------------------------------------------------------------
        // ------------------- Helpers for registry I/O ----------------------
        // -------------------------------------------------------------------

        /// <summary>
        /// Reads a registry value from the specified hive/path/name.
        /// If the key does not exist, it attempts to create it with <paramref name="defaultValue"/>.
        /// Returns the read value, or <paramref name="defaultValue"/> on failure.
        /// </summary>
        private static object ReadRegistryValue(RegistryHive hive,
                                                string path,
                                                string name,
                                                object defaultValue,
                                                RegistryValueKind kind)
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(path, writable: false))
                    {
                        if (key != null)
                        {
                            object value = key.GetValue(name);
                            if (value != null) return value;
                        }
                    }
                    // If the key does not exist, try to create it with defaultValue
                    using (RegistryKey createdKey = baseKey.CreateSubKey(path))
                    {
                        if (createdKey != null)
                        {
                            createdKey.SetValue(name, defaultValue, kind);
                            return defaultValue;
                        }
                        else
                        {
                            MessageBox.Show($"Failed to create registry key: {path}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading registry value {path}\\{name}: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
            return defaultValue;
        }

        /// <summary>
        /// Writes a registry value to the specified hive/path/name with the provided kind.
        /// If the key does not exist, it attempts to create it.
        /// </summary>
        private static void WriteRegistryValue(RegistryHive hive,
                                               string path,
                                               string name,
                                               object value,
                                               RegistryValueKind kind)
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(path, writable: true) ?? baseKey.CreateSubKey(path))
                    {
                        if (key != null)
                        {
                            key.SetValue(name, value, kind);
                        }
                        else
                        {
                            MessageBox.Show($"Failed to open or create registry key: {path}",
                                            "Error",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Administrator privileges are required to change registry settings.",
                                "Access Denied",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing registry value {path}\\{name}: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Tries to read a registry value of type <typeparamref name="T"/> from the specified path/name.
        /// If reading fails or parse fails, returns false. On success, outputs the value in <paramref name="value"/>.
        /// </summary>
        private static bool TryReadRegistryValue<T>(RegistryHive hive,
                                                    string path,
                                                    string name,
                                                    out T value,
                                                    T defaultValue)
        {
            value = defaultValue;
            try
            {
                // Decide the RegistryValueKind based on T
                RegistryValueKind valKind = (typeof(T) == typeof(string))
                    ? RegistryValueKind.String
                    : RegistryValueKind.DWord;

                object obj = ReadRegistryValue(hive, path, name, defaultValue, valKind);

                if (obj is T t)
                {
                    value = t;
                    return true;
                }
                else if (typeof(T) == typeof(int) && obj is string s && int.TryParse(s, out int parsed))
                {
                    value = (T)(object)parsed;
                    return true;
                }
                else if (typeof(T) == typeof(string) && obj != null)
                {
                    value = (T)obj;
                    return true;
                }
            }
            catch
            {
                // Already handled above
            }
            return false;
        }

        // -------------------------------------------------------------------
        // -------------- Helpers for services (Start = 2/3/4) ---------------
        // -------------------------------------------------------------------

        /// <summary>
        /// Writes the "Start" value for a given registry path to manage a Windows service start type.
        /// 2 = Automatic, 3 = Manual, 4 = Disabled
        /// </summary>
        private static void SetServiceStart(string registryPath, int startValue)
        {
            WriteRegistryValue(RegistryHive.LocalMachine,
                               registryPath,
                               StartValueName,
                               startValue,
                               RegistryValueKind.DWord);
        }

        /// <summary>
        /// Checks if a service is considered "enabled" by verifying its "Start" value is not 4.
        /// </summary>
        private static bool IsServiceEnabled(string registryPath)
        {
            // 4 = Disabled
            // 2 = Auto, 3 = Manual => consider it Enabled
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, registryPath, StartValueName, out int startValue, 2))
            {
                return (startValue != 4);
            }
            return false;
        }

        /// <summary>
        /// Checks if the "nvlddmkm" service key exists, implying an NVIDIA driver is present.
        /// </summary>
        public static bool HasNvidiaGpu()
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey nvKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm", false))
                {
                    return (nvKey != null);
                }
            }
        }

        /// <summary>
        /// Checks if the "amdkmdag" service key exists, implying an AMD driver is present.
        /// </summary>
        public static bool HasAmdGpu()
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey amdKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\amdkmdag", false))
                {
                    return (amdKey != null);
                }
            }
        }

        // -------------------------------------------------------------------
        // -------------- Original Windows / system settings -----------------
        // -------------------------------------------------------------------

        // 1) Diagnostics
        private static readonly string DiagTrackRegPath = @"SYSTEM\CurrentControlSet\Services\DiagTrack";
        private static readonly string DmwappushRegPath = @"SYSTEM\CurrentControlSet\Services\dmwappushservice";

        public static bool IsDiagnosticsEnabled()
        {
            bool diagOn = IsServiceEnabled(DiagTrackRegPath);
            bool dmwappushOn = IsServiceEnabled(DmwappushRegPath);
            return (diagOn || dmwappushOn);
        }

        public static void SetDiagnosticsEnabled(bool enabled)
        {
            int sv = enabled ? 3 : 4; // 3=Manual, 4=Disabled
            SetServiceStart(DiagTrackRegPath, sv);
            SetServiceStart(DmwappushRegPath, sv);
        }

        // 2) Animations
        private static readonly List<RegistrySetting> AnimationsSettings = new List<RegistrySetting>
        {
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DWM",
                ValueName = "DisallowAnimations",
                DisabledValue = 1,
                EnabledValue = 0,
                DefaultValue = 0,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.CurrentUser,
                RegistryPath = @"Control Panel\Desktop\WindowMetrics",
                ValueName = "MinAnimate",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.CurrentUser,
                RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                ValueName = "TaskbarAnimations",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.CurrentUser,
                RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                ValueName = "VisualFXSetting",
                DisabledValue = 3,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            }
        };

        public static bool IsAnimationsEnabled()
        {
            foreach (var setting in AnimationsSettings)
            {
                object val = ReadRegistryValue(setting.Hive,
                                               setting.RegistryPath,
                                               setting.ValueName,
                                               setting.DefaultValue,
                                               setting.ValueKind);
                if (!val.Equals(setting.EnabledValue))
                {
                    return false;
                }
            }
            return true;
        }

        public static void SetAnimationsEnabled(bool enabled)
        {
            foreach (var setting in AnimationsSettings)
            {
                object v = enabled ? setting.EnabledValue : setting.DisabledValue;
                WriteRegistryValue(setting.Hive,
                                   setting.RegistryPath,
                                   setting.ValueName,
                                   v,
                                   setting.ValueKind);
            }
        }

        // 3) Keyboard
        private const string KeyboardDelayRegistryPath = @"SYSTEM\CurrentControlSet\services\kbdclass\Parameters";
        private const string KeyboardDataQueueSizeValueName = "KeyboardDataQueueSize";
        private const int KeyboardDataQueueSizeEnabled = 40;
        private const int KeyboardDataQueueSizeDisabled = 100;

        public static bool IsKeyboardEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine,
                                          KeyboardDelayRegistryPath,
                                          KeyboardDataQueueSizeValueName,
                                          out int val,
                                          KeyboardDataQueueSizeDisabled))
            {
                return (val == KeyboardDataQueueSizeEnabled);
            }
            return false;
        }

        public static void SetKeyboardEnabled(bool enabled)
        {
            int v = enabled ? KeyboardDataQueueSizeEnabled : KeyboardDataQueueSizeDisabled;
            WriteRegistryValue(RegistryHive.LocalMachine,
                               KeyboardDelayRegistryPath,
                               KeyboardDataQueueSizeValueName,
                               v,
                               RegistryValueKind.DWord);
        }

        // 4) Mouse
        private const string MouseDelayRegistryPath = @"SYSTEM\CurrentControlSet\services\mouclass\Parameters";
        private const string MouseDataQueueSizeValueName = "MouseDataQueueSize";
        private const int MouseDataQueueSizeEnabled = 45;
        private const int MouseDataQueueSizeDisabled = 100;

        public static bool IsMouseEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine,
                                          MouseDelayRegistryPath,
                                          MouseDataQueueSizeValueName,
                                          out int val,
                                          MouseDataQueueSizeDisabled))
            {
                return (val == MouseDataQueueSizeEnabled);
            }
            return false;
        }

        public static void SetMouseEnabled(bool enabled)
        {
            int v = enabled ? MouseDataQueueSizeEnabled : MouseDataQueueSizeDisabled;
            WriteRegistryValue(RegistryHive.LocalMachine,
                               MouseDelayRegistryPath,
                               MouseDataQueueSizeValueName,
                               v,
                               RegistryValueKind.DWord);
        }

        // 5) Reporting (WER)
        private static readonly string WERPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting";
        private static readonly string WERPolicyPath2 = @"SOFTWARE\Policies\Microsoft\PCHealth\ErrorReporting";
        private static readonly string WERNonPolicyPath = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting";

        public static bool IsReportingEnabled()
        {
            TryReadRegistryValue<int>(RegistryHive.LocalMachine, WERPolicyPath, "Disabled", out int disabledVal, 0);
            return (disabledVal == 0);
        }

        public static void SetReportingEnabled(bool enabled)
        {
            int valDisabled = enabled ? 0 : 1;
            int valDoReport = enabled ? 1 : 0;
            int valLogging = enabled ? 0 : 1;

            WriteRegistryValue(RegistryHive.LocalMachine, WERPolicyPath, "Disabled", valDisabled, RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine, WERPolicyPath, "DoReport", valDoReport, RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine, WERPolicyPath, "LoggingDisabled", valLogging, RegistryValueKind.DWord);

            WriteRegistryValue(RegistryHive.LocalMachine, WERPolicyPath2, "DoReport", valDoReport, RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine, WERNonPolicyPath, "Disabled", valDisabled, RegistryValueKind.DWord);
        }

        // 6) Bluetooth
        private static readonly string[] BluetoothServices = new[]
        {
            @"SYSTEM\ControlSet001\Services\BTAGService",
            @"SYSTEM\ControlSet001\Services\bthserv",
            @"SYSTEM\ControlSet001\Services\BthAvctpSvc",
            @"SYSTEM\ControlSet001\Services\BluetoothUserService"
        };

        public static bool IsBluetoothEnabled()
        {
            foreach (var svcPath in BluetoothServices)
            {
                if (IsServiceEnabled(svcPath)) return true;
            }
            return false;
        }

        public static void SetBluetoothEnabled(bool enabled)
        {
            int startVal = enabled ? 3 : 4; // 3=Manual, 4=Disabled
            foreach (var svcPath in BluetoothServices)
            {
                SetServiceStart(svcPath, startVal);
            }
        }

        // 7) Background Apps (bam, dam)
        private static readonly string BamServicePath = @"SYSTEM\CurrentControlSet\Services\bam";
        private static readonly string DamServicePath = @"SYSTEM\CurrentControlSet\Services\dam";

        public static bool IsBgappsEnabled()
        {
            bool bamOn = IsServiceEnabled(BamServicePath);
            bool damOn = IsServiceEnabled(DamServicePath);
            return (bamOn || damOn);
        }

        public static void SetBgappsEnabled(bool enabled)
        {
            int sv = enabled ? 3 : 4;
            SetServiceStart(BamServicePath, sv);
            SetServiceStart(DamServicePath, sv);
        }

        // 8) Transparency
        private const string TransparencyRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string TransparencyValueName = "EnableTransparency";

        public static bool IsTransparencyEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.CurrentUser, TransparencyRegistryPath, TransparencyValueName, out int val, 1))
            {
                return (val == 1);
            }
            return false;
        }

        public static void SetTransparencyEnabled(bool enabled)
        {
            WriteRegistryValue(RegistryHive.CurrentUser,
                               TransparencyRegistryPath,
                               TransparencyValueName,
                               enabled ? 1 : 0,
                               RegistryValueKind.DWord);
        }

        // 9) Game Mode
        private const string GameBarPath = @"SOFTWARE\Microsoft\GameBar";

        public static bool IsGamemodeEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.CurrentUser, GameBarPath, "AllowAutoGameMode", out int val, 0))
            {
                return (val == 1);
            }
            return false;
        }

        public static void SetGamemodeEnabled(bool enabled)
        {
            int v = enabled ? 1 : 0;
            WriteRegistryValue(RegistryHive.CurrentUser, GameBarPath, "AllowAutoGameMode", v, RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.CurrentUser, GameBarPath, "AutoGameModeEnabled", v, RegistryValueKind.DWord);
        }

        // 10) Notifications
        private static readonly List<RegistrySetting> NotificationSettings = new List<RegistrySetting>
        {
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications",
                ValueName = "ToastEnabled",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Explorer",
                ValueName = "DisableNotificationCenter",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 0,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.CurrentUser,
                RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener",
                ValueName = "Value",
                DisabledValue = "Deny",
                EnabledValue = "Allow",
                DefaultValue = "Allow",
                ValueKind = RegistryValueKind.String
            }
        };

        public static bool IsNotificationSettingsEnabled()
        {
            foreach (var setting in NotificationSettings)
            {
                object curVal = ReadRegistryValue(
                    setting.Hive,
                    setting.RegistryPath,
                    setting.ValueName,
                    setting.DefaultValue,
                    setting.ValueKind);

                if (setting.ValueKind == RegistryValueKind.String)
                {
                    // Compare strings
                    string currentStr = curVal as string;
                    string enabledStr = setting.EnabledValue as string;
                    if (!string.Equals(currentStr, enabledStr, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                else
                {
                    // Compare as DWord
                    if (!curVal.Equals(setting.EnabledValue))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static void SetNotificationSettingsEnabled(bool enabled)
        {
            foreach (var setting in NotificationSettings)
            {
                object valToSet = enabled ? setting.EnabledValue : setting.DisabledValue;
                WriteRegistryValue(setting.Hive, setting.RegistryPath, setting.ValueName, valToSet, setting.ValueKind);
            }
        }

        // -------------------------------------------------------------------
        // --------------------- Additional CPU methods ----------------------
        // -------------------------------------------------------------------

        // 1) timersBtn
        private const string TimersRegistryPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel";
        private const string TimersValueName = "DistributeTimers";

        public static bool IsTimersEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, TimersRegistryPath, TimersValueName, out int val, 0))
            {
                return (val == 0);
            }
            return false;
        }

        public static void SetTimersEnabled(bool enabled)
        {
            int v = enabled ? 0 : 1;
            WriteRegistryValue(RegistryHive.LocalMachine, TimersRegistryPath, TimersValueName, v, RegistryValueKind.DWord);
        }

        // 2) cstatesBtn
        private static readonly List<RegistrySetting> CStatesSettings = new List<RegistrySetting>
        {
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings",
                ValueName = "AwayModeEnabled",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings",
                ValueName = "AllowStandby",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings",
                ValueName = "AllowHybridSleep",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            // CPU Throttle Min
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c",
                ValueName = "ValueMin",
                DisabledValue = 100,
                EnabledValue = 0,
                DefaultValue = 0,
                ValueKind = RegistryValueKind.DWord
            }
        };

        public static bool IsCStatesEnabled()
        {
            foreach (var setting in CStatesSettings)
            {
                object curVal = ReadRegistryValue(setting.Hive,
                                                  setting.RegistryPath,
                                                  setting.ValueName,
                                                  setting.DefaultValue,
                                                  setting.ValueKind);

                if (!curVal.Equals(setting.EnabledValue))
                {
                    return false;
                }
            }
            return true;
        }

        public static void SetCStatesEnabled(bool enabled)
        {
            foreach (var setting in CStatesSettings)
            {
                object valToSet = enabled ? setting.EnabledValue : setting.DisabledValue;
                WriteRegistryValue(setting.Hive,
                                   setting.RegistryPath,
                                   setting.ValueName,
                                   valToSet,
                                   setting.ValueKind);
            }
        }

        // 3) eventProcessorBtn
        private const string EventProcessorRegistryPath = @"SYSTEM\CurrentControlSet\Control\Power";
        private const string EventProcessorValueName = "EventProcessorEnabled";

        public static bool IsEventProcessorEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, EventProcessorRegistryPath, EventProcessorValueName, out int val, 1))
            {
                return (val == 1);
            }
            return false;
        }

        public static void SetEventProcessorEnabled(bool enabled)
        {
            int v = enabled ? 1 : 0;
            WriteRegistryValue(RegistryHive.LocalMachine, EventProcessorRegistryPath, EventProcessorValueName, v, RegistryValueKind.DWord);
        }

        // 4) fairShareBtn
        private const string FairShareRegistryPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Quota System";
        private const string FairShareValueName = "EnableCpuQuota";

        public static bool IsFairShareEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, FairShareRegistryPath, FairShareValueName, out int val, 1))
            {
                return (val == 1);
            }
            return false;
        }

        public static void SetFairShareEnabled(bool enabled)
        {
            int v = enabled ? 1 : 0;
            WriteRegistryValue(RegistryHive.LocalMachine, FairShareRegistryPath, FairShareValueName, v, RegistryValueKind.DWord);
        }

        // 5) coreParkingBtn
        private const string CoreParkingRegistryPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583";
        private const string CoreParkingValueName = "ValueMin";

        public static bool IsCoreParkingEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, CoreParkingRegistryPath, CoreParkingValueName, out int val, 0))
            {
                return (val == 0);
            }
            return false;
        }

        public static void SetCoreParkingEnabled(bool enabled)
        {
            int v = enabled ? 0 : 100;
            WriteRegistryValue(RegistryHive.LocalMachine, CoreParkingRegistryPath, CoreParkingValueName, v, RegistryValueKind.DWord);
        }

        // -------------------------------------------------------------------
        // ------------- Additional GPU-related methods (Nvidia/AMD) --------
        // -------------------------------------------------------------------

        // A) energyDriverBtn
        private static readonly string GpuEnergyDrvPath = @"SYSTEM\CurrentControlSet\Services\GpuEnergyDrv";
        private static readonly string GpuEnergyDrPath = @"SYSTEM\CurrentControlSet\Services\GpuEnergyDr";

        public static bool IsEnergyDriverEnabled()
        {
            bool drv1 = IsServiceEnabled(GpuEnergyDrvPath);
            bool drv2 = IsServiceEnabled(GpuEnergyDrPath);
            return (drv1 || drv2);
        }

        public static void SetEnergyDriverEnabled(bool enabled)
        {
            int startVal = enabled ? 2 : 4; // 2=Automatic, 4=Disabled
            SetServiceStart(GpuEnergyDrvPath, startVal);
            SetServiceStart(GpuEnergyDrPath, startVal);
        }

        // B) telemetryBtn
        private const string NvRunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string NvRunValueName = "NvBackend";
        private const string NvBackendDefaultPath = "\"C:\\Program Files (x86)\\NVIDIA Corporation\\Update Core\\NvBackend.exe\" /start";

        private static readonly List<RegistrySetting> TelemetryRidSettings = new List<RegistrySetting>
        {
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\NVIDIA Corporation\Global\FTS",
                ValueName = "EnableRID66610",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\NVIDIA Corporation\Global\FTS",
                ValueName = "EnableRID64640",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\NVIDIA Corporation\Global\FTS",
                ValueName = "EnableRID44231",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            }
        };

        public static bool IsTelemetryEnabled()
        {
            // 1) Check if "NvBackend" is in RUN
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey runKey = baseKey.OpenSubKey(NvRunKeyPath, false))
            {
                if (runKey?.GetValue(NvRunValueName) == null)
                    return false;
            }

            // 2) Check the "EnableRIDxxx" (all must be 1)
            foreach (var setting in TelemetryRidSettings)
            {
                object curVal = ReadRegistryValue(setting.Hive,
                                                  setting.RegistryPath,
                                                  setting.ValueName,
                                                  setting.DefaultValue,
                                                  setting.ValueKind);

                if (!curVal.Equals(setting.EnabledValue))
                {
                    return false;
                }
            }
            return true;
        }

        public static void SetTelemetryEnabled(bool enabled)
        {
            // 1) Toggle "NvBackend" in RUN
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey runKey = baseKey.OpenSubKey(NvRunKeyPath, writable: true))
            {
                if (runKey != null)
                {
                    if (!enabled)
                    {
                        try { runKey.DeleteValue(NvRunValueName); } catch { }
                    }
                    else
                    {
                        runKey.SetValue(NvRunValueName, NvBackendDefaultPath, RegistryValueKind.String);
                    }
                }
            }

            // 2) Toggle "EnableRIDxxxx"
            foreach (var setting in TelemetryRidSettings)
            {
                object valToSet = enabled ? setting.EnabledValue : setting.DisabledValue;
                WriteRegistryValue(setting.Hive,
                                   setting.RegistryPath,
                                   setting.ValueName,
                                   valToSet,
                                   setting.ValueKind);
            }
        }

        // C) PreemptionBtn
        private const string NvidiaClass0000 = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000";
        private const string NvlddmkmPath = @"SYSTEM\CurrentControlSet\Services\nvlddmkm";

        private static readonly Dictionary<string, object> PreemptionDisabledValues_Class0000 = new Dictionary<string, object>
        {
            { "DisableCudaContextPreemption", 1 },
            { "GPUPreemptionLevel", 0 },
            { "ComputePreemption", 0 },
        };

        private static readonly Dictionary<string, object> PreemptionDisabledValues_nvlddmkm = new Dictionary<string, object>
        {
            { "DisablePreemption", 1 },
            { "DisableCudaContextPreemption", 1 }
        };

        public static bool IsPreemptionEnabled()
        {
            // Check NvidiaClass0000
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey classKey = baseKey.OpenSubKey(NvidiaClass0000, false))
            {
                if (classKey != null)
                {
                    foreach (var kvp in PreemptionDisabledValues_Class0000)
                    {
                        object actualVal = classKey.GetValue(kvp.Key);
                        if (actualVal is int intVal && intVal == (int)kvp.Value)
                        {
                            return false;
                        }
                    }
                }
            }

            // Check nvlddmkm
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey nvKey = baseKey.OpenSubKey(NvlddmkmPath, false))
            {
                if (nvKey != null)
                {
                    foreach (var kvp in PreemptionDisabledValues_nvlddmkm)
                    {
                        object actualVal = nvKey.GetValue(kvp.Key);
                        if (actualVal is int intVal && intVal == (int)kvp.Value)
                        {
                            return false;
                        }
                    }
                }
            }
            // If none match => enabled
            return true;
        }

        public static void SetPreemptionEnabled(bool enabled)
        {
            if (enabled)
            {
                // Remove keys
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    // NvidiaClass0000
                    using (RegistryKey classKey = baseKey.OpenSubKey(NvidiaClass0000, writable: true))
                    {
                        if (classKey != null)
                        {
                            foreach (var kvp in PreemptionDisabledValues_Class0000)
                            {
                                try
                                {
                                    if (classKey.GetValue(kvp.Key) != null)
                                        classKey.DeleteValue(kvp.Key);
                                }
                                catch { }
                            }
                        }
                    }

                    // nvlddmkm
                    using (RegistryKey nvKey = baseKey.OpenSubKey(NvlddmkmPath, writable: true))
                    {
                        if (nvKey != null)
                        {
                            foreach (var kvp in PreemptionDisabledValues_nvlddmkm)
                            {
                                try
                                {
                                    if (nvKey.GetValue(kvp.Key) != null)
                                        nvKey.DeleteValue(kvp.Key);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            else
            {
                // Create keys with "disabled" values
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    // NvidiaClass0000
                    using (RegistryKey classKey = baseKey.CreateSubKey(NvidiaClass0000))
                    {
                        if (classKey != null)
                        {
                            foreach (var kvp in PreemptionDisabledValues_Class0000)
                            {
                                classKey.SetValue(kvp.Key, kvp.Value, RegistryValueKind.DWord);
                            }
                        }
                    }

                    // nvlddmkm
                    using (RegistryKey nvKey = baseKey.CreateSubKey(NvlddmkmPath))
                    {
                        if (nvKey != null)
                        {
                            foreach (var kvp in PreemptionDisabledValues_nvlddmkm)
                            {
                                nvKey.SetValue(kvp.Key, kvp.Value, RegistryValueKind.DWord);
                            }
                        }
                    }
                }
            }
        }

        // D) hdcpBtn
        private const string HdcpBasePath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000\DAL2_DATA__2_0\DisplayPath_4\EDID_D109_78E9\Option";
        private const string HdcpValueName = "ProtectionControl";

        private static readonly byte[] HdcpDisabledBytes = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };
        private static readonly byte[] HdcpEnabledBytes = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public static bool IsHdcpEnabled()
        {
            object raw = ReadRegistryValue(RegistryHive.LocalMachine,
                                           HdcpBasePath,
                                           HdcpValueName,
                                           HdcpEnabledBytes,
                                           RegistryValueKind.Binary);
            if (raw is byte[] arr)
            {
                return CompareByteArrays(arr, HdcpEnabledBytes);
            }
            return false;
        }

        public static void SetHdcpEnabled(bool enabled)
        {
            var data = enabled ? HdcpEnabledBytes : HdcpDisabledBytes;
            WriteRegistryValue(RegistryHive.LocalMachine,
                               HdcpBasePath,
                               HdcpValueName,
                               data,
                               RegistryValueKind.Binary);
        }

        private static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        // E) overlayBtn
        private const string OverlayRegistryPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000";
        private const string OverlayValueName = "AllowRSOverlay";

        public static bool IsOverlayEnabled()
        {
            object val = ReadRegistryValue(RegistryHive.LocalMachine,
                                           OverlayRegistryPath,
                                           OverlayValueName,
                                           "true",
                                           RegistryValueKind.String);
            return (val is string s && s.Equals("true", StringComparison.OrdinalIgnoreCase));
        }

        public static void SetOverlayEnabled(bool enabled)
        {
            string v = enabled ? "true" : "false";
            WriteRegistryValue(RegistryHive.LocalMachine,
                               OverlayRegistryPath,
                               OverlayValueName,
                               v,
                               RegistryValueKind.String);
        }

        // F) ulpsBtn
        private const string UlpsRegistryPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000";
        private const string UlpsValueName = "EnablingUlps";
        private const string UlpsNaValueName = "EnablingUlps_NA";

        public static bool IsUlpsEnabled()
        {
            bool isUlpsOn = false;
            bool isUlpsNaOn = false;

            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, UlpsRegistryPath, UlpsValueName, out int val1, 1))
            {
                isUlpsOn = (val1 == 1);
            }

            object val2 = ReadRegistryValue(RegistryHive.LocalMachine,
                                            UlpsRegistryPath,
                                            UlpsNaValueName,
                                            "1",
                                            RegistryValueKind.String);
            if (val2 is string sVal && sVal == "1")
            {
                isUlpsNaOn = true;
            }

            return (isUlpsOn && isUlpsNaOn);
        }

        public static void SetUlpsEnabled(bool enabled)
        {
            int ulpsVal = enabled ? 1 : 0;
            string ulpsNaVal = enabled ? "1" : "0";

            WriteRegistryValue(RegistryHive.LocalMachine,
                               UlpsRegistryPath,
                               UlpsValueName,
                               ulpsVal,
                               RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                               UlpsRegistryPath,
                               UlpsNaValueName,
                               ulpsNaVal,
                               RegistryValueKind.String);
        }


        // -------------------------------------------------------------------
        // ============ NOVÉ METODY PRO VAŠE DODATEČNÉ FUNKCE ================
        // -------------------------------------------------------------------

        // A) csrssPriorityBtn (CpuPriorityClass, IoPriority)
        private const string CsrssPerfOptionsPath =
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions";

        // ENABLED => CpuPriorityClass=4, IoPriority=3
        // DISABLED => CpuPriorityClass=3, IoPriority=2
        public static bool IsCsrssPriorityEnabled()
        {
            bool ok1 = TryReadRegistryValue<int>(RegistryHive.LocalMachine,
                CsrssPerfOptionsPath, "CpuPriorityClass", out int cpuPriority, 3);
            bool ok2 = TryReadRegistryValue<int>(RegistryHive.LocalMachine,
                CsrssPerfOptionsPath, "IoPriority", out int ioPriority, 2);

            if (!ok1 || !ok2) return false;

            // Podle požadavku: 4 a 3 => Enabled, 3 a 2 => Disabled
            return (cpuPriority == 4 && ioPriority == 3);
        }

        public static void SetCsrssPriorityEnabled(bool enabled)
        {
            int cpuVal = enabled ? 4 : 3;
            int ioVal = enabled ? 3 : 2;

            WriteRegistryValue(RegistryHive.LocalMachine,
                CsrssPerfOptionsPath, "CpuPriorityClass", cpuVal, RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                CsrssPerfOptionsPath, "IoPriority", ioVal, RegistryValueKind.DWord);
        }

        // B) cortanaBtn
        // HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search => sedm DWORD hodnot (AllowCortana, AllowCloudSearch, ...)
        private static readonly List<RegistrySetting> CortanaSettings = new List<RegistrySetting>
        {
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                ValueName = "AllowCortana",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                ValueName = "AllowCloudSearch",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                ValueName = "AllowCortanaAboveLock",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                ValueName = "AllowSearchToUseLocation",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                ValueName = "ConnectedSearchUseWeb",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                ValueName = "ConnectedSearchUseWebOverMeteredConnections",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 1,
                ValueKind = RegistryValueKind.DWord
            },
            new RegistrySetting
            {
                Hive = RegistryHive.LocalMachine,
                RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                ValueName = "DisableWebSearch",
                DisabledValue = 0,
                EnabledValue = 1,
                DefaultValue = 0,
                ValueKind = RegistryValueKind.DWord
            },
        };

        public static bool IsCortanaEnabled()
        {
            foreach (var setting in CortanaSettings)
            {
                object curVal = ReadRegistryValue(setting.Hive, setting.RegistryPath, setting.ValueName, setting.DefaultValue, setting.ValueKind);
                if (!curVal.Equals(setting.EnabledValue))
                {
                    return false;
                }
            }
            return true;
        }

        public static void SetCortanaEnabled(bool enabled)
        {
            foreach (var setting in CortanaSettings)
            {
                object valToSet = enabled ? setting.EnabledValue : setting.DisabledValue;
                WriteRegistryValue(setting.Hive, setting.RegistryPath, setting.ValueName, valToSet, setting.ValueKind);
            }
        }

        // C) smartScreenBtn
        // 1) HKLM\SOFTWARE\Policies\Microsoft\Windows\System => "EnablingSmartScreen" => 0/1
        // 2) HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer => "SmartScreenEnabled" => "Off"/"On" (REG_SZ)
        // 3) HKU\!USER_SID!\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost => "EnablingWebContentEvaluation" => 0/1
        private static readonly string SmartScreenPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\System";
        private static readonly string SmartScreenExplorerPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer";

        // Tady si zjistíme SID aktuálního uživatele (podobně jako CurrentUser).
        private static string CurrentUserSid
        {
            get
            {
                var sid = WindowsIdentity.GetCurrent().User?.Value;
                return sid ?? ".DEFAULT";
            }
        }

        // Cesta do HKU\[SID]\Software\Microsoft\Windows\CurrentVersion\AppHost
        private static string SmartScreenUserPath
        {
            get
            {
                return CurrentUserSid + @"\Software\Microsoft\Windows\CurrentVersion\AppHost";
            }
        }

        public static bool IsSmartScreenEnabled()
        {
            // 1) EnablingSmartScreen => 1
            if (!TryReadRegistryValue<int>(RegistryHive.LocalMachine, SmartScreenPolicyPath, "EnablingSmartScreen", out int policyVal, 1))
                return false;
            if (policyVal != 1)
                return false;

            // 2) SmartScreenEnabled => "On"
            object sseVal = ReadRegistryValue(RegistryHive.LocalMachine, SmartScreenExplorerPath, "SmartScreenEnabled", "Off", RegistryValueKind.String);
            if (!(sseVal is string s) || !s.Equals("On", StringComparison.OrdinalIgnoreCase))
                return false;

            // 3) EnablingWebContentEvaluation => 1 (HKU)
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64))
            {
                using (RegistryKey userKey = baseKey.OpenSubKey(SmartScreenUserPath, false))
                {
                    if (userKey == null)
                        return false; // pokud klíč neexistuje, bereme to jako vypnuté

                    object webEvalVal = userKey.GetValue("EnablingWebContentEvaluation");
                    if (webEvalVal is int intVal)
                    {
                        return (intVal == 1);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public static void SetSmartScreenEnabled(bool enabled)
        {
            // 1) HKLM\SOFTWARE\Policies\Microsoft\Windows\System => "EnablingSmartScreen" => 0/1
            WriteRegistryValue(RegistryHive.LocalMachine,
                SmartScreenPolicyPath,
                "EnablingSmartScreen",
                enabled ? 1 : 0,
                RegistryValueKind.DWord);

            // 2) HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer => "SmartScreenEnabled" => "On"/"Off"
            WriteRegistryValue(RegistryHive.LocalMachine,
                SmartScreenExplorerPath,
                "SmartScreenEnabled",
                enabled ? "On" : "Off",
                RegistryValueKind.String);

            // 3) HKU\[SID]\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost => "EnablingWebContentEvaluation" => 0/1
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64))
            {
                using (RegistryKey userKey = baseKey.CreateSubKey(SmartScreenUserPath))
                {
                    if (userKey != null)
                    {
                        userKey.SetValue("EnablingWebContentEvaluation", enabled ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
            }
        }

        // D) windowsInsiderBtn
        // HKLM\SOFTWARE\Microsoft\PolicyManager\current\device\System => "AllowExperimentation" => 0/1
        // HKLM\SOFTWARE\Microsoft\PolicyManager\default\System\AllowExperimentation => "value" => 0/1
        private static readonly string InsiderCurrentPath = @"SOFTWARE\Microsoft\PolicyManager\current\device\System";
        private static readonly string InsiderDefaultPath = @"SOFTWARE\Microsoft\PolicyManager\default\System\AllowExperimentation";

        public static bool IsWindowsInsiderEnabled()
        {
            bool ok1 = TryReadRegistryValue<int>(RegistryHive.LocalMachine, InsiderCurrentPath, "AllowExperimentation", out int curVal, 0);
            if (!ok1 || curVal != 1) return false;

            bool ok2 = TryReadRegistryValue<int>(RegistryHive.LocalMachine, InsiderDefaultPath, "value", out int defVal, 0);
            if (!ok2 || defVal != 1) return false;

            return true;
        }

        public static void SetWindowsInsiderEnabled(bool enabled)
        {
            int v = enabled ? 1 : 0;
            WriteRegistryValue(RegistryHive.LocalMachine, InsiderCurrentPath, "AllowExperimentation", v, RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine, InsiderDefaultPath, "value", v, RegistryValueKind.DWord);
        }

        // E) cuiSwitch2 (Biometrics)
        // HKLM\SOFTWARE\Policies\Microsoft\Biometrics => "Enabled" => 0/1
        private static readonly string BiometricsPath = @"SOFTWARE\Policies\Microsoft\Biometrics";

        public static bool IsBiometricsEnabled()
        {
            bool ok = TryReadRegistryValue<int>(RegistryHive.LocalMachine, BiometricsPath, "Enabled", out int val, 1);
            if (!ok) return false;
            return (val == 1);
        }

        public static void SetBiometricsEnabled(bool enabled)
        {
            int v = enabled ? 1 : 0;
            WriteRegistryValue(RegistryHive.LocalMachine, BiometricsPath, "Enabled", v, RegistryValueKind.DWord);
        }


        public static bool IsBrowserBackgroundProcessesEnabled()
        {
            // Microsoft Edge policies
            int edgeStartupBoost = (int)ReadRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 1, RegistryValueKind.DWord);
            int edgeHardwareAcceleration = (int)ReadRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "HardwareAccelerationModeEnabled", 1, RegistryValueKind.DWord);
            int edgeBackgroundMode = (int)ReadRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "BackgroundModeEnabled", 1, RegistryValueKind.DWord);
            bool edgePoliciesEnabled = (edgeStartupBoost == 1 && edgeHardwareAcceleration == 1 && edgeBackgroundMode == 1);

            int edgeElevationStart = 0;
            bool edgeElevationEnabled = TryReadRegistryValue<int>(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\MicrosoftEdgeElevationService", "Start", out edgeElevationStart, 2) && (edgeElevationStart == 2);
            int edgeUpdateStart = 0;
            bool edgeUpdateEnabled = TryReadRegistryValue<int>(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\edgeupdate", "Start", out edgeUpdateStart, 2) && (edgeUpdateStart == 2);
            int edgeUpdateMStart = 0;
            bool edgeUpdateMEnabled = TryReadRegistryValue<int>(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\edgeupdatem", "Start", out edgeUpdateMStart, 2) && (edgeUpdateMStart == 2);
            bool edgeServicesEnabled = edgeElevationEnabled && edgeUpdateEnabled && edgeUpdateMEnabled;

            // Google Chrome policies
            int chromeStartupBoost = (int)ReadRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", "StartupBoostEnabled", 1, RegistryValueKind.DWord);
            int chromeBackgroundMode = (int)ReadRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", "BackgroundModeEnabled", 1, RegistryValueKind.DWord);
            bool chromePoliciesEnabled = (chromeStartupBoost == 1 && chromeBackgroundMode == 1);

            int chromeElevationStart = 0;
            bool chromeElevationEnabled = TryReadRegistryValue<int>(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\GoogleChromeElevationService", "Start", out chromeElevationStart, 2) && (chromeElevationStart == 2);
            int gupdateStart = 0;
            bool gupdateEnabled = TryReadRegistryValue<int>(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\gupdate", "Start", out gupdateStart, 2) && (gupdateStart == 2);
            int gupdatemStart = 0;
            bool gupdatemEnabled = TryReadRegistryValue<int>(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\gupdatem", "Start", out gupdatemStart, 2) && (gupdatemStart == 2);
            bool chromeServicesEnabled = chromeElevationEnabled && gupdateEnabled && gupdatemEnabled;

            return edgePoliciesEnabled && edgeServicesEnabled && chromePoliciesEnabled && chromeServicesEnabled;
        }


        public static void SetBrowserBackgroundProcessesEnabled(bool enabled)
        {
            int policyValue = enabled ? 1 : 0;
            // Microsoft Edge policies
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SOFTWARE\Policies\Microsoft\Edge",
                               "StartupBoostEnabled",
                               policyValue,
                               RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SOFTWARE\Policies\Microsoft\Edge",
                               "HardwareAccelerationModeEnabled",
                               policyValue,
                               RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SOFTWARE\Policies\Microsoft\Edge",
                               "BackgroundModeEnabled",
                               policyValue,
                               RegistryValueKind.DWord);

            int serviceStart = enabled ? 2 : 4;
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SYSTEM\CurrentControlSet\Services\MicrosoftEdgeElevationService",
                               "Start",
                               serviceStart,
                               RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SYSTEM\CurrentControlSet\Services\edgeupdate",
                               "Start",
                               serviceStart,
                               RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SYSTEM\CurrentControlSet\Services\edgeupdatem",
                               "Start",
                               serviceStart,
                               RegistryValueKind.DWord);

            // Google Chrome policies
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SOFTWARE\Policies\Google\Chrome",
                               "StartupBoostEnabled",
                               policyValue,
                               RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SOFTWARE\Policies\Google\Chrome",
                               "BackgroundModeEnabled",
                               policyValue,
                               RegistryValueKind.DWord);

            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SYSTEM\CurrentControlSet\Services\GoogleChromeElevationService",
                               "Start",
                               serviceStart,
                               RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SYSTEM\CurrentControlSet\Services\gupdate",
                               "Start",
                               serviceStart,
                               RegistryValueKind.DWord);
            WriteRegistryValue(RegistryHive.LocalMachine,
                               @"SYSTEM\CurrentControlSet\Services\gupdatem",
                               "Start",
                               serviceStart,
                               RegistryValueKind.DWord);
        }

        // For Advertising: if the DisabledByGroupPolicy value does not exist then advertising is enabled.
        private static readonly string AdvertisingInfoRegPath = @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo";
        private const string DisabledByGroupPolicyValueName = "DisabledByGroupPolicy";

        /// <summary>
        /// Returns true if advertising is enabled.
        /// If the registry key/value does not exist or its value is 0, then advertising is enabled.
        /// </summary>
        public static bool IsAdvertisingEnabled()
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey key = baseKey.OpenSubKey(AdvertisingInfoRegPath, false))
            {
                if (key == null || key.GetValue(DisabledByGroupPolicyValueName) == null)
                    return true;

                try
                {
                    int value = (int)key.GetValue(DisabledByGroupPolicyValueName);
                    // 0 means enabled, 1 means restricted/disabled.
                    return (value == 0);
                }
                catch
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Sets advertising to enabled or disabled.
        /// When enabled, the registry value is removed (or treated as 0).
        /// When disabled, the value is set to 1.
        /// </summary>
        public static void SetAdvertisingEnabled(bool enabled)
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey key = baseKey.OpenSubKey(AdvertisingInfoRegPath, true) ?? baseKey.CreateSubKey(AdvertisingInfoRegPath))
            {
                if (enabled)
                {
                    // Enable advertising: remove the value if it exists.
                    if (key.GetValue(DisabledByGroupPolicyValueName) != null)
                        key.DeleteValue(DisabledByGroupPolicyValueName);
                }
                else
                {
                    // Disable advertising: set value to 1.
                    key.SetValue(DisabledByGroupPolicyValueName, 1, RegistryValueKind.DWord);
                }
            }
        }

        // ------------------ NEW: App Privacy Restriction ------------------
        private static readonly string AppPrivacyRegPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy";
        private static readonly string[] AppPrivacyValueNames = new string[]
        {
            "LetAppsAccessAccountInfo",
            "LetAppsAccessGazeInput",
            "LetAppsAccessCallHistory",
            "LetAppsAccessContacts",
            "LetAppsGetDiagnosticInfo",
            "LetAppsAccessEmail",
            "LetAppsAccessLocation",
            "LetAppsAccessMessaging",
            "LetAppsAccessMotion",
            "LetAppsAccessNotifications",
            "LetAppsAccessTasks",
            "LetAppsAccessCalendar",
            "LetAppsAccessTrustedDevices",
            "LetAppsAccessBackgroundSpatialPerception",
            "LetAppsActivateWithVoice",
            "LetAppsActivateWithVoiceAboveLock",
            "LetAppsSyncWithDevices",
            "LetAppsAccessRadios",
            "LetAppsAccessPhone",
            "LetAppsRunInBackground"
        };

        /// <summary>
        /// Determines whether app privacy is NOT restricted.
        /// Returns true if at least one registry value is not 2 (meaning at least one is unrestricted),
        /// and false if all values are 2 (fully restricted).
        /// This boolean is used to set the UI toggle (cuiSwitch25.Checked):
        ///   Checked = not restricted; Unchecked = restricted.
        /// </summary>
        public static bool IsAppPrivacyRestricted()
        {
            foreach (var valName in AppPrivacyValueNames)
            {
                int val = 0;
                TryReadRegistryValue<int>(RegistryHive.LocalMachine, AppPrivacyRegPath, valName, out val, 0);
                if (val != 2)
                    return true; // At least one value is not restricted
            }
            return false; // All values are 2, i.e. restricted
        }

        /// <summary>
        /// Sets the app privacy restriction state.
        /// When the parameter is true (toggle checked => NOT restricted), all values are set to 0.
        /// When false (toggle unchecked => restricted), all values are set to 2.
        /// </summary>
        public static void SetAppPrivacyRestricted(bool notRestricted)
        {
            int appPrivacyValue = notRestricted ? 0 : 2;
            foreach (var valName in AppPrivacyValueNames)
            {
                WriteRegistryValue(RegistryHive.LocalMachine, AppPrivacyRegPath, valName, appPrivacyValue, RegistryValueKind.DWord);
            }
        }

        // ------------------ NEW: Automatic Maintenance ------------------
        private static readonly string AutoMaintenanceRegPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance";
        private const string MaintenanceDisabledValueName = "MaintenanceDisabled";

        /// <summary>
        /// Returns true if Automatic Maintenance is enabled.
        /// If the registry key or value does not exist, it is assumed to be enabled.
        /// </summary>
        public static bool IsAutomaticMaintenanceEnabled()
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey key = baseKey.OpenSubKey(AutoMaintenanceRegPath, false))
            {
                if (key == null || key.GetValue(MaintenanceDisabledValueName) == null)
                    return true; // assume enabled if missing

                try
                {
                    int value = (int)key.GetValue(MaintenanceDisabledValueName);
                    // 0 means enabled, 1 means disabled
                    return (value == 0);
                }
                catch
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Sets Automatic Maintenance to enabled or disabled.
        /// When enabled (true) the registry value is set to 0; when disabled (false) it is set to 1.
        /// </summary>
        public static void SetAutomaticMaintenanceEnabled(bool enabled)
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey key = baseKey.OpenSubKey(AutoMaintenanceRegPath, true) ?? baseKey.CreateSubKey(AutoMaintenanceRegPath))
            {
                if (enabled)
                {
                    key.SetValue(MaintenanceDisabledValueName, 0, RegistryValueKind.DWord);
                }
                else
                {
                    key.SetValue(MaintenanceDisabledValueName, 1, RegistryValueKind.DWord);
                }
            }
        }




    }
}



