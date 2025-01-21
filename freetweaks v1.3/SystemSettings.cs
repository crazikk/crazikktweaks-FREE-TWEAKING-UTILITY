using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        /// <summary>
        /// Diagnostics considered enabled if either DiagTrack or dmwappushservice is not disabled (Start != 4).
        /// </summary>
        public static bool IsDiagnosticsEnabled()
        {
            bool diagOn = IsServiceEnabled(DiagTrackRegPath);
            bool dmwappushOn = IsServiceEnabled(DmwappushRegPath);
            return (diagOn || dmwappushOn);
        }

        /// <summary>
        /// Enables or disables Diagnostics by setting services Start=3 (Manual) or 4 (Disabled).
        /// </summary>
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

        /// <summary>
        /// Checks if animations are enabled by verifying all relevant registry settings match their "enabled" values.
        /// </summary>
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

        /// <summary>
        /// Enables or disables animations by writing the respective registry values.
        /// </summary>
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

        /// <summary>
        /// Checks if the keyboard is "enabled" by comparing a specific registry value to a known integer.
        /// </summary>
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

        /// <summary>
        /// Enables or disables keyboard tweaks by setting the KeyboardDataQueueSize (40 or 100).
        /// </summary>
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

        /// <summary>
        /// Checks if the mouse is "enabled" by comparing a registry value to a known integer.
        /// </summary>
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

        /// <summary>
        /// Enables or disables mouse tweaks by setting the MouseDataQueueSize (45 or 100).
        /// </summary>
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

        /// <summary>
        /// Determines whether error reporting is enabled by checking "Disabled" in WER policy.
        /// </summary>
        public static bool IsReportingEnabled()
        {
            TryReadRegistryValue<int>(RegistryHive.LocalMachine, WERPolicyPath, "Disabled", out int disabledVal, 0);
            return (disabledVal == 0);
        }

        /// <summary>
        /// Enables or disables Windows Error Reporting in various registry locations (WERPolicyPath, WERPolicyPath2, etc.).
        /// </summary>
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

        /// <summary>
        /// Checks if Bluetooth is enabled by verifying at least one related service is not disabled.
        /// </summary>
        public static bool IsBluetoothEnabled()
        {
            foreach (var svcPath in BluetoothServices)
            {
                if (IsServiceEnabled(svcPath)) return true;
            }
            return false;
        }

        /// <summary>
        /// Enables or disables Bluetooth services (BTAGService, bthserv, BthAvctpSvc, BluetoothUserService).
        /// </summary>
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

        /// <summary>
        /// Checks if background apps are enabled if either bam/dam is not disabled.
        /// </summary>
        public static bool IsBgappsEnabled()
        {
            bool bamOn = IsServiceEnabled(BamServicePath);
            bool damOn = IsServiceEnabled(DamServicePath);
            return (bamOn || damOn);
        }

        /// <summary>
        /// Enables or disables background apps by changing bam/dam service start types.
        /// </summary>
        public static void SetBgappsEnabled(bool enabled)
        {
            int sv = enabled ? 3 : 4; // 3=Manual, 4=Disabled
            SetServiceStart(BamServicePath, sv);
            SetServiceStart(DamServicePath, sv);
        }

        // 8) Transparency
        private const string TransparencyRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string TransparencyValueName = "EnableTransparency";

        /// <summary>
        /// Checks if transparency is enabled by reading "EnableTransparency" (1 = enabled).
        /// </summary>
        public static bool IsTransparencyEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.CurrentUser, TransparencyRegistryPath, TransparencyValueName, out int val, 1))
            {
                return (val == 1);
            }
            return false;
        }

        /// <summary>
        /// Enables or disables transparency by writing 1 or 0 to "EnableTransparency".
        /// </summary>
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

        /// <summary>
        /// Checks if Game Mode is enabled (AllowAutoGameMode=1).
        /// </summary>
        public static bool IsGamemodeEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.CurrentUser, GameBarPath, "AllowAutoGameMode", out int val, 0))
            {
                return (val == 1);
            }
            return false;
        }

        /// <summary>
        /// Enables or disables Game Mode by setting "AllowAutoGameMode" and "AutoGameModeEnabled" in GameBarPath.
        /// </summary>
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

        /// <summary>
        /// Checks if notification settings are enabled by verifying each relevant registry setting.
        /// </summary>
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

        /// <summary>
        /// Enables or disables notification settings by writing enabled or disabled values to each registry setting.
        /// </summary>
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

        /// <summary>
        /// Timers are considered enabled if "DistributeTimers"=0, disabled if =1.
        /// </summary>
        public static bool IsTimersEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, TimersRegistryPath, TimersValueName, out int val, 0))
            {
                return (val == 0);
            }
            return false;
        }

        /// <summary>
        /// Enables or disables timers by setting "DistributeTimers" to 0 or 1.
        /// </summary>
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

        /// <summary>
        /// Checks if C-States are enabled by verifying multiple registry entries are set to their "enabled" values.
        /// </summary>
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

        /// <summary>
        /// Enables or disables C-States by writing values to power settings (e.g. AwayModeEnabled, AllowStandby, etc.).
        /// </summary>
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

        /// <summary>
        /// Checks if EventProcessor is enabled by reading "EventProcessorEnabled"=1.
        /// </summary>
        public static bool IsEventProcessorEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, EventProcessorRegistryPath, EventProcessorValueName, out int val, 1))
            {
                return (val == 1);
            }
            return false;
        }

        /// <summary>
        /// Enables or disables EventProcessor by writing 1 or 0 to "EventProcessorEnabled".
        /// </summary>
        public static void SetEventProcessorEnabled(bool enabled)
        {
            int v = enabled ? 1 : 0;
            WriteRegistryValue(RegistryHive.LocalMachine, EventProcessorRegistryPath, EventProcessorValueName, v, RegistryValueKind.DWord);
        }

        // 4) fairShareBtn
        private const string FairShareRegistryPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Quota System";
        private const string FairShareValueName = "EnableCpuQuota";

        /// <summary>
        /// Checks if FairShare is enabled by verifying "EnableCpuQuota"=1 in the Quota System.
        /// </summary>
        public static bool IsFairShareEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, FairShareRegistryPath, FairShareValueName, out int val, 1))
            {
                return (val == 1);
            }
            return false;
        }

        /// <summary>
        /// Enables or disables FairShare by writing 1 or 0 to "EnableCpuQuota".
        /// </summary>
        public static void SetFairShareEnabled(bool enabled)
        {
            int v = enabled ? 1 : 0;
            WriteRegistryValue(RegistryHive.LocalMachine, FairShareRegistryPath, FairShareValueName, v, RegistryValueKind.DWord);
        }

        // 5) coreParkingBtn
        private const string CoreParkingRegistryPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583";
        private const string CoreParkingValueName = "ValueMin";

        /// <summary>
        /// Checks if core parking is enabled by verifying "ValueMin"=0 (meaning no parking).
        /// </summary>
        public static bool IsCoreParkingEnabled()
        {
            if (TryReadRegistryValue<int>(RegistryHive.LocalMachine, CoreParkingRegistryPath, CoreParkingValueName, out int val, 0))
            {
                return (val == 0);
            }
            return false;
        }

        /// <summary>
        /// Enables or disables core parking by setting "ValueMin"=0 or 100 in power settings.
        /// </summary>
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

        /// <summary>
        /// Checks if the GPU energy driver is enabled by verifying neither GpuEnergyDrv nor GpuEnergyDr is set to start=4.
        /// </summary>
        public static bool IsEnergyDriverEnabled()
        {
            // If either is "Start != 4" => consider it enabled.
            bool drv1 = IsServiceEnabled(GpuEnergyDrvPath);
            bool drv2 = IsServiceEnabled(GpuEnergyDrPath);
            return (drv1 || drv2);
        }

        /// <summary>
        /// Enables or disables GPU energy driver by setting Start=2 (Automatic) or 4 (Disabled) for both GpuEnergyDrv and GpuEnergyDr.
        /// </summary>
        public static void SetEnergyDriverEnabled(bool enabled)
        {
            int startVal = enabled ? 2 : 4; // 2=Automatic, 4=Disabled
            SetServiceStart(GpuEnergyDrvPath, startVal);
            SetServiceStart(GpuEnergyDrPath, startVal);
        }

        // B) telemetryBtn
        private const string NvRunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string NvRunValueName = "NvBackend";
        // Default path for Nvidia backend process
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

        /// <summary>
        /// Checks if Nvidia telemetry is enabled by verifying the presence of "NvBackend" in Run,
        /// and the relevant "EnableRIDxxx" registry entries set to 1.
        /// </summary>
        public static bool IsTelemetryEnabled()
        {
            // 1) Check if "NvBackend" is in RUN
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey runKey = baseKey.OpenSubKey(NvRunKeyPath, false))
                {
                    if (runKey?.GetValue(NvRunValueName) == null)
                        return false;
                }
            }

            // 2) Check the "EnableRIDxxx" (must be all 1 for enabled)
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

        /// <summary>
        /// Enables or disables Nvidia Telemetry.
        /// If disabled, it removes "NvBackend" from Run; if enabled, it adds it back.
        /// Also writes 0 or 1 to EnableRIDxxxx in HKLM\SOFTWARE\NVIDIA Corporation\Global\FTS.
        /// </summary>
        public static void SetTelemetryEnabled(bool enabled)
        {
            // 1) Toggle "NvBackend" in RUN
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey runKey = baseKey.OpenSubKey(NvRunKeyPath, writable: true))
                {
                    if (runKey != null)
                    {
                        if (!enabled)
                        {
                            // Remove
                            try { runKey.DeleteValue(NvRunValueName); }
                            catch { /* if doesn't exist, ignore */ }
                        }
                        else
                        {
                            // Create with default path
                            runKey.SetValue(NvRunValueName, NvBackendDefaultPath, RegistryValueKind.String);
                        }
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

        // If Preemption is "disabled", we create the following keys/values.
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

        /// <summary>
        /// Checks if preemption is enabled (i.e., these "disabled" keys do not exist).
        /// If any of these keys match the "disabled" value, we consider it disabled.
        /// </summary>
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

        /// <summary>
        /// Sets preemption to enabled (removing registry keys) or disabled (creating those keys).
        /// </summary>
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
                                catch
                                {
                                    // ignore
                                }
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
                                catch
                                {
                                    // ignore
                                }
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

        /// <summary>
        /// Checks if HDCP is enabled by comparing the registry binary value with the known "enabled" bytes.
        /// </summary>
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

        /// <summary>
        /// Enables or disables HDCP by writing specific binary data to "ProtectionControl".
        /// </summary>
        public static void SetHdcpEnabled(bool enabled)
        {
            var data = enabled ? HdcpEnabledBytes : HdcpDisabledBytes;
            WriteRegistryValue(RegistryHive.LocalMachine,
                               HdcpBasePath,
                               HdcpValueName,
                               data,
                               RegistryValueKind.Binary);
        }

        /// <summary>
        /// Helper to compare two byte arrays for equality.
        /// </summary>
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

        /// <summary>
        /// Checks if AMD overlay is enabled ("AllowRSOverlay"="true").
        /// </summary>
        public static bool IsOverlayEnabled()
        {
            object val = ReadRegistryValue(RegistryHive.LocalMachine,
                                           OverlayRegistryPath,
                                           OverlayValueName,
                                           "true",
                                           RegistryValueKind.String);
            return (val is string s && s.Equals("true", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Enables or disables AMD overlay by setting "AllowRSOverlay" to "true" or "false".
        /// </summary>
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

        /// <summary>
        /// Checks if AMD ULPS is enabled (EnablingUlps=1 and EnablingUlps_NA="1").
        /// </summary>
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

        /// <summary>
        /// Enables or disables AMD ULPS by setting EnablingUlps=1 or 0, and EnablingUlps_NA="1" or "0".
        /// </summary>
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
    }
}
