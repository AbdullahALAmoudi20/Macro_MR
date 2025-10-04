using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Reflection;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace MacroMR
{
    // A dedicated class to hold all macro settings for thread-safe updates.
    public class MacroConfiguration
    {
        public string TriggerKey { get; set; } = "";
        public string Key1 { get; set; } = "";
        public string Key2 { get; set; } = "";
        public string Key3 { get; set; } = "";
        public string Key4 { get; set; } = "";
        public string Key5 { get; set; } = "";
        public bool Key1Enabled { get; set; }
        public bool Key2Enabled { get; set; }
        public bool Key3Enabled { get; set; }
        public bool Key4Enabled { get; set; }
        public bool Key5Enabled { get; set; }
        public bool InfiniteModeEnabled { get; set; }
        public double MacroSpeed { get; set; }

        // Corresponding virtual key codes
        public int VkTrigger { get; set; }
        public int Vk1 { get; set; }
        public int Vk2 { get; set; }
        public int Vk3 { get; set; }
        public int Vk4 { get; set; }
        public int Vk5 { get; set; }
    }

    public partial class MainWindow : Window
    {
        // ======== State Management ========
        private static volatile MacroConfiguration CurrentConfig = new MacroConfiguration();
        private static volatile bool isMacroRunning = false;
        private static bool key3Held = false;

        // ======== Keyboard Hooking State ========
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private static IntPtr _hookID = IntPtr.Zero;
        private static bool _isTriggerHeldDown = false;
        private static volatile bool _isInfiniteLoopRunning = false;
        private static bool _isInfiniteTriggerDown = false;


        public MainWindow()
        {
            InitializeComponent();
            _proc = HookCallback;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            BumpPriority();
            this.Closed += Window_Closed;
            await CheckForUpdates(); // Automatically check for updates on startup
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            StopMacroInternal();
        }

        public async void ReloadSettings()
        {
            bool wasRunning = isMacroRunning;

            if (wasRunning)
            {
                // This stops the hook, sets isMacroRunning=false, and updates UI to "Start Macro"
                StopMacroInternal();
                // Give it a moment to fully stop and unhook before we try to re-hook, without blocking the UI.
                await Task.Delay(100);
            }

            // This loads the new settings from the file into CurrentConfig
            LoadSettings();

            if (wasRunning)
            {
                // We are on the UI thread, so no dispatcher needed.
                if (string.IsNullOrWhiteSpace(CurrentConfig.TriggerKey))
                {
                    MessageBox.Show("❌ لا يمكن إعادة تشغيل الماكرو. يرجى تعيين مفتاح تشغيل صالح في الإعدادات.", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // The core "start" logic from StartMacro_Click
                _hookID = SetHook(_proc);
                isMacroRunning = true;
                StartMacroButton.Content = "إيقاف الماكرو";
            }
        }

        // This method now creates a new configuration object, making updates atomic.
        private void LoadSettings()
        {
            var newConfig = new MacroConfiguration
            {
                TriggerKey = Properties.Settings.Default.MainKey,
                Key1 = Properties.Settings.Default.Key1,
                Key2 = Properties.Settings.Default.Key2,
                Key3 = Properties.Settings.Default.Key3,
                Key4 = Properties.Settings.Default.Key4,
                Key5 = Properties.Settings.Default.Key5,
                Key1Enabled = Properties.Settings.Default.EnableKey1,
                Key2Enabled = Properties.Settings.Default.EnableKey2,
                Key3Enabled = Properties.Settings.Default.EnableKey3,
                Key4Enabled = Properties.Settings.Default.EnableKey4,
                Key5Enabled = Properties.Settings.Default.EnableKey5,
                MacroSpeed = Properties.Settings.Default.MacroSpeed,
                InfiniteModeEnabled = Properties.Settings.Default.InfiniteMacro
            };

            if (newConfig.MacroSpeed < 0) newConfig.MacroSpeed = 0;

            // Calculate virtual key codes and add them to the new config
            newConfig.VkTrigger = GetVirtualKeyCode(newConfig.TriggerKey);
            newConfig.Vk1 = GetVirtualKeyCode(newConfig.Key1);
            newConfig.Vk2 = GetVirtualKeyCode(newConfig.Key2);
            newConfig.Vk3 = GetVirtualKeyCode(newConfig.Key3);
            newConfig.Vk4 = GetVirtualKeyCode(newConfig.Key4);
            newConfig.Vk5 = GetVirtualKeyCode(newConfig.Key5);

            // Atomically swap the configuration.
            CurrentConfig = newConfig;
        }

        private void StartMacro_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();

            if (string.IsNullOrWhiteSpace(CurrentConfig.TriggerKey))
            {
                MessageBox.Show("❌ يرجى تعيين مفتاح التشغيل في الإعدادات.", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!isMacroRunning)
            {
                _hookID = SetHook(_proc);
                isMacroRunning = true;
                StartMacroButton.Content = "إيقاف الماكرو";
            }
            else
            {
                StopMacroInternal();
            }
        }

        private void StopMacroInternal()
        {
            if (!isMacroRunning) return;

            _isInfiniteLoopRunning = false;
            _isInfiniteTriggerDown = false;

            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }

            isMacroRunning = false;
            _isTriggerHeldDown = false;

            if (key3Held && CurrentConfig.Vk3 != 0)
            {
                SendInputKeyUp(CurrentConfig.Vk3);
                key3Held = false;
            }

            Dispatcher.Invoke(() =>
            {
                StartMacroButton.Content = "بدء الماكرو";
            });
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT kbdStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                if (kbdStruct.dwExtraInfo == (IntPtr)MACRO_EXTRA_INFO)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                if (isMacroRunning)
                {
                    var config = CurrentConfig; // Use a consistent config for this event.
                    int vkCode = (int)kbdStruct.vkCode;

                    if (vkCode == config.VkTrigger)
                    {
                        if (config.InfiniteModeEnabled)
                        {
                            if (wParam == (IntPtr)WM_KEYDOWN)
                            {
                                if (!_isInfiniteTriggerDown)
                                {
                                    _isInfiniteTriggerDown = true;
                                    _isInfiniteLoopRunning = !_isInfiniteLoopRunning;

                                    if (_isInfiniteLoopRunning)
                                    {
                                        Task.Run(() => InfiniteMacroLoop());
                                    }
                                }
                            }
                            else if (wParam == (IntPtr)WM_KEYUP)
                            {
                                _isInfiniteTriggerDown = false;
                            }
                        }
                        else
                        {
                            if (wParam == (IntPtr)WM_KEYDOWN)
                            {
                                if (!_isTriggerHeldDown)
                                {
                                    _isTriggerHeldDown = true;
                                    Task.Run(() => ExecutePressSequence());
                                }
                            }
                            else if (wParam == (IntPtr)WM_KEYUP)
                            {
                                if (_isTriggerHeldDown)
                                {
                                    _isTriggerHeldDown = false;
                                    Task.Run(() => ExecuteReleaseSequence());
                                }
                            }
                        }
                        return (IntPtr)1;
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void ExecutePressSequence()
        {
            var config = CurrentConfig;

            // Check if the trigger is still held before each action
            if (config.Key1Enabled && config.Vk1 != 0)
            {
                if (!_isTriggerHeldDown) return;
                TapKeyFast(config.Vk1); PreciseSleep(config.MacroSpeed);
            }

            if (config.Key2Enabled && config.Vk2 != 0)
            {
                if (!_isTriggerHeldDown) return;
                TapKeyFast(config.Vk2); PreciseSleep(config.MacroSpeed);
            }

            if (config.Key3Enabled && config.Vk3 != 0)
            {
                if (!_isTriggerHeldDown) return;
                SendInputKeyDown(config.Vk3);
                key3Held = true;
            }
        }

        private static void ExecuteReleaseSequence()
        {
            var config = CurrentConfig;
            if (key3Held) { SendInputKeyUp(config.Vk3); key3Held = false; }
            PreciseSleep(config.MacroSpeed, false); // This sequence should not be interrupted
            if (config.Key4Enabled && config.Vk4 != 0) TapKeyFast(config.Vk4); PreciseSleep(config.MacroSpeed, false);
            if (config.Key5Enabled && config.Vk5 != 0) TapKeyFast(config.Vk5);
        }

        private static void InfiniteMacroLoop()
        {
            while (_isInfiniteLoopRunning)
            {
                var config = CurrentConfig; // Get latest settings for each loop.

                if (config.Key1Enabled && config.Vk1 != 0) TapKeyFast(config.Vk1);
                PreciseSleep(config.MacroSpeed);

                if (config.Key2Enabled && config.Vk2 != 0) TapKeyFast(config.Vk2);
                PreciseSleep(config.MacroSpeed);

                if (config.Key3Enabled && config.Vk3 != 0) TapKeyFast(config.Vk3);
                PreciseSleep(config.MacroSpeed);

                if (config.Key4Enabled && config.Vk4 != 0) TapKeyFast(config.Vk4);
                PreciseSleep(config.MacroSpeed);

                if (config.Key5Enabled && config.Vk5 != 0) TapKeyFast(config.Vk5);
                PreciseSleep(config.MacroSpeed);
            }
        }


        // ======== Fast Input Sending (SendInput) ========

        private static void TapKeyFast(int vk)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = (ushort)vk;
            inputs[0].U.ki.dwExtraInfo = (UIntPtr)MACRO_EXTRA_INFO;
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = (ushort)vk;
            inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;
            inputs[1].U.ki.dwExtraInfo = (UIntPtr)MACRO_EXTRA_INFO;
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendInputKeyDown(int vk)
        {
            INPUT input = new INPUT { type = INPUT_KEYBOARD };
            input.U.ki.wVk = (ushort)vk;
            input.U.ki.dwFlags = 0;
            input.U.ki.dwExtraInfo = (UIntPtr)MACRO_EXTRA_INFO;
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendInputKeyUp(int vk)
        {
            INPUT input = new INPUT { type = INPUT_KEYBOARD };
            input.U.ki.wVk = (ushort)vk;
            input.U.ki.dwFlags = KEYEVENTF_KEYUP;
            input.U.ki.dwExtraInfo = (UIntPtr)MACRO_EXTRA_INFO;
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        // ======== Helper Utilities ========

        private static int GetVirtualKeyCode(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return 0;
            if (Enum.TryParse(key, true, out System.Windows.Forms.Keys parsedKey))
            {
                return (int)parsedKey;
            }
            return 0;
        }

        private static void PreciseSleep(double ms, bool isInterruptible = true)
        {
            if (ms <= 0) return;
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalMilliseconds < ms)
            {
                if (isInterruptible)
                {
                    // Check for the condition to stop the macro based on its current mode.
                    // The infinite loop is now stopped by completing its current cycle, not by interrupting sleep.
                    bool normalModeStop = !CurrentConfig.InfiniteModeEnabled && !_isTriggerHeldDown;

                    if (normalModeStop)
                    {
                        break;
                    }
                }

                // Use a hybrid sleep mechanism for accuracy without high CPU usage.
                // If we have more than ~16ms to wait, we can afford to sleep, which yields the CPU.
                if (ms - sw.Elapsed.TotalMilliseconds > 16)
                {
                    Thread.Sleep(1); // Sleep for a minimum duration.
                }
                else
                {
                    // For the last few milliseconds, use SpinWait for higher precision.
                    Thread.SpinWait(20); // SpinWait is appropriate for very short waits.
                }
            }
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates(true); // Manual check
        }

        private async Task CheckForUpdates(bool manualCheck = false)
        {
            // IMPORTANT: Replace these with your GitHub username and repository name.
            string githubOwner = "AbdullahALAmoudi20";
            string githubRepo = "MacroMR";

            string url = $"https://api.github.com/repos/{githubOwner}/{githubRepo}/releases/latest";

            try
            {
                using (var client = new HttpClient())
                {
                    // GitHub API requires a User-Agent header.
                    client.DefaultRequestHeaders.Add("User-Agent", "MacroMR-UpdateRequest");

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        if (manualCheck)
                        {
                            MessageBox.Show($"تعذر استرداد معلومات التحديث. يرجى التحقق من اتصالك بالإنترنت أو تفاصيل مستودع GitHub.", "فشل التحقق من وجود تحديثات", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        return;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Use Regex to parse the JSON response to avoid dependency issues.
                    string tagPattern = "\"tag_name\":\\s*\"(.*?)\"";
                    string urlPattern = "\"html_url\":\\s*\"(.*?)\"";

                    Match tagMatch = Regex.Match(jsonResponse, tagPattern);
                    Match urlMatch = Regex.Match(jsonResponse, urlPattern);

                    if (tagMatch.Success && urlMatch.Success)
                    {
                        string latestVersionStr = tagMatch.Groups[1].Value.TrimStart('v', 'V');
                        string releaseUrl = urlMatch.Groups[1].Value;

                        if (Version.TryParse(latestVersionStr, out Version latestVersion))
                        {
                            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                            if (latestVersion > currentVersion)
                            {
                                var result = MessageBox.Show($"إصدار جديد ({latestVersion}) متاح. أنت تستخدم الإصدار {currentVersion}.\n\nهل ترغب في الانتقال إلى صفحة التنزيل؟", "تحديث متوفر", MessageBoxButton.YesNo, MessageBoxImage.Information);
                                if (result == MessageBoxResult.Yes)
                                {
                                    Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
                                }
                            }
                            else if (manualCheck)
                            {
                                MessageBox.Show("أنت تستخدم أحدث إصدار.", "مُحدَّث", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    else if (manualCheck)
                    {
                        MessageBox.Show($"تعذر تحليل معلومات التحديث من GitHub.", "فشل التحقق من وجود تحديثات", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                if (manualCheck)
                {
                    MessageBox.Show($"حدث خطأ أثناء التحقق من وجود تحديثات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            HomePanel.Visibility = Visibility.Hidden;
            MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            MainFrame.Content = new SettingsPage();
        }

        private void YouTube_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://youtube.com/@mr.marwan44?si=OpNURlMdzfGk_wHb") { UseShellExecute = true });
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://discord.gg/VPM53an4") { UseShellExecute = true });
        }


        // ======== Priority ========

        private void BumpPriority()
        {
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch { }
        }

        // ======== WinAPI ========

        private const int MACRO_EXTRA_INFO = 1337;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT { public uint type; public InputUnion U; }
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; [FieldOffset(0)] public HARDWAREINPUT hi; }
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}

