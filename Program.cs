using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Auto_Click_Console.Platform.Interfaces;
using Auto_Click_Console.Platform.Windows;
using Auto_Click_Console.Platform.Linux;
using Auto_Click_Console.Platform.Fallback;
using Auto_Click_Console.Utils;

namespace Auto_Click_Console
{
    public class Program
    {
        // State variables
        private static bool _isRunning = false;
        private static CancellationTokenSource? _clickerTokenSource;
        
        // Platform components
        private static IAutoClicker? _autoClicker;
        private static IHotkeyManager? _hotkeyManager;
        
        // UI and Config components
        private static UIManager _uiManager = new UIManager();
        private static ConfigManager _configManager = new ConfigManager();
        private static ConfigManager.AppSettings _settings;
        
        private static bool _needsMenuRefresh = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Cross-Platform Auto Clicker");
            Console.WriteLine("==========================");

            // Load saved settings
            _settings = _configManager.LoadSettings();
            
            InitializePlatformSpecificComponents();

            bool exitRequested = false;

            // Register function key hotkeys
            _hotkeyManager?.RegisterHotkey(ModifierKeys.None, '1', () => SafeUIUpdate(StartClicking)); // F1
            _hotkeyManager?.RegisterHotkey(ModifierKeys.None, '2', () => SafeUIUpdate(StopClicking));  // F2
            _hotkeyManager?.RegisterHotkey(ModifierKeys.None, '3', () => SafeUIUpdate(TestHotkeyListener)); // F3

            // Start the hotkey listener
            _hotkeyManager?.StartListening();

            while (!exitRequested)
            {
                if (_needsMenuRefresh)
                {
                    ShowMenu();
                    _needsMenuRefresh = false;
                }

                if (!Console.KeyAvailable)
                {
                    await Task.Delay(100);
                    continue;
                }

                string choice = Console.ReadLine() ?? string.Empty;
                _needsMenuRefresh = true; // Need to refresh after input

                switch (choice)
                {
                    case "1":
                        ConfigureClickInterval();
                        break;
                    case "2":
                        StartClicking();
                        break;
                    case "3":
                        StopClicking();
                        break;
                    case "4":
                        ShowHelpInfo();
                        break;
                    case "5":
                        TestHotkeyListener();
                        break;
                    case "6":
                        ConfigureClickType();
                        break;
                    case "7":
                        exitRequested = true;
                        StopClicking(); // Make sure to stop clicking before exiting
                        _hotkeyManager?.StopListening();
                        break;
                    default:
                        Console.WriteLine("Invalid option, please try again.");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey(true);
                        break;
                }
            }

            Console.WriteLine("Auto Clicker has been closed. Goodbye!");
        }

        // Helper to safely update UI from hotkey callbacks
        private static void SafeUIUpdate(Action action)
        {
            action();
            _needsMenuRefresh = true;
        }

        private static void ShowMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Cross-Platform Auto Clicker");
            Console.WriteLine("==========================");
            Console.ResetColor();
            Console.WriteLine($"Current click interval: {_settings.ClickInterval}ms");
            Console.WriteLine($"Current click type: {_settings.ClickType}");
            Console.WriteLine($"Status: {(_isRunning ? "Running" : "Stopped")}");
            Console.WriteLine($"Platform: {RuntimeInformation.OSDescription}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("1. Configure click interval");
            Console.WriteLine("2. Start clicking (F1)");
            Console.WriteLine("3. Stop clicking (F2)");
            Console.WriteLine("4. Help");
            Console.WriteLine("5. Test hotkey listener (F3)");
            Console.WriteLine("6. Configure click type");
            Console.WriteLine("7. Exit");
            Console.ResetColor();
            Console.Write("\nSelect an option: ");
        }

