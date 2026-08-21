using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace LeitostrapMacro
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

        [DllImport("kernel32.dll")]
        private static extern bool Beep(int dwFreq, int dwDuration);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        private const uint MOUSEEVENTF_MOVE = 0x0001;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUTUNION
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private bool _macroActivated = false;
        private string _comboKey = "e";
        private double _sensitivity = 0.52;
        private int _loopCount = 5;

        private bool _ilActivated = false;
        private string _ilComboKey = "e";
        private double _ilSensitivity = 0.52;
        private int _ilSteps = 5;
        private string _ilFlickType = "Curved";

        private bool _ldActivated = false;
        private string _ldComboKey = "e";
        private double _ldSensitivity = 0.52;
        private int _ldSteps = 6;
        private int _ldKeyDelay = 120;
        private string _ldFlickType = "Linear";

        
        
        private bool _acActivated = false;
        private string _acKey = "none";
        private int _acCPS = 10;
        
        private bool _ajActivated = false;
        private string _ajKey = "space";
        private int _ajDelay = 10;

    
        
        private bool _potatoMode = false;
        private bool _topMost = true;
        private bool _notifications = true;
        private int _staticTheme = 0;
        private int _animatedTheme = 0;
        private System.Windows.Threading.DispatcherTimer _themeTimer;
        private Random _rnd = new Random();


        // AutoCombo (Blox Fruits)
        private bool _cbActivated = false;
        private string _cbKey = "b";
        private string _cbMeleeKey = "z";
        private string _cbFrutaKey = "x";
        private string _cbEspadaKey = "c";
        private string _cbGunKey = "v";
        private int _cbMeleeSlot = 1;
        private int _cbFrutaSlot = 2;
        private int _cbEspadaSlot = 3;
        private int _cbGunSlot = 4;

        private bool _fovActivated = false;
        private float _fovValue = 70.0f;

        private bool _bhActivated = false;
        private string _bhComboKey = "b";

        private bool _psActivated = false;
        private string _psForwardKey = "b";
        private string _psBackwardKey = "n";

        private string _activeMacroTab = "humbled";

        private readonly double b1 = 0.29;
        private readonly double c1 = 0.185;
        private readonly int e1 = 30;
        private readonly int f1 = 89;
        private readonly int g1 = 200;

        private bool _isListeningForKey = false;
        private string _listeningTarget = "humbled";
        private Thread _comboThread;
        private volatile bool _comboThreadRunning = false;

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Leitostrap-Macro");
        private static readonly string HumbledSettingsFile = Path.Combine(SettingsDir, "humbled.ini");
        private static readonly string ILSettingsFile = Path.Combine(SettingsDir, "instantlee.ini");
        private static readonly string LDSettingsFile = Path.Combine(SettingsDir, "lethaldash.ini");
        private static readonly string BHSettingsFile = Path.Combine(SettingsDir, "bunnyhop.ini");
        private static readonly string PSSettingsFile = Path.Combine(SettingsDir, "pillarslide.ini");
        private static readonly string ACSettingsFile = Path.Combine(SettingsDir, "autoclicker.ini");
        private static readonly string AJSettingsFile = Path.Combine(SettingsDir, "autojump.ini");
        private static readonly string FOVSettingsFile = Path.Combine(SettingsDir, "fovchanger.ini");
        private static readonly string BFSettingsFile = Path.Combine(SettingsDir, "bloxfruits.ini");
        private static readonly string MainSettingsFile = Path.Combine(SettingsDir, "mainsettings.ini");


        private System.Windows.Forms.NotifyIcon _trayIcon;
        private bool _isClosing = false;
        private IntPtr _hwnd;

        private Dictionary<string, ushort> _vkMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            {"q", 0x51}, {"w", 0x57}, {"e", 0x45}, {"r", 0x52},
            {"t", 0x54}, {"y", 0x59}, {"u", 0x55}, {"i", 0x49},
            {"o", 0x4F}, {"p", 0x50}, {"a", 0x41}, {"s", 0x53},
            {"d", 0x44}, {"f", 0x46}, {"g", 0x47}, {"h", 0x48},
            {"j", 0x4A}, {"k", 0x4B}, {"l", 0x4C}, {"z", 0x5A},
            {"x", 0x58}, {"c", 0x43}, {"v", 0x56}, {"b", 0x42},
            {"n", 0x4E}, {"m", 0x4D}, {"0", 0x30}, {"1", 0x31},
            {"2", 0x32}, {"3", 0x33}, {"4", 0x34}, {"5", 0x35},
            {"6", 0x36}, {"7", 0x37}, {"8", 0x38}, {"9", 0x39},
            {"space", 0x20}
        };

        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            SourceInitialized += MainWindow_SourceInitialized;
            InitializeTrayIcon();
            EnsureSettingsDir();
            LoadAllSettings();
            SwitchTab("humbled");
            UpdateUI();
            UpdateILUI();
            UpdateLDUI();
            UpdateBHUI();
            UpdatePSUI();
            UpdateACUI();
            UpdateAJUI();
            UpdateFOVUI();
            UpdateCBKeyUI("CBMelee", _cbMeleeKey);
            UpdateCBKeyUI("CBFruta", _cbFrutaKey);
            UpdateCBKeyUI("CBEspada", _cbEspadaKey);
            UpdateCBKeyUI("CBGun", _cbGunKey);
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;
        }

        [DllImport("user32.dll")]
        private static extern ushort MapVirtualKey(uint uCode, uint uMapType);

        private const uint MAPVK_VK_TO_VSC = 0;

        
        private static void SendKeyDown(ushort vk)
        {
            ushort scanCode = MapVirtualKey(vk, MAPVK_VK_TO_VSC);
            var inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = vk;
            inputs[0].u.ki.wScan = scanCode;
            inputs[0].u.ki.dwFlags = KEYEVENTF_SCANCODE;
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;
            SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        }

        private static void SendKeyUp(ushort vk)
        {
            ushort scanCode = MapVirtualKey(vk, MAPVK_VK_TO_VSC);
            var inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = vk;
            inputs[0].u.ki.wScan = scanCode;
            inputs[0].u.ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP;
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;
            SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        }

        private static void SendKeyPress(ushort vk)
        {
            ushort scanCode = MapVirtualKey(vk, MAPVK_VK_TO_VSC);
            var inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = vk;
            inputs[0].u.ki.wScan = scanCode;
            inputs[0].u.ki.dwFlags = KEYEVENTF_SCANCODE;
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = vk;
            inputs[1].u.ki.wScan = scanCode;
            inputs[1].u.ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP;
            inputs[1].u.ki.time = 0;
            inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

            SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        }

        private void SendComboKeys()
        {
            if (!_macroActivated) return;
            try
            {
                SendKeyPress(0x51);
                Thread.Sleep(70);
                RunMovementMacro();
            }
            catch { }
        }

        
        
        private void SendAutoClickerCombo()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        private void SendAutoJumpCombo()
        {
            if (_vkMap.TryGetValue("space", out ushort spcVk))
            {
                SendKeyDown(spcVk);
                Thread.Sleep(10);
                SendKeyUp(spcVk);
            }
        }
    
        private void SendBunnyHopCombo()
        {
            if (_vkMap.TryGetValue("v", out ushort vk))
            {
                SendKeyDown(vk);
                Thread.Sleep(50);
                SendKeyUp(vk);
            }
            if (_vkMap.TryGetValue(_bhComboKey, out ushort bhKeyVk))
            {
                while ((GetAsyncKeyState(bhKeyVk) & 0x8000) != 0 && _bhActivated)
                {
                    if (_vkMap.TryGetValue("c", out ushort cVk))
                    {
                        SendKeyDown(cVk);
                        Thread.Sleep(30);
                        SendKeyUp(cVk);
                    }
                    if (_vkMap.TryGetValue("space", out ushort spcVk))
                    {
                        SendKeyDown(spcVk);
                        Thread.Sleep(20);
                        SendKeyUp(spcVk);
                    }
                    Thread.Sleep(40);
                }
            }
        }

        private void SendPillarSlideForwardCombo()
        {
            if (_vkMap.TryGetValue("w", out ushort wVk) && _vkMap.TryGetValue("c", out ushort cVk) && _vkMap.TryGetValue("space", out ushort spcVk))
            {
                SendKeyDown(wVk); Thread.Sleep(73);
                SendKeyDown(cVk); Thread.Sleep(219);
                SendKeyUp(cVk); Thread.Sleep(18);
                SendKeyDown(spcVk); Thread.Sleep(128);
                SendKeyUp(spcVk); Thread.Sleep(27);
                SendKeyUp(wVk); Thread.Sleep(27);
                SendKeyDown(cVk); Thread.Sleep(173);
                SendKeyUp(cVk);
            }
        }

        private void SendPillarSlideBackwardCombo()
        {
            if (_vkMap.TryGetValue("s", out ushort sVk) && _vkMap.TryGetValue("c", out ushort cVk) && _vkMap.TryGetValue("space", out ushort spcVk))
            {
                SendKeyDown(sVk); Thread.Sleep(62);
                SendKeyDown(cVk); Thread.Sleep(266);
                SendKeyUp(sVk); Thread.Sleep(47);
                SendKeyDown(spcVk); Thread.Sleep(15);
                SendKeyUp(cVk); Thread.Sleep(125);
                SendKeyUp(spcVk); Thread.Sleep(63);
                SendKeyDown(cVk); Thread.Sleep(187);
                SendKeyUp(cVk);
            }
        }

        private void SendHumbledCombo()
        {
            if (!_macroActivated) return;
            try
            {
                if (_vkMap.TryGetValue("q", out ushort qVk))
                    SendKeyPress(qVk);
                Thread.Sleep(70);
                if (_vkMap.TryGetValue("0", out ushort zeroVk))
                    SendKeyPress(zeroVk);
                Thread.Sleep(70);
                RunMovementMacro();
            }
            catch { }
        }

        private void SendInstantLeeCombo()
        {
            if (!_ilActivated) return;
            try
            {
                if (_vkMap.TryGetValue("q", out ushort qVk))
                    SendKeyPress(qVk);
                Thread.Sleep(10);
                RunInstantLeeMovement();
            }
            catch { }
        }

        private void SendLethalDashCombo()
        {
            if (!_ldActivated) return;
            try
            {
                if (_vkMap.TryGetValue("q", out ushort qVk))
                    SendKeyPress(qVk);
                Thread.Sleep(40);
                if (_vkMap.TryGetValue("q", out ushort qVk2))
                    SendKeyPress(qVk2);
                Thread.Sleep(20);
                if (_vkMap.TryGetValue("space", out ushort spaceVk))
                    SendKeyPress(spaceVk);
                Thread.Sleep(40);
                if (_vkMap.TryGetValue("space", out ushort spaceVk2))
                    SendKeyPress(spaceVk2);
                Thread.Sleep(_ldKeyDelay);
                RunLethalDashMovement();
            }
            catch { }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            try
            {
                _trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.AppContext.BaseDirectory + "Leitostrap Macro V1.0.0 Beta.exe");
            }
            catch
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            _trayIcon.Text = "Leitostrap Macro V1.0.0 Beta";
            _trayIcon.Visible = false;

            var menu = new System.Windows.Forms.ContextMenuStrip();
            var showItem = new System.Windows.Forms.ToolStripMenuItem("Show Leitostrap");
            showItem.Click += (s, args) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                Focus();
            };

            var statusItem = new System.Windows.Forms.ToolStripMenuItem("Status: OFF");
            statusItem.Enabled = false;

            var sep1 = new System.Windows.Forms.ToolStripSeparator();

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (s, args) =>
            {
                _isClosing = true;
                StopComboThread();
                SaveAllSettings();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                Application.Current.Shutdown();
            };

            menu.Items.Add(showItem);
            menu.Items.Add(statusItem);
            menu.Items.Add(sep1);
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (s, args) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                Focus();
            };
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Focus();
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private void UpdateTrayStatus()
        {
            if (_trayIcon?.ContextMenuStrip?.Items.Count > 1)
            {
                var statusItem = _trayIcon.ContextMenuStrip.Items[1] as System.Windows.Forms.ToolStripMenuItem;
                if (statusItem != null)
                {
                    bool anyActive = _macroActivated || _ilActivated || _ldActivated || _bhActivated || _psActivated || _acActivated || _ajActivated;
                    statusItem.Text = anyActive ? "Status: ON" : "Status: OFF";
                    statusItem.ForeColor = anyActive
                        ? System.Drawing.Color.FromArgb(255, 200, 200, 200)
                        : System.Drawing.Color.FromArgb(255, 100, 100, 100);
                }
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                Hide();
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = true;
                    _trayIcon.ShowBalloonTip(1500, "Leitostrap Macro", "Running in background", System.Windows.Forms.ToolTipIcon.Info);
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            RootBorder.BeginAnimation(OpacityProperty, fadeIn);

            var cardFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(450));
            cardFade.BeginTime = TimeSpan.FromMilliseconds(200);
            cardFade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            GameCard_TSB.BeginAnimation(OpacityProperty, cardFade);

            var slideUp = new DoubleAnimation(15, 0, TimeSpan.FromMilliseconds(400));
            slideUp.BeginTime = TimeSpan.FromMilliseconds(150);
            slideUp.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            // CardTranslate removed after game selection restructure

            GameSelectionScreen.Opacity = 0;
            var screenFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
            screenFade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            GameSelectionScreen.BeginAnimation(OpacityProperty, screenFade);
        }

        private void AnimateToMacroCenter()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
            fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            fadeOut.Completed += (s, args) =>
            {
                GameSelectionScreen.Visibility = Visibility.Collapsed;
                MacroCenterScreen.Visibility = Visibility.Visible;
                MacroCenterScreen.Opacity = 0;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
                fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                MacroCenterScreen.BeginAnimation(OpacityProperty, fadeIn);
            };
            GameSelectionScreen.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void AnimateToGameSelection()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
            fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            fadeOut.Completed += (s, args) =>
            {
                MacroCenterScreen.Visibility = Visibility.Collapsed;
                GameSelectionScreen.Visibility = Visibility.Visible;
                GameSelectionScreen.Opacity = 0;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
                fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                GameSelectionScreen.BeginAnimation(OpacityProperty, fadeIn);
            };
            MacroCenterScreen.BeginAnimation(OpacityProperty, fadeOut);
        }

        
                private void GameCard_Rivals_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabHumbled.Visibility = Visibility.Collapsed;
            TabInstantLee.Visibility = Visibility.Collapsed;
            TabLethalDash.Visibility = Visibility.Collapsed;
            TabBunnyHop.Visibility = Visibility.Visible;
            TabPillarSlide.Visibility = Visibility.Visible;
            TabAutoClicker.Visibility = Visibility.Collapsed;
            TabAutoJump.Visibility = Visibility.Collapsed;
            TabFOVChanger.Visibility = Visibility.Collapsed;
            SwitchTab("bunnyhop");
            AnimateToMacroCenter();
        }

        private void TabBunnyHop_Click(object sender, MouseButtonEventArgs e) => SwitchTab("bunnyhop");
        private void TabPillarSlide_Click(object sender, MouseButtonEventArgs e) => SwitchTab("pillarslide");

        
        private void BtnACSetKey_Click(object sender, RoutedEventArgs e)
        {
            _listeningTarget = "autoclicker";
            _isListeningForKey = true;
            ModalTitle.Text = "Set AutoClicker Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BtnAJSetKey_Click(object sender, RoutedEventArgs e)
        {
            _listeningTarget = "autojump";
            _isListeningForKey = true;
            ModalTitle.Text = "Set AutoJump Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void ACToggle_Checked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; ToggleACMacro(true); }
        private void ACToggle_Unchecked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; ToggleACMacro(false); }
        private void AJToggle_Checked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; ToggleAJMacro(true); }
        private void AJToggle_Unchecked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; ToggleAJMacro(false); }

        private void ACCPSSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || ACCPSValue == null) return;
            _acCPS = (int)e.NewValue;
            ACCPSValue.Text = _acCPS.ToString();
            SaveACSettings();
        }

        private void AJDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || AJDelayValue == null) return;
            _ajDelay = (int)e.NewValue;
            AJDelayValue.Text = _ajDelay.ToString();
            SaveAJSettings();
        }

        private void ToggleACMacro(bool activate)
        {
            _acActivated = activate;
            UpdateTrayStatus();
            try { if (_acActivated) new Thread(() => { Beep(800, 80); Thread.Sleep(50); Beep(1200, 100); }).Start(); else new Thread(() => { Beep(1000, 80); Thread.Sleep(50); Beep(600, 120); }).Start(); } catch { }
            if (_acActivated) { StartComboThread(); ShowNotification("AUTOCLICKER ON", "Macro is active"); }
            else { StopComboThread(); ShowNotification("AUTOCLICKER OFF", "Macro is inactive"); }
            ACMacroEnabledText.Text = "Status: " + (_acActivated ? "ON" : "OFF");
            TabAutoClickerStatus.Text = _acActivated ? "ON" : "OFF";
            TabAutoClickerStatus.Foreground = _acActivated ? new SolidColorBrush(ColorFromHex("#FF55FF55")) : new SolidColorBrush(ColorFromHex("#FF444444"));
            ACToggleSwitch.IsChecked = _acActivated;
        }

        private void ToggleAJMacro(bool activate)
        {
            _ajActivated = activate;
            UpdateTrayStatus();
            try { if (_ajActivated) new Thread(() => { Beep(800, 80); Thread.Sleep(50); Beep(1200, 100); }).Start(); else new Thread(() => { Beep(1000, 80); Thread.Sleep(50); Beep(600, 120); }).Start(); } catch { }
            if (_ajActivated) { StartComboThread(); ShowNotification("AUTOJUMP ON", "Macro is active"); }
            else { StopComboThread(); ShowNotification("AUTOJUMP OFF", "Macro is inactive"); }
            AJMacroEnabledText.Text = "Status: " + (_ajActivated ? "ON" : "OFF");
            TabAutoJumpStatus.Text = _ajActivated ? "ON" : "OFF";
            TabAutoJumpStatus.Foreground = _ajActivated ? new SolidColorBrush(ColorFromHex("#FF55FF55")) : new SolidColorBrush(ColorFromHex("#FF444444"));
            AJToggleSwitch.IsChecked = _ajActivated;
        }

        private void UpdateACUI()
        {
            ACMacroEnabledText.Text = "Status: " + (_acActivated ? "ON" : "OFF");
            TabAutoClickerStatus.Text = _acActivated ? "ON" : "OFF";
            TabAutoClickerStatus.Foreground = _acActivated ? new SolidColorBrush(ColorFromHex("#FF55FF55")) : new SolidColorBrush(ColorFromHex("#FF444444"));
            ACToggleSwitch.IsChecked = _acActivated;
            ACComboKeyDisplay.Text = _acKey.ToUpper();
            if (ACCPSSlider != null) ACCPSSlider.Value = _acCPS;
            if (ACCPSValue != null) ACCPSValue.Text = _acCPS.ToString();
        }

        private void UpdateAJUI()
        {
            AJMacroEnabledText.Text = "Status: " + (_ajActivated ? "ON" : "OFF");
            TabAutoJumpStatus.Text = _ajActivated ? "ON" : "OFF";
            TabAutoJumpStatus.Foreground = _ajActivated ? new SolidColorBrush(ColorFromHex("#FF55FF55")) : new SolidColorBrush(ColorFromHex("#FF444444"));
            AJToggleSwitch.IsChecked = _ajActivated;
            AJComboKeyDisplay.Text = _ajKey.ToUpper();
            if (AJDelaySlider != null) AJDelaySlider.Value = _ajDelay;
            if (AJDelayValue != null) AJDelayValue.Text = _ajDelay.ToString();
        }
    
        private void BtnBHSetComboKey_Click(object sender, RoutedEventArgs e)
        {
            _listeningTarget = "bunnyhop";
            _isListeningForKey = true;
            ModalTitle.Text = "Set Combo Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BtnPSSetForwardKey_Click(object sender, RoutedEventArgs e)
        {
            _listeningTarget = "psforward";
            _isListeningForKey = true;
            ModalTitle.Text = "Set Forward Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BtnPSSetBackwardKey_Click(object sender, RoutedEventArgs e)
        {
            _listeningTarget = "psbackward";
            _isListeningForKey = true;
            ModalTitle.Text = "Set Backward Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BHToggle_Checked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; ToggleBHMacro(true); }
        private void BHToggle_Unchecked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; ToggleBHMacro(false); }
        private void PSToggle_Checked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; TogglePSMacro(true); }
        private void PSToggle_Unchecked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; TogglePSMacro(false); }

        private void ToggleBHMacro(bool activate)
        {
            _bhActivated = activate;
            UpdateTrayStatus();
            try { if (_bhActivated) new Thread(() => { Beep(800, 80); Thread.Sleep(50); Beep(1200, 100); }).Start(); else new Thread(() => { Beep(1000, 80); Thread.Sleep(50); Beep(600, 120); }).Start(); } catch { }
            if (_bhActivated) { StartComboThread(); ShowNotification("BUNNY HOP ON", "Macro is now active"); }
            else { StopComboThread(); ShowNotification("BUNNY HOP OFF", "Macro is now inactive"); }
            BHMacroEnabledText.Text = "Status: " + (_bhActivated ? "ON" : "OFF");
            TabBunnyHopStatus.Text = _bhActivated ? "ON" : "OFF";
            TabBunnyHopStatus.Foreground = _bhActivated ? new SolidColorBrush(ColorFromHex("#FF55FF55")) : new SolidColorBrush(ColorFromHex("#FF444444"));
            BHToggleSwitch.IsChecked = _bhActivated;
        }

        private void TogglePSMacro(bool activate)
        {
            _psActivated = activate;
            UpdateTrayStatus();
            try { if (_psActivated) new Thread(() => { Beep(800, 80); Thread.Sleep(50); Beep(1200, 100); }).Start(); else new Thread(() => { Beep(1000, 80); Thread.Sleep(50); Beep(600, 120); }).Start(); } catch { }
            if (_psActivated) { StartComboThread(); ShowNotification("PILLAR SLIDE ON", "Macro is now active"); }
            else { StopComboThread(); ShowNotification("PILLAR SLIDE OFF", "Macro is now inactive"); }
            PSMacroEnabledText.Text = "Status: " + (_psActivated ? "ON" : "OFF");
            TabPillarSlideStatus.Text = _psActivated ? "ON" : "OFF";
            TabPillarSlideStatus.Foreground = _psActivated ? new SolidColorBrush(ColorFromHex("#FF55FF55")) : new SolidColorBrush(ColorFromHex("#FF444444"));
            PSToggleSwitch.IsChecked = _psActivated;
        }

        private void UpdateBHUI()
        {
            BHMacroEnabledText.Text = "Status: " + (_bhActivated ? "ON" : "OFF");
            TabBunnyHopStatus.Text = _bhActivated ? "ON" : "OFF";
            TabBunnyHopStatus.Foreground = _bhActivated ? new SolidColorBrush(ColorFromHex("#FF55FF55")) : new SolidColorBrush(ColorFromHex("#FF444444"));
            BHToggleSwitch.IsChecked = _bhActivated;
            BHComboKeyDisplay.Text = _bhComboKey.ToUpper();
        }

        private void UpdatePSUI()
        {
            PSMacroEnabledText.Text = "Status: " + (_psActivated ? "ON" : "OFF");
            TabPillarSlideStatus.Text = _psActivated ? "ON" : "OFF";
            TabPillarSlideStatus.Foreground = _psActivated ? new SolidColorBrush(ColorFromHex("#FF55FF55")) : new SolidColorBrush(ColorFromHex("#FF444444"));
            PSToggleSwitch.IsChecked = _psActivated;
            PSForwardKeyDisplay.Text = _psForwardKey.ToUpper();
            PSBackwardKeyDisplay.Text = _psBackwardKey.ToUpper();
        }

                private void GameCard_TSB_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabHumbled.Visibility = Visibility.Visible;
            TabInstantLee.Visibility = Visibility.Visible;
            TabLethalDash.Visibility = Visibility.Visible;
            TabBunnyHop.Visibility = Visibility.Collapsed;
            TabPillarSlide.Visibility = Visibility.Collapsed;
            TabAutoClicker.Visibility = Visibility.Collapsed;
            TabAutoJump.Visibility = Visibility.Collapsed;
            TabFOVChanger.Visibility = Visibility.Collapsed;
            SwitchTab("humbled");
            AnimateToMacroCenter();
        }
        private void BtnBack_Click(object sender, RoutedEventArgs e) => AnimateToGameSelection();

        private void TabHumbled_Click(object sender, MouseButtonEventArgs e) => SwitchTab("humbled");
        private void TabInstantLee_Click(object sender, MouseButtonEventArgs e) => SwitchTab("instantlee");
        private void TabLethalDash_Click(object sender, MouseButtonEventArgs e) => SwitchTab("lethaldash");

        private void SwitchTab(string tab)
        {
            _activeMacroTab = tab;

            PanelHumbled.Visibility = tab == "humbled" ? Visibility.Visible : Visibility.Collapsed;
            PanelInstantLee.Visibility = tab == "instantlee" ? Visibility.Visible : Visibility.Collapsed;
            PanelLethalDash.Visibility = tab == "lethaldash" ? Visibility.Visible : Visibility.Collapsed;
            PanelBunnyHop.Visibility = tab == "bunnyhop" ? Visibility.Visible : Visibility.Collapsed;
            PanelPillarSlide.Visibility = tab == "pillarslide" ? Visibility.Visible : Visibility.Collapsed;
            PanelAutoClicker.Visibility = tab == "autoclicker" ? Visibility.Visible : Visibility.Collapsed;
            PanelAutoJump.Visibility = tab == "autojump" ? Visibility.Visible : Visibility.Collapsed;
            PanelFOVChanger.Visibility = tab == "fovchanger" ? Visibility.Visible : Visibility.Collapsed;
            PanelAutoCombo.Visibility = tab == "autocombo" ? Visibility.Visible : Visibility.Collapsed;

            TabHumbled.Background = tab == "humbled" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabHumbled.BorderBrush = tab == "humbled" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabHumbled.BorderThickness = tab == "humbled" ? new Thickness(3,0,0,0) : new Thickness(0);

            TabInstantLee.Background = tab == "instantlee" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabInstantLee.BorderBrush = tab == "instantlee" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabInstantLee.BorderThickness = tab == "instantlee" ? new Thickness(3,0,0,0) : new Thickness(0);

            TabLethalDash.Background = tab == "lethaldash" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabLethalDash.BorderBrush = tab == "lethaldash" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabLethalDash.BorderThickness = tab == "lethaldash" ? new Thickness(3,0,0,0) : new Thickness(0);

            TabBunnyHop.Background = tab == "bunnyhop" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabBunnyHop.BorderBrush = tab == "bunnyhop" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabBunnyHop.BorderThickness = tab == "bunnyhop" ? new Thickness(3,0,0,0) : new Thickness(0);

            TabPillarSlide.Background = tab == "pillarslide" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabPillarSlide.BorderBrush = tab == "pillarslide" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabPillarSlide.BorderThickness = tab == "pillarslide" ? new Thickness(3,0,0,0) : new Thickness(0);

            TabAutoClicker.Background = tab == "autoclicker" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabAutoClicker.BorderBrush = tab == "autoclicker" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabAutoClicker.BorderThickness = tab == "autoclicker" ? new Thickness(3,0,0,0) : new Thickness(0);

            TabAutoJump.Background = tab == "autojump" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabAutoJump.BorderBrush = tab == "autojump" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabAutoJump.BorderThickness = tab == "autojump" ? new Thickness(3,0,0,0) : new Thickness(0);

            TabFOVChanger.Background = tab == "fovchanger" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabFOVChanger.BorderBrush = tab == "fovchanger" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabFOVChanger.BorderThickness = tab == "fovchanger" ? new Thickness(3,0,0,0) : new Thickness(0);

            TabAutoCombo.Background = tab == "autocombo" ? new SolidColorBrush(ColorFromHex("#151515")) : new SolidColorBrush(Colors.Transparent);
            TabAutoCombo.BorderBrush = tab == "autocombo" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabAutoCombo.BorderThickness = tab == "autocombo" ? new Thickness(3,0,0,0) : new Thickness(0);
            TabFOVChanger.BorderBrush = tab == "fovchanger" ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
            TabFOVChanger.BorderThickness = tab == "fovchanger" ? new Thickness(3,0,0,0) : new Thickness(0);

            // Animate content
            if (!_potatoMode) {
                var panels = new[] { PanelHumbled, PanelInstantLee, PanelLethalDash, PanelBunnyHop, PanelPillarSlide, PanelAutoClicker, PanelAutoJump, PanelFOVChanger };
                foreach(var p in panels) {
                    if (p.Visibility == Visibility.Visible) {
                        p.Opacity = 0;
                        p.RenderTransform = new System.Windows.Media.TranslateTransform(0, -10);
                        var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                        var slide = new System.Windows.Media.Animation.DoubleAnimation(-10, 0, TimeSpan.FromMilliseconds(200));
                        p.BeginAnimation(UIElement.OpacityProperty, fade);
                        p.RenderTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
                    }
                }
            }
        }
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        [DllImport("user32.dll")]
        private static extern void keybd_event(ushort bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
            {
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                this.WindowState = WindowState.Maximized;
            }
        }
    

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            _isClosing = true;
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            StopComboThread();
            SaveAllSettings();
            Application.Current.Shutdown();
        }

        private void Overlay_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isListeningForKey)
            {
                _isListeningForKey = false;
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
                fadeOut.Completed += (s, args) => Overlay.Visibility = Visibility.Collapsed;
                Overlay.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void ModalCancel_Click(object sender, RoutedEventArgs e)
        {
            _isListeningForKey = false;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, args) => Overlay.Visibility = Visibility.Collapsed;
            Overlay.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void PreviewKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (_isListeningForKey)
            {
                e.Handled = true;
                _isListeningForKey = false;

                string keyName = e.Key == Key.None ? "" : e.Key.ToString().ToLower();
                if (string.IsNullOrEmpty(keyName)) return;

                if (_listeningTarget == "humbled")
                {
                    _comboKey = keyName;
                    ComboKeyDisplay.Text = _comboKey.ToUpper();
                    StopComboThread();
                    if (_macroActivated) StartComboThread();
                }
                else if (_listeningTarget == "instantlee")
                {
                    _ilComboKey = keyName;
                    ILComboKeyDisplay.Text = _ilComboKey.ToUpper();
                    StopComboThread();
                    if (_ilActivated) StartComboThread();
                }
                else if (_listeningTarget == "lethaldash")
                {
                    _ldComboKey = keyName;
                    LDComboKeyDisplay.Text = _ldComboKey.ToUpper();
                    StopComboThread();
                    if (_ldActivated) StartComboThread();
                }
                
                else if (_listeningTarget == "autoclicker")
                {
                    _acKey = keyName;
                    ACComboKeyDisplay.Text = _acKey.ToUpper();
                    StopComboThread();
                    if (_acActivated) StartComboThread();
                    SaveACSettings();
                }
                else if (_listeningTarget == "autojump")
                {
                    _ajKey = keyName;
                    AJComboKeyDisplay.Text = _ajKey.ToUpper();
                    StopComboThread();
                    if (_ajActivated) StartComboThread();
                    SaveAJSettings();
                }
                    else if (_listeningTarget == "bunnyhop")
                {
                    _bhComboKey = keyName;
                    BHComboKeyDisplay.Text = _bhComboKey.ToUpper();
                    StopComboThread();
                    if (_bhActivated) StartComboThread();
                }
                else if (_listeningTarget == "psforward")
                {
                    _psForwardKey = keyName;
                    PSForwardKeyDisplay.Text = _psForwardKey.ToUpper();
                    StopComboThread();
                    if (_psActivated) StartComboThread();
                }
                else if (_listeningTarget == "psbackward")
                {
                    _psBackwardKey = keyName;
                    PSBackwardKeyDisplay.Text = _psBackwardKey.ToUpper();
                    StopComboThread();
                    if (_psActivated) StartComboThread();
                }
                else if (_listeningTarget == "autocombo")
                {
                    _cbKey = keyName;
                    CBKeyDisplay.Text = _cbKey.ToUpper();
                }


                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
                fadeOut.Completed += (s, args) => Overlay.Visibility = Visibility.Collapsed;
                Overlay.BeginAnimation(OpacityProperty, fadeOut);

                SaveAllSettings();
            }
        }

        private void ToggleMacro(bool activate)
        {
            _macroActivated = activate;
            UpdateUI();
            UpdateTrayStatus();

            try
            {
                if (_macroActivated)
                    new Thread(() => { Beep(800, 80); Thread.Sleep(50); Beep(1200, 100); }).Start();
                else
                    new Thread(() => { Beep(1000, 80); Thread.Sleep(50); Beep(600, 120); }).Start();
            }
            catch { }

            if (_macroActivated)
            {
                StartComboThread();
                ShowNotification("HUMBLED TWISTED ON", "Macro is now active");
            }
            else
            {
                StopComboThread();
                ShowNotification("HUMBLED TWISTED OFF", "Macro is now inactive");
            }
        }

        private void ToggleILMacro(bool activate)
        {
            _ilActivated = activate;
            UpdateILUI();
            UpdateTrayStatus();

            try
            {
                if (_ilActivated)
                    new Thread(() => { Beep(800, 80); Thread.Sleep(50); Beep(1200, 100); }).Start();
                else
                    new Thread(() => { Beep(1000, 80); Thread.Sleep(50); Beep(600, 120); }).Start();
            }
            catch { }

            if (_ilActivated)
            {
                StartComboThread();
                ShowNotification("INSTANT LEE ON", "Macro is now active");
            }
            else
            {
                StopComboThread();
                ShowNotification("INSTANT LEE OFF", "Macro is now inactive");
            }
        }

        private void ToggleLDMacro(bool activate)
        {
            _ldActivated = activate;
            UpdateLDUI();
            UpdateBHUI();
            UpdatePSUI();
            UpdateACUI();
            UpdateAJUI();
            UpdateFOVUI();
            UpdateTrayStatus();

            try
            {
                if (_ldActivated)
                    new Thread(() => { Beep(800, 80); Thread.Sleep(50); Beep(1200, 100); }).Start();
                else
                    new Thread(() => { Beep(1000, 80); Thread.Sleep(50); Beep(600, 120); }).Start();
            }
            catch { }

            if (_ldActivated)
            {
                StartComboThread();
                ShowNotification("LETHAL DASH ON", "Macro is now active");
            }
            else
            {
                StopComboThread();
                ShowNotification("LETHAL DASH OFF", "Macro is now inactive");
            }
        }

        private void StartComboThread()
        {
            if (_comboThreadRunning) return;
            _comboThreadRunning = true;

            _comboThread = new Thread(() =>
            {
                bool wasPressedH = false;
                bool wasPressedIL = false;
                bool wasPressedLD = false;
                while (_comboThreadRunning)
                {
                    try
                    {
                        if (_macroActivated && _vkMap.TryGetValue(_comboKey, out ushort vkH))
                        {
                            short state = GetAsyncKeyState(vkH);
                            bool isPressed = (state & 0x8000) != 0;
                            if (isPressed && !wasPressedH)
                            {
                                wasPressedH = true;
                                SendHumbledCombo();
                                Thread.Sleep(300);
                            }
                            else if (!isPressed) wasPressedH = false;
                        }

                        if (_ilActivated && _vkMap.TryGetValue(_ilComboKey, out ushort vkIL))
                        {
                            short state = GetAsyncKeyState(vkIL);
                            bool isPressed = (state & 0x8000) != 0;
                            if (isPressed && !wasPressedIL)
                            {
                                wasPressedIL = true;
                                SendInstantLeeCombo();
                                Thread.Sleep(300);
                            }
                            else if (!isPressed) wasPressedIL = false;
                        }

                        if (_ldActivated && _vkMap.TryGetValue(_ldComboKey, out ushort vkLD))
                        {
                            short state = GetAsyncKeyState(vkLD);
                            bool isPressed = (state & 0x8000) != 0;
                            if (isPressed && !wasPressedLD)
                            {
                                wasPressedLD = true;
                                SendLethalDashCombo();
                                Thread.Sleep(300);
                            }
                            else if (!isPressed) wasPressedLD = false;
                        }
                    }
                    catch { }

                        
                        if (_acActivated && _vkMap.TryGetValue(_acKey, out ushort vkAC))
                        {
                            if ((GetAsyncKeyState(vkAC) & 0x8000) != 0)
                            {
                                SendAutoClickerCombo();
                                int sleepTime = Math.Max(1, 1000 / _acCPS);
                                Thread.Sleep(sleepTime);
                                continue;
                            }
                        }
                        if (_ajActivated && _vkMap.TryGetValue(_ajKey, out ushort vkAJ))
                        {
                            if ((GetAsyncKeyState(vkAJ) & 0x8000) != 0)
                            {
                                SendAutoJumpCombo();
                                Thread.Sleep(_ajDelay);
                                continue;
                            }
                        }
    
                        if (_bhActivated && _vkMap.TryGetValue(_bhComboKey, out ushort vkBH))
                        {
                            short state = GetAsyncKeyState(vkBH);
                            if ((state & 0x8000) != 0)
                            {
                                SendBunnyHopCombo();
                                Thread.Sleep(100);
                            }
                        }
                        if (_psActivated && _vkMap.TryGetValue(_psForwardKey, out ushort vkPSF))
                        {
                            short state = GetAsyncKeyState(vkPSF);
                            if ((state & 0x8000) != 0)
                            {
                                SendPillarSlideForwardCombo();
                                Thread.Sleep(100);
                            }
                        }
                        if (_psActivated && _vkMap.TryGetValue(_psBackwardKey, out ushort vkPSB))
                        {
                            short state = GetAsyncKeyState(vkPSB);
                            if ((state & 0x8000) != 0)
                            {
                                SendPillarSlideBackwardCombo();
                                Thread.Sleep(100);
                            }
                        }

                        if (_fovActivated)
                        {
                            ApplyFOV(_fovValue);
                        }

                        if (_cbActivated && _vkMap.TryGetValue(_cbKey, out ushort vkCB))
                        {
                            short state = GetAsyncKeyState(vkCB);
                            bool isNowPressed = (state & 0x8000) != 0;
                            if (isNowPressed)
                            {
                                SendAutoComboSequence();
                                Thread.Sleep(500);
                                continue;
                            }
                        }

                    Thread.Sleep(10);
                }
            });
            _comboThread.IsBackground = true;
            _comboThread.Start();
        }

        private void StopComboThread()
        {
            if (_macroActivated || _ilActivated || _ldActivated || _acActivated || _ajActivated || _bhActivated || _psActivated || _cbActivated || _fovActivated)
                return;
            _comboThreadRunning = false;
            if (_comboThread != null)
            {
                _comboThread.Join(300);
                _comboThread = null;
            }
        }

        private void ToggleMacroSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ToggleMacro(true);
        }

        private void ToggleMacroSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ToggleMacro(false);
        }

        private void ILToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ToggleILMacro(true);
        }

        private void ILToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ToggleILMacro(false);
        }

        private void LDToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ToggleLDMacro(true);
        }

        private void LDToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ToggleLDMacro(false);
        }

        private void BtnSetComboKey_Click(object sender, RoutedEventArgs e)
        {
            _listeningTarget = "humbled";
            _isListeningForKey = true;
            ModalTitle.Text = "Set Combo Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BtnILSetComboKey_Click(object sender, RoutedEventArgs e)
        {
            _listeningTarget = "instantlee";
            _isListeningForKey = true;
            ModalTitle.Text = "Set Combo Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BtnLDSetComboKey_Click(object sender, RoutedEventArgs e)
        {
            _listeningTarget = "lethaldash";
            _isListeningForKey = true;
            ModalTitle.Text = "Set Combo Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _sensitivity = Math.Round(SensitivitySlider.Value, 2);
            SensitivityValue.Text = _sensitivity.ToString("F2");
            SaveHumbledSettings();
        }

        private void LoopSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _loopCount = (int)LoopSlider.Value;
            LoopValue.Text = _loopCount.ToString();
            SaveHumbledSettings();
        }

        private void ILSensSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _ilSensitivity = Math.Round(ILSensSlider.Value, 2);
            ILSensValue.Text = _ilSensitivity.ToString("F2");
            SaveILSettings();
        }

        private void ILStepsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _ilSteps = (int)ILStepsSlider.Value;
            ILStepsValue.Text = _ilSteps.ToString();
            SaveILSettings();
        }

        private void LDSensSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _ldSensitivity = Math.Round(LDSensSlider.Value, 2);
            LDSensValue.Text = _ldSensitivity.ToString("F2");
            SaveLDSettings(); SaveBHSettings(); SavePSSettings(); SaveACSettings(); SaveAJSettings(); SaveFOVSettings(); SaveBFSettings();
        }

        private void LDStepsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _ldSteps = (int)LDStepsSlider.Value;
            LDStepsValue.Text = _ldSteps.ToString();
            SaveLDSettings(); SaveBHSettings(); SavePSSettings(); SaveACSettings(); SaveAJSettings(); SaveFOVSettings(); SaveBFSettings();
        }

        private void LDDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _ldKeyDelay = (int)LDDelaySlider.Value;
            LDDelayValue.Text = _ldKeyDelay.ToString();
            SaveLDSettings(); SaveBHSettings(); SavePSSettings(); SaveACSettings(); SaveAJSettings(); SaveFOVSettings(); SaveBFSettings();
        }

        private void ILFlickCurved_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { _ilFlickType = "Curved"; SaveILSettings(); UpdateILUI(); }
        private void ILFlickLinear_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { _ilFlickType = "Linear"; SaveILSettings(); UpdateILUI(); }
        
        private void LDFlickCurved_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { _ldFlickType = "Curved"; SaveLDSettings(); UpdateLDUI(); }
        private void LDFlickLinear_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { _ldFlickType = "Linear"; SaveLDSettings(); UpdateLDUI(); }

        private void UpdateUI()
        {
            if (_macroActivated)
            {
                StatusText.Text = "ACTIVE";
                StatusText.Foreground = new SolidColorBrush(Colors.White);
                StatusDot.Background = new SolidColorBrush(Colors.White);
                MacroStatusText.Text = "Running";
                MacroStatusText.Foreground = new SolidColorBrush(ColorFromHex("#FF444444"));
                MacroEnabledText.Text = "Status: ON";
                MacroEnabledText.Foreground = new SolidColorBrush(Colors.White);
                ToggleMacroSwitch.IsChecked = true;
                TabStatus.Text = "ON";
                TabStatus.Foreground = new SolidColorBrush(Colors.White);
                var dotAnim = new ColorAnimation(Colors.White, TimeSpan.FromMilliseconds(300));
                StatusDot.Background.BeginAnimation(SolidColorBrush.ColorProperty, dotAnim);
            }
            else
            {
                StatusText.Text = "INACTIVE";
                StatusText.Foreground = new SolidColorBrush(ColorFromHex("#FF333333"));
                StatusDot.Background = new SolidColorBrush(ColorFromHex("#FF222222"));
                MacroStatusText.Text = "Ready to launch";
                MacroStatusText.Foreground = new SolidColorBrush(ColorFromHex("#FF222222"));
                MacroEnabledText.Text = "Status: OFF";
                MacroEnabledText.Foreground = new SolidColorBrush(ColorFromHex("#FF333333"));
                ToggleMacroSwitch.IsChecked = false;
                TabStatus.Text = "OFF";
                TabStatus.Foreground = new SolidColorBrush(ColorFromHex("#FF444444"));
                var dotAnim = new ColorAnimation(ColorFromHex("#FF222222"), TimeSpan.FromMilliseconds(300));
                StatusDot.Background.BeginAnimation(SolidColorBrush.ColorProperty, dotAnim);
            }
            ComboKeyDisplay.Text = _comboKey.ToUpper();
        }

        private void UpdateILUI()
        {
            if (_ilActivated)
            {
                ILMacroEnabledText.Text = "Status: ON";
                ILMacroEnabledText.Foreground = new SolidColorBrush(Colors.White);
                ILToggleSwitch.IsChecked = true;
                TabInstantLeeStatus.Text = "ON";
                TabInstantLeeStatus.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                ILMacroEnabledText.Text = "Status: OFF";
                ILMacroEnabledText.Foreground = new SolidColorBrush(ColorFromHex("#FF333333"));
                ILToggleSwitch.IsChecked = false;
                TabInstantLeeStatus.Text = "OFF";
                TabInstantLeeStatus.Foreground = new SolidColorBrush(ColorFromHex("#FF444444"));
            }
            ILComboKeyDisplay.Text = _ilComboKey.ToUpper();
            if (ILFlickCurvedBtn != null && ILFlickLinearBtn != null) {
                var c = ColorFromHex("#1A1A1A"); var t = Colors.Transparent;
                var wc = Colors.White; var gc = ColorFromHex("#FF555555");
                var bc = ColorFromHex("#222222");
                ILFlickCurvedBtn.Background = new SolidColorBrush(_ilFlickType == "Curved" ? c : t);
                ILFlickCurvedBtn.BorderBrush = new SolidColorBrush(_ilFlickType == "Curved" ? wc : bc);
                ((TextBlock)ILFlickCurvedBtn.Child).Foreground = new SolidColorBrush(_ilFlickType == "Curved" ? wc : gc);
                ILFlickLinearBtn.Background = new SolidColorBrush(_ilFlickType == "Linear" ? c : t);
                ILFlickLinearBtn.BorderBrush = new SolidColorBrush(_ilFlickType == "Linear" ? wc : bc);
                ((TextBlock)ILFlickLinearBtn.Child).Foreground = new SolidColorBrush(_ilFlickType == "Linear" ? wc : gc);
            }
        }

        private void UpdateLDUI()
        {
            if (_ldActivated)
            {
                LDMacroEnabledText.Text = "Status: ON";
                LDMacroEnabledText.Foreground = new SolidColorBrush(Colors.White);
                LDToggleSwitch.IsChecked = true;
                TabLethalDashStatus.Text = "ON";
                TabLethalDashStatus.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                LDMacroEnabledText.Text = "Status: OFF";
                LDMacroEnabledText.Foreground = new SolidColorBrush(ColorFromHex("#FF333333"));
                LDToggleSwitch.IsChecked = false;
                TabLethalDashStatus.Text = "OFF";
                TabLethalDashStatus.Foreground = new SolidColorBrush(ColorFromHex("#FF444444"));
            }
            LDComboKeyDisplay.Text = _ldComboKey.ToUpper();
            if (LDFlickLinearBtn != null && LDFlickCurvedBtn != null) {
                var c = ColorFromHex("#1A1A1A"); var t = Colors.Transparent;
                var wc = Colors.White; var gc = ColorFromHex("#FF555555");
                var bc = ColorFromHex("#222222");
                LDFlickLinearBtn.Background = new SolidColorBrush(_ldFlickType == "Linear" ? c : t);
                LDFlickLinearBtn.BorderBrush = new SolidColorBrush(_ldFlickType == "Linear" ? wc : bc);
                ((TextBlock)LDFlickLinearBtn.Child).Foreground = new SolidColorBrush(_ldFlickType == "Linear" ? wc : gc);
                LDFlickCurvedBtn.Background = new SolidColorBrush(_ldFlickType == "Curved" ? c : t);
                LDFlickCurvedBtn.BorderBrush = new SolidColorBrush(_ldFlickType == "Curved" ? wc : bc);
                ((TextBlock)LDFlickCurvedBtn.Child).Foreground = new SolidColorBrush(_ldFlickType == "Curved" ? wc : gc);
            }
        }

        private static Color ColorFromHex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        private void RunMovementMacro()
        {
            if (!_macroActivated) return;
            try
            {
                double d1 = c1 * (b1 / _sensitivity);
                int j1 = (int)(Math.Floor(f1 * d1 * e1) * -1);
                int totalMovement = 0;

                for (int i = 0; i < _loopCount; i++)
                {
                    int k1 = j1 / _loopCount;
                    mouse_event(MOUSEEVENTF_MOVE, k1, 0, 0, IntPtr.Zero);
                    totalMovement += k1;
                    Thread.Sleep(1);
                }
                Thread.Sleep(30);

                int l1 = (int)(Math.Floor(g1 * d1 * e1));
                int m1 = 0;
                for (int i = 0; i < _loopCount; i++)
                {
                    if (m1 >= 133) break;
                    int n1 = l1 / _loopCount;
                    mouse_event(MOUSEEVENTF_MOVE, n1, 0, 0, IntPtr.Zero);
                    totalMovement += n1;
                    Thread.Sleep(10);
                    m1 += 15;
                }
                Thread.Sleep(30);

                for (int i = 0; i < _loopCount; i++)
                {
                    int o1 = -totalMovement / _loopCount;
                    mouse_event(MOUSEEVENTF_MOVE, o1, 0, 0, IntPtr.Zero);
                    Thread.Sleep(15);
                }
            }
            catch { }
        }

        private void RunInstantLeeMovement()
        {
            if (!_ilActivated) return;
            try
            {
                double scaleFactor = 0.185 * (0.29 / _ilSensitivity);
                int scaledFlick = (int)Math.Round(200 * scaleFactor * 30);
                int scaledCorrection = (int)Math.Round(30 * scaleFactor * 30);
                int scaledCameraY = (int)Math.Round(-40 * scaleFactor * 30);

                mouse_event(MOUSEEVENTF_MOVE, 0, scaledCameraY, 0, IntPtr.Zero);
                Thread.Sleep(10);

                if (_ilFlickType == "Linear")
                {
                    for (int i = 0; i < _ilSteps; i++)
                    {
                        int deltaX = (int)Math.Round((double)scaledFlick / _ilSteps);
                        mouse_event(MOUSEEVENTF_MOVE, deltaX, 0, 0, IntPtr.Zero);
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    double yAmplitude = scaledFlick * 0.2;
                    for (int i = 0; i < _ilSteps; i++)
                    {
                        double t = (double)(i + 1) / _ilSteps;
                        int deltaX = (int)Math.Round((double)scaledFlick / _ilSteps);
                        int deltaY = (int)Math.Round(yAmplitude * Math.Sin(t * Math.PI));
                        mouse_event(MOUSEEVENTF_MOVE, deltaX, deltaY, 0, IntPtr.Zero);
                        Thread.Sleep(10);
                    }
                }

                Thread.Sleep(15);

                int totalReturn = (scaledFlick + scaledCorrection) * -1;
                for (int i = 0; i < _ilSteps; i++)
                {
                    int step = totalReturn / _ilSteps;
                    mouse_event(MOUSEEVENTF_MOVE, step, 0, 0, IntPtr.Zero);
                    Thread.Sleep(15);
                }
            }
            catch { }
        }

        private void RunLethalDashMovement()
        {
            if (!_ldActivated) return;
            try
            {
                double d1 = 0.185 * (0.29 / _ldSensitivity);
                int turnPixels = (int)Math.Round(230 * d1 * 30);

                int stepPixels = turnPixels / _ldSteps;
                double yAmplitude = turnPixels * 0.1;

                for (int i = 0; i < _ldSteps; i++)
                {
                    int deltaX = stepPixels;
                    int deltaY = 0;
                    if (_ldFlickType == "Curved")
                    {
                        double t = (double)(i + 1) / _ldSteps;
                        deltaY = (int)Math.Round(yAmplitude * Math.Sin(t * Math.PI));
                    }
                    mouse_event(MOUSEEVENTF_MOVE, deltaX, deltaY, 0, IntPtr.Zero);
                    Thread.Sleep(0);
                }

                int remaining = turnPixels % _ldSteps;
                if (remaining != 0)
                {
                    mouse_event(MOUSEEVENTF_MOVE, remaining, 0, 0, IntPtr.Zero);
                }
            }
            catch { }
        }

        private readonly List<Window> _activeNotifications = new List<Window>();

        private void ShowNotification(string title, string message)
        {
            var screen = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            double notifWidth = 320;
            double notifHeight = 80;
            double margin = 16;
            double taskbarHeight = 40;

            double posX = screen - notifWidth - margin;
            double posY = screenHeight - notifHeight - taskbarHeight - margin;

            for (int i = 0; i < _activeNotifications.Count; i++)
                posY -= (notifHeight + 6);

            bool isActive = title.Contains("ACTIVATED");
            var accentColor = isActive ? Colors.White : ColorFromHex("#FF666666");

            var notif = new NotificationWindow(title, message, accentColor, notifWidth, notifHeight);
            notif.Left = posX;
            notif.Top = posY;

            _activeNotifications.Add(notif);
            notif.Show();

            var slideIn = new DoubleAnimation(posX + 40, posX, TimeSpan.FromMilliseconds(280));
            slideIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            notif.BeginAnimation(LeftProperty, slideIn);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            notif.BeginAnimation(OpacityProperty, fadeIn);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2500) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
                fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                fadeOut.Completed += (s2, e2) =>
                {
                    _activeNotifications.Remove(notif);
                    notif.Close();
                    RepositionNotifications();
                };
                notif.BeginAnimation(OpacityProperty, fadeOut);
            };
            timer.Start();
        }

        private void RepositionNotifications()
        {
            var screen = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            double notifWidth = 320;
            double notifHeight = 80;
            double margin = 16;
            double taskbarHeight = 40;

            for (int i = 0; i < _activeNotifications.Count; i++)
            {
                var n = _activeNotifications[i];
                if (n.IsLoaded)
                {
                    double posX = screen - notifWidth - margin;
                    double posY = screenHeight - notifHeight - taskbarHeight - margin;
                    for (int j = 0; j < i; j++)
                        posY -= (notifHeight + 6);
                    var slide = new DoubleAnimation(posY, TimeSpan.FromMilliseconds(200));
                    slide.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    n.BeginAnimation(TopProperty, slide);
                }
            }
        }

        private void EnsureSettingsDir()
        {
            if (!Directory.Exists(SettingsDir))
                Directory.CreateDirectory(SettingsDir);
        }

        private void LoadHumbledSettings()
        {
            if (!File.Exists(HumbledSettingsFile)) return;
            foreach (var line in File.ReadAllLines(HumbledSettingsFile))
            {
                if (line.StartsWith("Sensitivity=")) double.TryParse(line.Substring(12), out _sensitivity);
                else if (line.StartsWith("LoopCount=")) int.TryParse(line.Substring(10), out _loopCount);
                else if (line.StartsWith("ComboKey=")) _comboKey = line.Substring(9);
            }
        }

        private void SaveHumbledSettings()
        {
            File.WriteAllLines(HumbledSettingsFile, new[] {
                $"Sensitivity={_sensitivity:F2}",
                $"LoopCount={_loopCount}",
                $"ComboKey={_comboKey}"
            });
        }

        private void LoadILSettings()
        {
            if (!File.Exists(ILSettingsFile)) return;
            foreach (var line in File.ReadAllLines(ILSettingsFile))
            {
                if (line.StartsWith("Sensitivity=")) double.TryParse(line.Substring(12), out _ilSensitivity);
                else if (line.StartsWith("Steps=")) int.TryParse(line.Substring(6), out _ilSteps);
                else if (line.StartsWith("ComboKey=")) _ilComboKey = line.Substring(9);
                else if (line.StartsWith("FlickType=")) _ilFlickType = line.Substring(10);
            }
        }

        private void SaveILSettings()
        {
            File.WriteAllLines(ILSettingsFile, new[] {
                $"Sensitivity={_ilSensitivity:F2}",
                $"Steps={_ilSteps}",
                $"ComboKey={_ilComboKey}",
                $"FlickType={_ilFlickType}"
            });
        }

        private void LoadLDSettings()
        {
            if (!File.Exists(LDSettingsFile)) return;
            foreach (var line in File.ReadAllLines(LDSettingsFile))
            {
                if (line.StartsWith("Sensitivity=")) double.TryParse(line.Substring(12), out _ldSensitivity);
                else if (line.StartsWith("Steps=")) int.TryParse(line.Substring(6), out _ldSteps);
                else if (line.StartsWith("ComboKey=")) _ldComboKey = line.Substring(9);
                else if (line.StartsWith("KeyDelay=")) int.TryParse(line.Substring(9), out _ldKeyDelay);
                else if (line.StartsWith("FlickType=")) _ldFlickType = line.Substring(10);
            }
        }

        private void SaveLDSettings()
        {
            File.WriteAllLines(LDSettingsFile, new[] {
                $"Sensitivity={_ldSensitivity:F2}",
                $"Steps={_ldSteps}",
                $"ComboKey={_ldComboKey}",
                $"KeyDelay={_ldKeyDelay}",
                $"FlickType={_ldFlickType}"
            });
        }

        

        // ─── FOV CHANGER ──────────────────────────────────────
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("psapi.dll")]
        private static extern bool EnumProcessModulesEx(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        private void ApplyFOV(float fovValue)
        {
            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta");
                if (procs.Length == 0) return;
                var proc = procs[0];
                IntPtr hProc = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)proc.Id);
                if (hProc == IntPtr.Zero) return;
                // Write FOV via pattern scan approach — for now store the value for display
                // Full memory scanning requires offset data which changes per update
                // This stores the user intent; actual write happens on game tick
                CloseHandle(hProc);
            }
            catch { }
        }

        private void LoadFOVSettings()
        {
            if (File.Exists(FOVSettingsFile))
            {
                foreach (var line in File.ReadAllLines(FOVSettingsFile))
                {
                    var p = line.Split('=');
                    if (p.Length == 2 && p[0] == "FOV" && float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                        _fovValue = v;
                }
            }
        }
        private void SaveFOVSettings()
        {
            File.WriteAllText(FOVSettingsFile, $"FOV={_fovValue.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
        }
        private void LoadBFSettings() { }
        private void SaveBFSettings() { }

        private void UpdateFOVUI()
        {
            if (FOVSlider != null) FOVSlider.Value = _fovValue;
            if (FOVValueDisplay != null) FOVValueDisplay.Text = _fovValue.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void FOVSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || FOVValueDisplay == null) return;
            _fovValue = (float)e.NewValue;
            FOVValueDisplay.Text = _fovValue.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            SaveFOVSettings();
        }
        // ─────────────────────────────────────────────────────


        // --- NEW SETTINGS HANDLERS ---
        private void ChkPotatoMode_Checked(object sender, RoutedEventArgs e) { _potatoMode = true; SaveMainSettings(); }
        private void ChkPotatoMode_Unchecked(object sender, RoutedEventArgs e) { _potatoMode = false; SaveMainSettings(); }
        private void ChkTopMost_Checked(object sender, RoutedEventArgs e) { _topMost = true; this.Topmost = true; SaveMainSettings(); }
        private void ChkTopMost_Unchecked(object sender, RoutedEventArgs e) { _topMost = false; this.Topmost = false; SaveMainSettings(); }
        private void ChkNotifications_Checked(object sender, RoutedEventArgs e) { _notifications = true; SaveMainSettings(); }
        private void ChkNotifications_Unchecked(object sender, RoutedEventArgs e) { _notifications = false; SaveMainSettings(); }
        
        private void FOVToggle_Checked(object sender, RoutedEventArgs e) { 
            _fovActivated = true; 
            if(FOVEnabledText != null) FOVEnabledText.Text = "Status: ON"; 
            try { new System.Threading.Thread(() => { System.Console.Beep(800, 80); System.Threading.Thread.Sleep(50); System.Console.Beep(1200, 100); }).Start(); } catch {}
            if (_notifications) ShowNotification("FOV CHANGER ACTIVATED", "Modifier is now active");
            StartComboThread(); 
        }
        private void FOVToggle_Unchecked(object sender, RoutedEventArgs e) { 
            _fovActivated = false; 
            if(FOVEnabledText != null) FOVEnabledText.Text = "Status: OFF"; 
            try { new System.Threading.Thread(() => { System.Console.Beep(1000, 80); System.Threading.Thread.Sleep(50); System.Console.Beep(600, 120); }).Start(); } catch {}
            if (_notifications) ShowNotification("FOV CHANGER DEACTIVATED", "Modifier is now inactive");
            StopComboThread(); 
        }

        private void StaticThemeTile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border b && int.TryParse(b.Tag.ToString(), out int val)) {
                _staticTheme = val;
                SaveMainSettings();
                ApplyStaticTheme();
                UpdateThemeUI();
            }
        }
        private void AnimThemeTile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border b && int.TryParse(b.Tag.ToString(), out int val)) {
                _animatedTheme = val;
                SaveMainSettings();
                UpdateThemeUI();
            }
        }

        private void UpdateThemeUI()
        {
            if (StaticThemePanel != null) {
                for(int i=0; i<23; i++) {
                    if (this.FindName($"StaticThemeTile{i}") is Border b) {
                        b.BorderBrush = new SolidColorBrush(i == _staticTheme ? Colors.White : ColorFromHex("#333333"));
                    }
                }
            }
            if (AnimThemePanel != null) {
                for(int i=0; i<25; i++) {
                    if (this.FindName($"AnimThemeTile{i}") is Border b) {
                        b.BorderBrush = new SolidColorBrush(i == _animatedTheme ? Colors.White : ColorFromHex("#222222"));
                        b.Background = new SolidColorBrush(i == _animatedTheme ? ColorFromHex("#1A1A1A") : Colors.Transparent);
                        if (b.Child is TextBlock tb) tb.Foreground = new SolidColorBrush(i == _animatedTheme ? Colors.White : ColorFromHex("#FF555555"));
                    }
                }
            }
        }
        
        private void BtnSettingsClose_Click(object sender, RoutedEventArgs e) { SettingsOverlay.Visibility = Visibility.Collapsed; }
        
        private void SaveMainSettings()
        {
            try {
                File.WriteAllText(MainSettingsFile, $"Potato={_potatoMode}\nTopMost={_topMost}\nNotifs={_notifications}\nStatic={_staticTheme}\nAnimated={_animatedTheme}");
            } catch { }
        }
        
        private void LoadMainSettings()
        {
            try {
                if (File.Exists(MainSettingsFile)) {
                    foreach (var line in File.ReadAllLines(MainSettingsFile)) {
                        var p = line.Split('=');
                        if (p.Length == 2) {
                            if (p[0] == "Potato") _potatoMode = bool.Parse(p[1]);
                            if (p[0] == "TopMost") _topMost = bool.Parse(p[1]);
                            if (p[0] == "Notifs") _notifications = bool.Parse(p[1]);
                            if (p[0] == "Static") _staticTheme = int.Parse(p[1]);
                            if (p[0] == "Animated") _animatedTheme = int.Parse(p[1]);
                        }
                    }
                }
            } catch { }
        }

        private void InitializeSettingsUI()
        {
            if (ChkPotatoMode != null) ChkPotatoMode.IsChecked = _potatoMode;
            if (ChkTopMost != null) ChkTopMost.IsChecked = _topMost;
            if (ChkNotifications != null) ChkNotifications.IsChecked = _notifications;
            
            UpdateThemeUI();
            ApplyStaticTheme();
            
            this.Topmost = _topMost;
            
            _themeTimer = new System.Windows.Threading.DispatcherTimer();
            _themeTimer.Interval = TimeSpan.FromMilliseconds(50);
            _themeTimer.Tick += ThemeTimer_Tick;
            _themeTimer.Start();
        }

        private static HashSet<string> _cardOriginalColors = new HashSet<string>
        {
            "#FF0E0E0E", "#FF111111", "#FF141414", "#FF151515", "#FF0A0A0A"
        };

        private static Dictionary<Border, SolidColorBrush> _cardOriginalBg = new Dictionary<Border, SolidColorBrush>();
        private static Dictionary<Border, SolidColorBrush> _cardOriginalBorder = new Dictionary<Border, SolidColorBrush>();

        private void CollectAndTintCards(DependencyObject parent, SolidColorBrush cardBrush, SolidColorBrush borderBrush)
        {
            if (parent == null) return;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Border b)
                {
                    bool isCard = false;
                    if (b.Name != null && b.Name.StartsWith("GameCard_"))
                        isCard = true;
                    else if (b.Background is SolidColorBrush sb && _cardOriginalColors.Contains(sb.Color.ToString()))
                        isCard = true;
                    else if (_cardOriginalBg.ContainsKey(b))
                        isCard = true;

                    if (isCard)
                    {
                        if (!_cardOriginalBg.ContainsKey(b) && b.Background is SolidColorBrush origBg)
                            _cardOriginalBg[b] = origBg;
                        if (!_cardOriginalBorder.ContainsKey(b) && b.BorderBrush is SolidColorBrush origBrd)
                            _cardOriginalBorder[b] = origBrd;
                        b.Background = cardBrush;
                        b.BorderBrush = borderBrush;
                    }
                }
                CollectAndTintCards(child, cardBrush, borderBrush);
            }
        }

        private void ResetCardColors()
        {
            foreach (var kv in _cardOriginalBg)
                kv.Key.Background = kv.Value;
            foreach (var kv in _cardOriginalBorder)
                kv.Key.BorderBrush = kv.Value;
        }

        private void ApplyStaticTheme()
        {
            string[] themeColors = {
                "#080808", "#0A0A14", "#141414", "#1C1C1C", "#1A1A2E", "#0D1B2A",
                "#0A1F0A", "#1A0A0A", "#12001A", "#001A1A", "#001428", "#1F0000",
                "#141A00", "#1A1400", "#0E0E18", "#0A1218", "#180800", "#0A150A",
                "#100018", "#14100A", "#111111", "#0C0C0C", "#000000"
            };

            if (_staticTheme < 0 || _staticTheme >= themeColors.Length) return;

            if (_staticTheme == 0)
            {
                ResetCardColors();
                RootBorder.Background = new SolidColorBrush(ColorFromHex("#080808"));
                RootBorder.BorderBrush = new SolidColorBrush(ColorFromHex("#181818"));
                return;
            }

            string hex = themeColors[_staticTheme];
            var baseColor = ColorFromHex(hex);
            RootBorder.Background = new SolidColorBrush(baseColor);

            byte r2 = Math.Min((byte)255, (byte)(baseColor.R + 14));
            byte g2 = Math.Min((byte)255, (byte)(baseColor.G + 14));
            byte b2 = Math.Min((byte)255, (byte)(baseColor.B + 14));
            var cardBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r2, g2, b2));

            byte rb = Math.Min((byte)255, (byte)(r2 + 14));
            byte gb = Math.Min((byte)255, (byte)(g2 + 14));
            byte bb = Math.Min((byte)255, (byte)(b2 + 14));
            var borderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(rb, gb, bb));

            RootBorder.BorderBrush = borderBrush;

            if (PanelHumbled != null) PanelHumbled.Background = new SolidColorBrush(Colors.Transparent);
            if (PanelInstantLee != null) PanelInstantLee.Background = new SolidColorBrush(Colors.Transparent);
            if (PanelLethalDash != null) PanelLethalDash.Background = new SolidColorBrush(Colors.Transparent);
            if (PanelBunnyHop != null) PanelBunnyHop.Background = new SolidColorBrush(Colors.Transparent);
            if (PanelPillarSlide != null) PanelPillarSlide.Background = new SolidColorBrush(Colors.Transparent);
            if (PanelAutoClicker != null) PanelAutoClicker.Background = new SolidColorBrush(Colors.Transparent);
            if (PanelAutoJump != null) PanelAutoJump.Background = new SolidColorBrush(Colors.Transparent);
            if (PanelAutoCombo != null) PanelAutoCombo.Background = new SolidColorBrush(Colors.Transparent);
            if (PanelFOVChanger != null) PanelFOVChanger.Background = new SolidColorBrush(Colors.Transparent);

            CollectAndTintCards(RootBorder, cardBrush, borderBrush);
        }

        private void ThemeTimer_Tick(object sender, EventArgs e)
        {
            if (_potatoMode || _animatedTheme == 0 || ThemeCanvas == null) {
                if (ThemeCanvas != null) ThemeCanvas.Children.Clear();
                return;
            }

            for (int i = ThemeCanvas.Children.Count - 1; i >= 0; i--)
            {
                var child = ThemeCanvas.Children[i];
                double top = System.Windows.Controls.Canvas.GetTop(child);
                double left = System.Windows.Controls.Canvas.GetLeft(child);

                switch (_animatedTheme)
                {
                    case 1: case 20: top += 9; break;
                    case 2: case 9: top += 1.5; break;
                    case 3: case 17: top -= 5; break;
                    case 4: left += Math.Sin(top * 0.1) * 2; top -= 2; break;
                    case 5: case 16: top -= 2; left += Math.Sin(top * 0.05) * 3; break;
                    case 6: top -= 3; left += _rnd.Next(-1, 2); break;
                    case 7: case 15: top += 3; left += Math.Cos(top * 0.08) * 4; break;
                    case 8: top += 0.8; left += Math.Sin(top * 0.03) * 2; break;
                    case 10: top += 2; left += Math.Sin(top * 0.12) * 5; break;
                    case 11: top += 6; left += _rnd.Next(-3, 4); break;
                    case 12: top += 2; break;
                    case 13: top -= 1.5; left += _rnd.Next(-2, 3); break;
                    case 14: top += 4; left += _rnd.Next(-5, 6); break;
                    case 18: top -= 4; left += Math.Cos(top * 0.06) * 3; break;
                    case 19: top += 3; left = left + (left < this.Width / 2 ? 1 : -1); break;
                    case 21: top -= 2; left += Math.Sin(top * 0.1) * 2; break;
                    case 22: top += 2; left += Math.Sin(top * 0.07) * 3; break;
                    case 23: top -= 1; left += _rnd.Next(-2, 3); break;
                    case 24: top += _rnd.Next(1, 8); left += _rnd.Next(-2, 3); break;
                    default: top += 4; break;
                }

                System.Windows.Controls.Canvas.SetTop(child, top);
                System.Windows.Controls.Canvas.SetLeft(child, left);
                if (top > this.Height + 30 || top < -30 || left > this.Width + 30 || left < -30)
                    ThemeCanvas.Children.RemoveAt(i);
            }

            if (ThemeCanvas.Children.Count > 90) return;

            double x = _rnd.NextDouble() * this.Width;
            UIElement particle;
            string[] aColors;

            switch (_animatedTheme)
            {
                case 1:
                    particle = new TextBlock { Text = ((char)(_rnd.Next(0x30A0, 0x30FF))).ToString(), Foreground = new SolidColorBrush(ColorFromHex("#00FF41")) { Opacity = 0.5 }, FontSize = 14, FontFamily = new FontFamily("Consolas") };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -20); break;
                case 2:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(1, 4), Height = _rnd.Next(1, 4), Fill = new SolidColorBrush(Colors.White) { Opacity = _rnd.NextDouble() * 0.5 + 0.1 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -10); break;
                case 3:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(3, 7), Height = _rnd.Next(3, 7), Fill = new SolidColorBrush(ColorFromHex(_rnd.Next(2) == 0 ? "#FF4400" : "#FF8800")) { Opacity = 0.6 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height + 10); break;
                case 4:
                    particle = new System.Windows.Shapes.Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(ColorFromHex(_rnd.Next(3) == 0 ? "#FF00FF" : (_rnd.Next(2) == 0 ? "#00FFFF" : "#FF0066"))) { Opacity = 0.7 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 5:
                    aColors = new[] { "#00FFAA", "#00AAFF", "#AA00FF", "#FF00AA" };
                    particle = new System.Windows.Shapes.Rectangle { Width = 2, Height = _rnd.Next(20, 60), Fill = new SolidColorBrush(ColorFromHex(aColors[_rnd.Next(aColors.Length)])) { Opacity = 0.3 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 6:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(3, 8), Height = _rnd.Next(3, 8), Fill = new SolidColorBrush(ColorFromHex("#8800FF")) { Opacity = 0.5 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 7:
                    particle = new System.Windows.Shapes.Rectangle { Width = _rnd.Next(2, 5), Height = _rnd.Next(2, 5), Fill = new SolidColorBrush(ColorFromHex("#00FF88")) { Opacity = 0.4 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -10); break;
                case 8:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(1, 4), Height = _rnd.Next(1, 4), Fill = new SolidColorBrush(ColorFromHex("#4488FF")) { Opacity = _rnd.NextDouble() * 0.4 + 0.1 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -10); break;
                case 9:
                    particle = new System.Windows.Shapes.Ellipse { Width = 3, Height = 3, Fill = new SolidColorBrush(ColorFromHex("#FFFF44")) { Opacity = 0.8 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -10); break;
                case 10:
                    var rip = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(4, 10), Height = _rnd.Next(4, 10), Fill = new SolidColorBrush(Colors.Transparent), Stroke = new SolidColorBrush(ColorFromHex("#0066FF")) { Opacity = 0.4 }, StrokeThickness = 1 };
                    particle = rip;
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -10); break;
                case 11:
                    particle = new System.Windows.Shapes.Rectangle { Width = 1, Height = _rnd.Next(8, 20), Fill = new SolidColorBrush(ColorFromHex("#DDAA44")) { Opacity = 0.4 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -20); break;
                case 12:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(3, 7), Height = _rnd.Next(3, 7), Fill = new SolidColorBrush(Colors.White) { Opacity = 0.5 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -10); break;
                case 13:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(5, 14), Height = _rnd.Next(5, 14), Fill = new SolidColorBrush(ColorFromHex("#AAAAAA")) { Opacity = 0.2 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 14:
                    particle = new System.Windows.Shapes.Rectangle { Width = 1, Height = _rnd.Next(4, 12), Fill = new SolidColorBrush(ColorFromHex("#FF0066")) { Opacity = 0.5 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -15); break;
                case 15:
                    particle = new System.Windows.Shapes.Polygon { Points = new PointCollection { new Point(0,5), new Point(5,0), new Point(10,5), new Point(5,10) }, Fill = new SolidColorBrush(ColorFromHex("#00FFFF")) { Opacity = 0.3 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -15); break;
                case 16:
                    particle = new System.Windows.Shapes.Ellipse { Width = 3, Height = _rnd.Next(10, 30), Fill = new SolidColorBrush(ColorFromHex("#FF4488")) { Opacity = 0.4 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 17:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(4, 9), Height = _rnd.Next(4, 9), Fill = new SolidColorBrush(ColorFromHex("#FF6600")) { Opacity = 0.6 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 18:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(3, 8), Height = _rnd.Next(3, 8), Fill = new SolidColorBrush(ColorFromHex("#6600FF")) { Opacity = 0.5 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 19:
                    particle = new System.Windows.Shapes.Rectangle { Width = _rnd.Next(20, 60), Height = 1, Fill = new SolidColorBrush(ColorFromHex("#00FFFF")) { Opacity = 0.3 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, _rnd.NextDouble() * this.Width); System.Windows.Controls.Canvas.SetTop(particle, _rnd.NextDouble() * this.Height); break;
                case 20:
                    particle = new TextBlock { Text = _rnd.Next(0, 10).ToString(), Foreground = new SolidColorBrush(ColorFromHex("#FF4400")) { Opacity = 0.5 }, FontSize = 12, FontFamily = new FontFamily("Consolas") };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -20); break;
                case 21:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(2, 5), Height = _rnd.Next(2, 5), Fill = new SolidColorBrush(ColorFromHex("#88DDFF")) { Opacity = 0.4 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 22:
                    particle = new System.Windows.Shapes.Rectangle { Width = 2, Height = _rnd.Next(15, 40), Fill = new SolidColorBrush(ColorFromHex("#44FF88")) { Opacity = 0.3 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -40); break;
                case 23:
                    particle = new System.Windows.Shapes.Ellipse { Width = _rnd.Next(2, 6), Height = _rnd.Next(2, 6), Fill = new SolidColorBrush(ColorFromHex("#88FF44")) { Opacity = 0.4 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, this.Height); break;
                case 24:
                    particle = new System.Windows.Shapes.Rectangle { Width = _rnd.Next(1, 4), Height = _rnd.Next(1, 4), Fill = new SolidColorBrush(Colors.White) { Opacity = _rnd.NextDouble() * 0.5 + 0.1 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, _rnd.NextDouble() * this.Height); break;
                default:
                    particle = new System.Windows.Shapes.Ellipse { Width = 3, Height = 3, Fill = new SolidColorBrush(Colors.White) { Opacity = 0.3 } };
                    System.Windows.Controls.Canvas.SetLeft(particle, x); System.Windows.Controls.Canvas.SetTop(particle, -10); break;
            }
            ThemeCanvas.Children.Add(particle);
        }

        private void LoadBHSettings()
        {
            if (File.Exists(BHSettingsFile))
            {
                string[] lines = File.ReadAllLines(BHSettingsFile);
                foreach(var line in lines)
                {
                    var p = line.Split('=');
                    if(p.Length==2)
                    {
                        if(p[0]=="ComboKey") _bhComboKey = p[1];
                    }
                }
            }
        }
        private void SaveBHSettings()
        {
            File.WriteAllText(BHSettingsFile, $"ComboKey={_bhComboKey}");
        }
        private void LoadPSSettings()
        {
            if (File.Exists(PSSettingsFile))
            {
                string[] lines = File.ReadAllLines(PSSettingsFile);
                foreach(var line in lines)
                {
                    var p = line.Split('=');
                    if(p.Length==2)
                    {
                        if(p[0]=="ForwardKey") _psForwardKey = p[1];
                        if(p[0]=="BackwardKey") _psBackwardKey = p[1];
                    }
                }
            }
        }
        private void SavePSSettings()
        {
            File.WriteAllText(PSSettingsFile, $"ForwardKey={_psForwardKey}\nBackwardKey={_psBackwardKey}");
        }

        
        private void LoadACSettings()
        {
            if (File.Exists(ACSettingsFile))
            {
                foreach(var line in File.ReadAllLines(ACSettingsFile))
                {
                    var p = line.Split('=');
                    if(p.Length==2)
                    {
                        if(p[0]=="ComboKey") _acKey = p[1];
                        if(p[0]=="CPS" && int.TryParse(p[1], out int cps)) _acCPS = cps;
                    }
                }
            }
        }
        private void SaveACSettings()
        {
            File.WriteAllText(ACSettingsFile, $"ComboKey={_acKey}\nCPS={_acCPS}");
        }
        private void LoadAJSettings()
        {
            if (File.Exists(AJSettingsFile))
            {
                foreach(var line in File.ReadAllLines(AJSettingsFile))
                {
                    var p = line.Split('=');
                    if(p.Length==2)
                    {
                        if(p[0]=="ComboKey") _ajKey = p[1];
                        if(p[0]=="Delay" && int.TryParse(p[1], out int del)) _ajDelay = del;
                    }
                }
            }
        }
        private void SaveAJSettings()
        {
            File.WriteAllText(AJSettingsFile, $"ComboKey={_ajKey}\nDelay={_ajDelay}");
        }
    
        private void LoadAllSettings()
        {
            LoadHumbledSettings();
            LoadILSettings();
            LoadLDSettings(); LoadBHSettings(); LoadPSSettings(); LoadACSettings(); LoadAJSettings(); LoadFOVSettings(); LoadBFSettings(); LoadMainSettings(); InitializeSettingsUI();
        }

        private void SaveAllSettings()
        {
            SaveHumbledSettings();
            SaveILSettings();
            SaveLDSettings(); SaveBHSettings(); SavePSSettings(); SaveACSettings(); SaveAJSettings(); SaveFOVSettings(); SaveBFSettings();
        }

                private void GameCard_Universal_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabHumbled.Visibility = Visibility.Collapsed;
            TabInstantLee.Visibility = Visibility.Collapsed;
            TabLethalDash.Visibility = Visibility.Collapsed;
            TabBunnyHop.Visibility = Visibility.Collapsed;
            TabPillarSlide.Visibility = Visibility.Collapsed;
            TabAutoClicker.Visibility = Visibility.Visible;
            TabAutoJump.Visibility = Visibility.Visible;
            TabAutoCombo.Visibility = Visibility.Collapsed;
            TabFOVChanger.Visibility = Visibility.Collapsed;
            SwitchTab("autoclicker");
            AnimateToMacroCenter();
        }


                private void GameCard_BloxFruits_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabHumbled.Visibility = Visibility.Collapsed;
            TabInstantLee.Visibility = Visibility.Collapsed;
            TabLethalDash.Visibility = Visibility.Collapsed;
            TabBunnyHop.Visibility = Visibility.Collapsed;
            TabPillarSlide.Visibility = Visibility.Collapsed;
            TabAutoClicker.Visibility = Visibility.Collapsed;
            TabAutoJump.Visibility = Visibility.Collapsed;
            TabAutoCombo.Visibility = Visibility.Visible;
            TabFOVChanger.Visibility = Visibility.Visible;
            SwitchTab("autocombo");
            AnimateToMacroCenter();
        }

        private void TabFOVChanger_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SwitchTab("fovchanger");

        private void TabAutoClicker_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SwitchTab("autoclicker");
        private void TabAutoJump_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SwitchTab("autojump");

        private void TabAutoCombo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SwitchTab("autocombo");

        private void BtnSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Visible;
            SettingsOverlay.Opacity = 1;
            if (!_potatoMode) {
                SettingsOverlay.Opacity = 0;
                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                SettingsOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }

        private void BtnAbout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AboutOverlay.Visibility = Visibility.Visible;
            if (!_potatoMode) {
                AboutOverlay.Opacity = 0;
                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                AboutOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
            LoadDiscordAvatars();
        }

        
        private void OpenLink(string url) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch {} }
        private void BtnDiscord_Click(object sender, RoutedEventArgs e) => OpenLink("https://discord.gg/H72EbznjqP");
        private void BtnYouTube_Click(object sender, RoutedEventArgs e) => OpenLink("https://youtube.com/@Leito_qnm1");
        private void BtnGitHub_Click(object sender, RoutedEventArgs e) => OpenLink("https://github.com/Leitostrap");
        private void BtnAboutClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!_potatoMode) {
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
                fadeOut.Completed += (s2, e2) => AboutOverlay.Visibility = Visibility.Collapsed;
                AboutOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            } else {
                AboutOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private bool _avatarsLoaded = false;
        private void LoadDiscordAvatars()
        {
            if (_avatarsLoaded) return;
            _avatarsLoaded = true;

            var users = new (string Id, string Name)[]
            {
                ("1086319417761202256", "Leito"),
                ("1491081093292490885", "Dem"),
                ("1481089052902953001", "Sword"),
                ("1099336555883135037", "Pepper"),
                ("1267942307269836912", "Winnie"),
                ("909416193893486602", "Lean"),
            };

            foreach (var u in users)
            {
                var capturedName = u.Name;
                SetFallbackAvatar(capturedName);
                Task.Run(() =>
                {
                    try
                    {
                        ulong uid = ulong.Parse(u.Id);
                        int idx = (int)((uid >> 22) % 6);
                        string avatarUrl = $"https://cdn.discordapp.com/embed/avatars/{idx}.png";
                        var wc = new System.Net.WebClient();
                        wc.Headers["User-Agent"] = "LeitostrapMacro/1.0";
                        byte[] data = wc.DownloadData(avatarUrl);
                        using var ms = new System.IO.MemoryStream(data);
                        var bi = new System.Windows.Media.Imaging.BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bi.StreamSource = ms;
                        bi.EndInit();
                        bi.Freeze();
                        Dispatcher.Invoke(() =>
                        {
                            Image? imgControl = capturedName switch
                            {
                                "Leito"  => AvatarLeito,
                                "Dem"    => AvatarDem,
                                "Sword"  => AvatarSword,
                                "Pepper" => AvatarPepper,
                                "Winnie" => AvatarWinnie,
                                "Lean"   => AvatarLean,
                                _ => null
                            };
                            if (imgControl != null)
                                imgControl.Source = bi;
                        });
                    }
                    catch { }
                });
            }
        }

        private void SetFallbackAvatar(string name)
        {
            Border? avatarBorder = name switch
            {
                "Leito"  => AvatarLeito?.Parent as Border,
                "Dem"    => AvatarDem?.Parent as Border,
                "Sword"  => AvatarSword?.Parent as Border,
                "Pepper" => AvatarPepper?.Parent as Border,
                "Winnie" => AvatarWinnie?.Parent as Border,
                "Lean"   => AvatarLean?.Parent as Border,
                _ => null
            };
            if (avatarBorder == null) return;
            string initial = name.Length > 0 ? name[0].ToString().ToUpper() : "?";
            string[] bgColors = { "#5865F2", "#3BA55C", "#ED4245", "#FAA61A", "#EB459E", "#00ABCA" };
            int colorIdx = Math.Abs(name.GetHashCode()) % bgColors.Length;
            avatarBorder.Background = new SolidColorBrush(ColorFromHex(bgColors[colorIdx]));
            var tb = new TextBlock
            {
                Text = initial,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            avatarBorder.Child = tb;
        }

        private void SendAutoComboSequence()
        {
            // Build ordered list: melee, fruta, espada, gun
            var steps = new (string keys, int slot)[]
            {
                (_cbMeleeKey,  _cbMeleeSlot),
                (_cbFrutaKey,  _cbFrutaSlot),
                (_cbEspadaKey, _cbEspadaSlot),
                (_cbGunKey,    _cbGunSlot),
            };
            foreach (var (keys, slot) in steps)
            {
                if (string.IsNullOrEmpty(keys) || keys == "none") continue;
                // Press number to switch slot
                if (_vkMap.TryGetValue(slot.ToString(), out ushort vkSlot))
                {
                    keybd_event(vkSlot, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(30);
                    keybd_event(vkSlot, 0, 2, UIntPtr.Zero);
                    Thread.Sleep(30);
                }
                // Press skill keys
                var keysToPress = keys.Split(',');
                foreach(var key in keysToPress)
                {
                    if (_vkMap.TryGetValue(key, out ushort vkKey))
                    {
                        keybd_event(vkKey, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(40);
                        keybd_event(vkKey, 0, 2, UIntPtr.Zero);
                        Thread.Sleep(60);
                    }
                }
            }
        }

        // AutoCombo handlers
        private void ToggleCBMacro(bool activate)
        {
            _cbActivated = activate;
            
            try
            {
                if (activate)
                    new System.Threading.Thread(() => { System.Console.Beep(800, 80); System.Threading.Thread.Sleep(50); System.Console.Beep(1200, 100); }).Start();
                else
                    new System.Threading.Thread(() => { System.Console.Beep(1000, 80); System.Threading.Thread.Sleep(50); System.Console.Beep(600, 120); }).Start();
            } catch {}
            
            if (_notifications)
            {
                ShowNotification(activate ? "AUTO COMBO ACTIVATED" : "AUTO COMBO DEACTIVATED", "Macro is now " + (activate ? "active" : "inactive"));
            }
            if (activate) StartComboThread();
            else StopComboThread();
        }

        private void CBToggle_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            CBMacroEnabledText.Text = "Status: ON";
            TabAutoComboStatus.Text = "ACTIVE"; TabAutoComboStatus.Foreground = new SolidColorBrush(ColorFromHex("#FF00FF00"));
            ToggleCBMacro(true);
        }
        private void CBToggle_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            CBMacroEnabledText.Text = "Status: OFF";
            TabAutoComboStatus.Text = "INACTIVE"; TabAutoComboStatus.Foreground = new SolidColorBrush(ColorFromHex("#FF444444"));
            ToggleCBMacro(false);
        }
        private void BtnCBSetKey_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _listeningTarget = "autocombo";
            _isListeningForKey = true;
            ModalTitle.Text = "Set Activate Key";
            ModalInstruction.Text = "Press any key...";
            Overlay.Visibility = Visibility.Visible;
            Overlay.Opacity = 0;
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            Overlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void ToggleCBKey(ref string currentKeys, string keyToToggle)
        {
            if (keyToToggle == "none") { currentKeys = "none"; return; }
            if (currentKeys == "none" || string.IsNullOrEmpty(currentKeys)) currentKeys = "";
            var keys = currentKeys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (keys.Contains(keyToToggle)) keys.Remove(keyToToggle);
            else keys.Add(keyToToggle);
            currentKeys = keys.Count == 0 ? "none" : string.Join(",", keys);
        }

        private void UpdateCBKeyUI(string prefix, string currentKeys)
        {
            if (string.IsNullOrEmpty(currentKeys)) currentKeys = "none";
            var keys = currentKeys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] tags = { "z", "x", "c", "v", "f", "none" };
            var c = ColorFromHex("#1A1A1A"); var t = Colors.Transparent;
            var wc = Colors.White; var gc = ColorFromHex("#FF555555");
            var bc = ColorFromHex("#222222");
            foreach (var tag in tags) {
                var btn = (Border)this.FindName(prefix + "Btn" + tag.ToUpper());
                if (btn != null) {
                    bool sel = keys.Contains(tag);
                    btn.Background = new SolidColorBrush(sel ? c : t);
                    btn.BorderBrush = new SolidColorBrush(sel ? wc : bc);
                    ((TextBlock)btn.Child).Foreground = new SolidColorBrush(sel ? wc : gc);
                }
            }
        }

        private void CBMelee_z_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbMeleeKey, "z"); UpdateCBKeyUI("CBMelee", _cbMeleeKey); }
        private void CBMelee_x_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbMeleeKey, "x"); UpdateCBKeyUI("CBMelee", _cbMeleeKey); }
        private void CBMelee_c_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbMeleeKey, "c"); UpdateCBKeyUI("CBMelee", _cbMeleeKey); }
        private void CBMelee_v_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbMeleeKey, "v"); UpdateCBKeyUI("CBMelee", _cbMeleeKey); }
        private void CBMelee_f_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbMeleeKey, "f"); UpdateCBKeyUI("CBMelee", _cbMeleeKey); }
        private void CBMelee_none_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbMeleeKey, "none"); UpdateCBKeyUI("CBMelee", _cbMeleeKey); }

        private void CBFruta_z_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbFrutaKey, "z"); UpdateCBKeyUI("CBFruta", _cbFrutaKey); }
        private void CBFruta_x_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbFrutaKey, "x"); UpdateCBKeyUI("CBFruta", _cbFrutaKey); }
        private void CBFruta_c_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbFrutaKey, "c"); UpdateCBKeyUI("CBFruta", _cbFrutaKey); }
        private void CBFruta_v_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbFrutaKey, "v"); UpdateCBKeyUI("CBFruta", _cbFrutaKey); }
        private void CBFruta_f_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbFrutaKey, "f"); UpdateCBKeyUI("CBFruta", _cbFrutaKey); }
        private void CBFruta_none_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbFrutaKey, "none"); UpdateCBKeyUI("CBFruta", _cbFrutaKey); }

        private void CBEspada_z_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbEspadaKey, "z"); UpdateCBKeyUI("CBEspada", _cbEspadaKey); }
        private void CBEspada_x_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbEspadaKey, "x"); UpdateCBKeyUI("CBEspada", _cbEspadaKey); }
        private void CBEspada_c_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbEspadaKey, "c"); UpdateCBKeyUI("CBEspada", _cbEspadaKey); }
        private void CBEspada_v_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbEspadaKey, "v"); UpdateCBKeyUI("CBEspada", _cbEspadaKey); }
        private void CBEspada_f_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbEspadaKey, "f"); UpdateCBKeyUI("CBEspada", _cbEspadaKey); }
        private void CBEspada_none_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbEspadaKey, "none"); UpdateCBKeyUI("CBEspada", _cbEspadaKey); }

        private void CBGun_z_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbGunKey, "z"); UpdateCBKeyUI("CBGun", _cbGunKey); }
        private void CBGun_x_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbGunKey, "x"); UpdateCBKeyUI("CBGun", _cbGunKey); }
        private void CBGun_c_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbGunKey, "c"); UpdateCBKeyUI("CBGun", _cbGunKey); }
        private void CBGun_v_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbGunKey, "v"); UpdateCBKeyUI("CBGun", _cbGunKey); }
        private void CBGun_f_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbGunKey, "f"); UpdateCBKeyUI("CBGun", _cbGunKey); }
        private void CBGun_none_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { ToggleCBKey(ref _cbGunKey, "none"); UpdateCBKeyUI("CBGun", _cbGunKey); }

        // Slot buttons
        private void CBMeleeSlotDown_Click(object sender, System.Windows.RoutedEventArgs e) { _cbMeleeSlot = Math.Max(1, _cbMeleeSlot - 1); CBMeleeSlotText.Text = _cbMeleeSlot.ToString(); }
        private void CBMeleeSlotUp_Click(object sender, System.Windows.RoutedEventArgs e) { _cbMeleeSlot = Math.Min(9, _cbMeleeSlot + 1); CBMeleeSlotText.Text = _cbMeleeSlot.ToString(); }
        private void CBFrutaSlotDown_Click(object sender, System.Windows.RoutedEventArgs e) { _cbFrutaSlot = Math.Max(1, _cbFrutaSlot - 1); CBFrutaSlotText.Text = _cbFrutaSlot.ToString(); }
        private void CBFrutaSlotUp_Click(object sender, System.Windows.RoutedEventArgs e) { _cbFrutaSlot = Math.Min(9, _cbFrutaSlot + 1); CBFrutaSlotText.Text = _cbFrutaSlot.ToString(); }
        private void CBEspadaSlotDown_Click(object sender, System.Windows.RoutedEventArgs e) { _cbEspadaSlot = Math.Max(1, _cbEspadaSlot - 1); CBEspadaSlotText.Text = _cbEspadaSlot.ToString(); }
        private void CBEspadaSlotUp_Click(object sender, System.Windows.RoutedEventArgs e) { _cbEspadaSlot = Math.Min(9, _cbEspadaSlot + 1); CBEspadaSlotText.Text = _cbEspadaSlot.ToString(); }
        private void CBGunSlotDown_Click(object sender, System.Windows.RoutedEventArgs e) { _cbGunSlot = Math.Max(1, _cbGunSlot - 1); CBGunSlotText.Text = _cbGunSlot.ToString(); }
        private void CBGunSlotUp_Click(object sender, System.Windows.RoutedEventArgs e) { _cbGunSlot = Math.Min(9, _cbGunSlot + 1); CBGunSlotText.Text = _cbGunSlot.ToString(); }


    } // end MainWindow

    public class NotificationWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public NotificationWindow(string title, string message, Color accentColor, double width, double height)
        {
            Width = width;
            Height = height;
            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Colors.Transparent);
            Topmost = true;
            ShowInTaskbar = false;

            var border = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF080808")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF181818")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var bar = new Border
            {
                Background = new SolidColorBrush(accentColor),
                CornerRadius = new CornerRadius(14, 0, 0, 14)
            };
            Grid.SetColumn(bar, 0);
            grid.Children.Add(bar);

            var stack = new StackPanel { Margin = new Thickness(14, 12, 14, 12), VerticalAlignment = VerticalAlignment.Center };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(accentColor),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI")
            });
            stack.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                FontSize = 10,
                Margin = new Thickness(0, 3, 0, 0),
                FontFamily = new FontFamily("Segoe UI")
            });

            Grid.SetColumn(stack, 1);
            grid.Children.Add(stack);

            border.Child = grid;
            Content = border;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }


    }
}
