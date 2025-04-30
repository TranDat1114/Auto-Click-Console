using Auto_Click_Console.Platform.Interfaces;
using Auto_Click_Console.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Auto_Click_Console.Platform.Linux
{
    public class LinuxHotkeyManager : IHotkeyManager
    {
        private readonly Dictionary<string, Action> _hotkeyCallbacks = new();
        private bool _isListening = false;
        private Process? _xbindkeysProcess;
        private string? _configPath;
        private Thread? _monitorThread;
        private readonly string _hotkeySignalPath = "/tmp/autoclicker_hotkey";
        private FileSystemWatcher? _watcher;

        public void RegisterHotkey(ModifierKeys modifiers, char key, Action callback)
        {
            string hotkeyId = $"{modifiers}+{char.ToUpper(key)}";
            _hotkeyCallbacks[hotkeyId] = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void StartListening()
        {
            if (_isListening) return;
            
            // Check if xbindkeys is installed - use async process to avoid blocking
            try
            {
                using var checkProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "xbindkeys",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                
                checkProcess?.WaitForExit(500); // Add timeout to avoid hanging
                
                if (checkProcess?.ExitCode != 0)
                {
                    Console.WriteLine("xbindkeys not found. Falling back to console-based hotkeys.");
                    StartConsoleFallback();
                    return;
                }
            }
            catch
            {
                Console.WriteLine("Could not check for xbindkeys. Falling back to console-based hotkeys.");
                StartConsoleFallback();
                return;
            }

            _isListening = true;
            
            // Create a temporary config file for xbindkeys (once)
            _configPath = Path.Combine(Path.GetTempPath(), ".xbindkeysrc_autoclicker");
            CreateXbindkeysConfig();
            
            try
            {
                // Start xbindkeys with our config
                _xbindkeysProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "xbindkeys",
                    Arguments = $"-f {_configPath}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                
                // Start monitoring for hotkey signals in a more efficient way
                StartEfficientMonitoring();
                
                Console.WriteLine("Hotkey listener started (using xbindkeys)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting xbindkeys: {ex.Message}");
                Console.WriteLine("Falling back to console-based hotkeys.");
                StartConsoleFallback();
            }
        }

        private void StartEfficientMonitoring()
        {
            // Ensure signal file exists
            if (!File.Exists(_hotkeySignalPath))
            {
                File.WriteAllText(_hotkeySignalPath, string.Empty);
            }
            
            // Create watcher as a field to properly dispose later
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(_hotkeySignalPath) ?? ".")
            {
                Filter = Path.GetFileName(_hotkeySignalPath),
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            
            _watcher.Changed += (sender, e) => 
            {
                try
                {
                    // Use a more efficient approach to read the file
                    string? hotkeyId = null;
                    
                    // Retry with backoff to handle file lock issues
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            hotkeyId = File.ReadAllText(_hotkeySignalPath).Trim();
                            break;
                        }
                        catch when (attempt < 2)
                        {
                            Thread.Sleep(20 * (attempt + 1)); // Exponential backoff
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(hotkeyId) && _hotkeyCallbacks.TryGetValue(hotkeyId, out Action? callback))
                    {
                        callback?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading hotkey signal: {ex.Message}");
                }
            };
        }

        private void StartConsoleFallback()
        {
            _isListening = true;
            
            _monitorThread = new Thread(() =>
            {
                Console.WriteLine("Hotkey listener started (console mode)");
                
                while (_isListening)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                        
                        ModifierKeys modifiers = ModifierKeys.None;
                        if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                            modifiers |= ModifierKeys.Control;
                        if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0)
                            modifiers |= ModifierKeys.Alt;
                        if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0)
                            modifiers |= ModifierKeys.Shift;
                        
                        string hotkeyId = $"{modifiers}+{char.ToUpper(keyInfo.KeyChar)}";
                        
                        if (_hotkeyCallbacks.TryGetValue(hotkeyId, out Action? callback))
                        {
                            callback?.Invoke();
                        }
                    }
                    
                    // Small delay to reduce CPU usage
                    Thread.Sleep(20);
                }
            })
            {
                IsBackground = true
            };
            
            _monitorThread.Start();
        }

        public void StopListening()
        {
            if (!_isListening) return;
            _isListening = false;
            
            _watcher?.Dispose();
            
            if (_xbindkeysProcess != null && !_xbindkeysProcess.HasExited)
            {
                try
                {
                    _xbindkeysProcess.Kill();
                    _xbindkeysProcess.Dispose();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            // Try to delete the config file
            if (!string.IsNullOrEmpty(_configPath) && File.Exists(_configPath))
            {
                try
                {
                    File.Delete(_configPath);
                }
                catch
                {
                    // Ignore file deletion errors
                }
            }
        }
        
        private void CreateXbindkeysConfig()
        {
            if (string.IsNullOrEmpty(_configPath)) return;
            
            List<string> configLines = new();
            
            foreach (var hotkey in _hotkeyCallbacks)
            {
                string[] parts = hotkey.Key.Split('+');
                if (parts.Length < 2) continue;
                
                ModifierKeys mods;
                if (!Enum.TryParse(parts[0], out mods)) continue;
                
                char key = parts[1][0];
                
                string hotkeyString = ConvertToXbindkeysFormat(mods, key);
                
                // Command to write to signal file
                string command = $"echo '{hotkey.Key}' > {_hotkeySignalPath}";
                
                // Add to config
                configLines.Add($"\"{command}\"");
                configLines.Add($"  {hotkeyString}");
                configLines.Add("");
            }
            
            File.WriteAllLines(_configPath, configLines);
        }
        
        private string ConvertToXbindkeysFormat(ModifierKeys modifiers, char key)
        {
            List<string> mods = new();
            
            if ((modifiers & ModifierKeys.Control) != 0) mods.Add("Control");
            if ((modifiers & ModifierKeys.Alt) != 0) mods.Add("Alt");
            if ((modifiers & ModifierKeys.Shift) != 0) mods.Add("Shift");
            if ((modifiers & ModifierKeys.Win) != 0) mods.Add("Mod4");
            
            string keyName = char.ToUpper(key).ToString();
            
            return string.Join("+", mods) + " + " + keyName;
        }
    }
}