# README – **crazikktweaks FREE Utility**

This project is a simple Windows Forms (C#) application that allows you to tweak various system settings at the registry level (Windows, CPU, GPU, etc.) to optimize system performance and responsiveness. The project consists of:

1. **panel_core (menu.cs)** – the main form that provides the user interface, handles panel switching (Windows, CPU, GPU, Home), and the logic for loading/writing registry values.  
2. **SystemSettings (SystemSettings.cs)** – a static class that encapsulates all registry read/write operations. It contains the paths, values, and toggle logic for each setting (button) in the application.

---

## How to Build and Run

1. **Clone the repository** (if using Git) or copy the source files into a folder on your machine.
2. Open the solution/project in an IDE such as [Visual Studio] or [Visual Studio Code] (with .NET extensions installed).
3. Ensure you have the .NET Framework (at least 4.7.2 or a suitable .NET version) installed.
4. In your IDE, choose **Build** and then **Run** (F5).
5. The application will open a Windows Form with a sidebar menu (Home, Windows, CPU, GPU).

---

## Application Overview and Features

### Main Form (`panel_core` / *menu.cs*)
- **UI Components**:
  - **Home**, **Windows**, **CPU**, **GPU**: Tabs to navigate between different panels. Each panel contains toggle checkboxes for enabling/disabling specific system tweaks.
  - **Toggle buttons** (“CheckedChanged” event handlers): Call methods in `SystemSettings` to write to the registry. The application then verifies whether the registry change was successful.
  - **Restore Point** button: Creates a restore point in Windows (via PowerShell).
  - **Check for Updates**: Fetches version info via JSON from a URL and offers to open a Discord link if a newer version is found.
  - **Tutorial** and **logo** (PictureBox): Links to YouTube / official website.

- **Key Class**: `panel_core : Form`
  - The constructor sets up UI colors, hides unnecessary panels, and configures the *Home* panel by default.
  - The `InitializeAllSettings()` method reads all toggle states from the registry so the UI reflects the actual system status.

### `SystemSettings` Class (*SystemSettings.cs*)
- Implements most of the logic for:
  - **Reading / writing** values from/to the Windows Registry (HKLM, HKCU, etc.).
  - Determining whether a specific feature is enabled or disabled, based on registry paths and values.
  - Methods like `IsDiagnosticsEnabled()`, `SetDiagnosticsEnabled(bool)`, `IsTelemetryEnabled()`, `SetTelemetryEnabled(bool)`, etc.

- Each group of related settings (e.g., Animations, Bluetooth, AMD/Nvidia GPU) has its own constants for registry paths and corresponding functions `Is...Enabled()` + `Set...Enabled(bool)`.

---

## Important Notice

- **Registry changes** can affect system stability. It’s strongly recommended to:
  1. **Create a restore point** before applying any tweaks.
  2. Run the tool with **Administrator privileges**.
  3. Back up important data as needed.
- Some changes may require a system reboot (or a service/driver restart) to take effect.
- Certain GPU-related settings (Nvidia vs. AMD) only appear if the respective hardware is actually present in the system.

---

## Usage Guide

1. **Run as Administrator** (right-click → Run as Admin).
2. In the **left sidebar**, choose:
   - **Windows** – Toggles for diagnostics, animations, Bluetooth, notifications, Game Mode, etc.
   - **CPU** – Toggles for timers, C-states, core parking, Fair Share, and more.
   - **GPU** – Only the relevant section (AMD or Nvidia) is shown, based on hardware detection. You can disable/enable telemetry, HDCP, overlay, ULPS, etc.
   - **Home** – Returns to the default “home” view.
3. **Check or uncheck** the box next to a setting to enable or disable it. The application will attempt to update the registry and then verify the new state.
4. If an error occurs, it usually indicates Windows has blocked the change or a permission/path issue has arisen.
5. **Restore Point** – Recommended before making changes; creates a system restore point on the system drive.
6. **Check for Updates** – Checks the website for a newer version of the utility.
7. **Tutorial** – Opens the YouTube channel with guides.

## Support and Contact

- Official website: [crazikktweaks.com](https://www.crazikktweaks.com)  
- YouTube: [@crazikktweaksUS](https://www.youtube.com/@crazikktweaksUS)  
- Discord: [https://discord.gg/crazikktweaks](https://discord.gg/crazikktweaks)

For questions or issues, feel free to use the links above.



## License

> **Important Note**: The following license restricts commercial use and is therefore **not** OSI-approved as “open source.” It is a **custom** non-commercial license that allows study, personal use, and modification, but prohibits commercial exploitation.

```
# Non-Commercial License

Copyright (c) 2025 crazikktweaks

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to use,
copy, study, and modify the Software, subject to the following conditions:

1. **Non-Commercial Usage**  
   This Software is made available solely for personal, educational, or
   non-commercial research purposes. You may not use, reproduce, or distribute
   this Software, or any modified version of it, for any direct or indirect
   commercial purpose without the explicit written consent of the copyright
   holder.

2. **Attribution**  
   Any redistribution of the Software or its modifications must include
   attribution to the original author(s). This must be prominently displayed
   in any documentation or notices, stating that you have changed the
   Software and the nature of those changes.

3. **No Warranty**  
   THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
   FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
   DEALINGS IN THE SOFTWARE.

4. **No Liability**  
   In no event shall the copyright holder or contributors be held liable for
   any direct, indirect, incidental, special, exemplary, or consequential
   damages (including, but not limited to, procurement of substitute goods or
   services; loss of use, data, or profits; or business interruption) however
   caused and on any theory of liability, whether in contract, strict
   liability, or tort (including negligence or otherwise) arising in any way
   out of the use of this Software, even if advised of the possibility of
   such damage.

5. **Termination**  
   Any breach of the terms in this license, including use of the Software for
   commercial or for-profit purposes without explicit permission, will result
   in termination of your rights under this License. Upon termination, you
   must immediately cease all use and distribution of the Software and any
   modifications thereto.

By using the Software, you agree to all terms stated in this License.
If you do not agree, do not use, modify, or distribute this Software.
```