        private static void ConfigureClickInterval()
        {
            Console.Write("\nEnter click interval in milliseconds: ");
            if (int.TryParse(Console.ReadLine(), out int interval) && interval > 0)
            {
                _settings.ClickInterval = interval;
                _configManager.SaveSettings(_settings);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Click interval set to {_settings.ClickInterval}ms");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please enter a positive number.");
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        private static void ConfigureClickType()
        {
            Console.Clear();
            Console.WriteLine("Configure Click Type");
            Console.WriteLine("===================");
            Console.WriteLine("1. Left Click");
            Console.WriteLine("2. Right Click");
            Console.WriteLine("3. Middle Click");
            Console.Write("\nSelect click type: ");
            
            string choice = Console.ReadLine() ?? string.Empty;
            switch (choice)
            {
                case "1":
                    _settings.ClickType = MouseButton.Left;
                    break;
                case "2":
                    _settings.ClickType = MouseButton.Right;
                    break;
                case "3":
                    _settings.ClickType = MouseButton.Middle;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid selection. Using default (Left Click)");
                    _settings.ClickType = MouseButton.Left;
                    Console.ResetColor();
                    break;
            }
            
            _configManager.SaveSettings(_settings);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nClick type set to: {_settings.ClickType}");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true);
        }

        private static void StartClicking()
        {
            if (_isRunning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Auto-clicking already running!");
                Console.ResetColor();
                return;
            }

            _isRunning = true;
            _clickerTokenSource = new CancellationTokenSource();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Auto-clicking started with {_settings.ClickInterval}ms interval, {_settings.ClickType} click");
            Console.ResetColor();

            // Start clicking in a background task with optimized performance
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("Press Alt+Ctrl+X to stop");

                    // More efficient clicking loop
                    var token = _clickerTokenSource.Token;
                    while (!token.IsCancellationRequested)
                    {
                        _autoClicker?.Click(_settings.ClickType);

                        // For very fast intervals, optimize differently
                        if (_settings.ClickInterval < 20)
                        {
                            // For extremely fast clicking, use more efficient approach
                            var sw = new Stopwatch();
                            sw.Start();
                            while (sw.ElapsedMilliseconds < _settings.ClickInterval && !token.IsCancellationRequested)
                            {
                                Thread.SpinWait(100); // More efficient than Sleep for very short waits
                            }
                        }
                        else
                        {
                            await Task.Delay(_settings.ClickInterval, token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, nothing to do
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in clicking task: {ex.Message}");
                }
            });
        }

        private static void StopClicking()
        {
            if (!_isRunning)
                return;

            _clickerTokenSource?.Cancel();
            _isRunning = false;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Auto-clicking stopped");
            Console.ResetColor();
        }

        private static void ShowHelpInfo()
        {
            Console.Clear();
            Console.WriteLine("Auto Clicker Help");
            Console.WriteLine("================");
            Console.WriteLine("- Use the menu to configure click interval and control clicking");
            Console.WriteLine("- Click types supported: Left, Right, Middle mouse buttons");
            Console.WriteLine("- Settings are automatically saved between sessions");
            Console.WriteLine("- Press Alt+Ctrl+S to start clicking at any time");
            Console.WriteLine("- Press Alt+Ctrl+X to stop clicking at any time");
            Console.WriteLine("- On Linux, xdotool must be installed for clicking to work");
            Console.WriteLine("\nPress any key to return to the menu...");
            Console.ReadKey(true);
        }

        private static void TestHotkeyListener()
        {
            Console.WriteLine("Testing hotkey listener...");
            Console.WriteLine("Press F3 to trigger the test hotkey.");
            Console.WriteLine("Press any key to return to the menu...");

            // Register a test hotkey (F3)
            _hotkeyManager?.RegisterHotkey(ModifierKeys.None, '3', () =>
            {
                Console.WriteLine("Test hotkey (F3) triggered!");
            });

            // Wait for the user to press any key to return to the menu
            Console.ReadKey(true);
        }

        private static void InitializePlatformSpecificComponents()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _autoClicker = new WindowsAutoClicker();
                _hotkeyManager = new WindowsHotkeyManager();
                Console.WriteLine("Windows mode initialized");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _autoClicker = new LinuxAutoClicker();
                _hotkeyManager = new LinuxHotkeyManager();
                Console.WriteLine("Linux mode initialized");
            }
            else
            {
                _autoClicker = new FallbackAutoClicker();
                _hotkeyManager = new FallbackHotkeyManager();
                Console.WriteLine("Fallback mode initialized (limited functionality)");
            }
        }
    }
}

