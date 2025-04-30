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
        private static bool _isRunning = false;
        private static int _clickInterval = 100; // Default to 100ms
        private static CancellationTokenSource? _clickerTokenSource;
        private static IAutoClicker? _autoClicker;
        private static IHotkeyManager? _hotkeyManager;
        private static bool _needsMenuRefresh = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Cross-Platform Auto Clicker");
            Console.WriteLine("==========================");
            
            InitializePlatformSpecificComponents();
            
            bool exitRequested = false;
            
            // Register only the Alt+Ctrl hotkeys
            _hotkeyManager?.RegisterHotkey(ModifierKeys.Control | ModifierKeys.Alt, 'S', () => SafeUIUpdate(StartClicking));
            _hotkeyManager?.RegisterHotkey(ModifierKeys.Control | ModifierKeys.Alt, 'X', () => SafeUIUpdate(StopClicking));
            
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
                    // Reduce CPU usage when idle
                    await Task.Delay(50);
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
            Console.WriteLine("Cross-Platform Auto Clicker");
            Console.WriteLine("==========================");
            Console.WriteLine($"Current click interval: {_clickInterval}ms");
            Console.WriteLine($"Status: {(_isRunning ? "RUNNING" : "STOPPED")}");
            Console.WriteLine();
            Console.WriteLine("1. Configure click interval");
            Console.WriteLine("2. Start clicking (Alt+Ctrl+S)");
            Console.WriteLine("3. Stop clicking (Alt+Ctrl+X)");
            Console.WriteLine("4. Help");
            Console.WriteLine("5. Exit");
            Console.Write("\nSelect an option: ");
        }
        
        private static void ConfigureClickInterval()
        {
            Console.Write("\nEnter click interval in milliseconds: ");
            if (int.TryParse(Console.ReadLine(), out int interval) && interval > 0)
            {
                _clickInterval = interval;
                Console.WriteLine($"Click interval set to {_clickInterval}ms");
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a positive number.");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
        
        private static void StartClicking()
        {
            if (_isRunning)
            {
                Console.WriteLine("Auto-clicking already running!");
                return;
            }

            _isRunning = true;
            _clickerTokenSource = new CancellationTokenSource();
            
            // Start clicking in a background task with optimized performance
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"Auto-clicking started with {_clickInterval}ms interval");
                    Console.WriteLine("Press Alt+Ctrl+X to stop");
                    
                    // More efficient clicking loop
                    var token = _clickerTokenSource.Token;
                    while (!token.IsCancellationRequested)
                    {
                        _autoClicker?.Click();
                        
                        // For very fast intervals, optimize differently
                        if (_clickInterval < 20)
                        {
                            // For extremely fast clicking, use more efficient approach
                            var sw = new Stopwatch();
                            sw.Start();
                            while (sw.ElapsedMilliseconds < _clickInterval && !token.IsCancellationRequested)
                            {
                                Thread.SpinWait(100); // More efficient than Sleep for very short waits
                            }
                        }
                        else
                        {
                            await Task.Delay(_clickInterval, token);
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
            Console.WriteLine("Auto-clicking stopped");
        }
        
        private static void ShowHelpInfo()
        {
            Console.Clear();
            Console.WriteLine("Auto Clicker Help");
            Console.WriteLine("================");
            Console.WriteLine("- Use the menu to configure click interval and control clicking");
            Console.WriteLine("- Press Alt+Ctrl+S to start clicking at any time");
            Console.WriteLine("- Press Alt+Ctrl+X to stop clicking at any time");
            Console.WriteLine("- On Linux, xdotool must be installed for clicking to work");
            Console.WriteLine("\nPress any key to return to the menu...");
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

