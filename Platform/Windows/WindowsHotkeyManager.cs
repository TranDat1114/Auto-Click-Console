using Auto_Click_Console.Platform.Interfaces;
using Auto_Click_Console.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Auto_Click_Console.Platform.Windows
{
    public class WindowsHotkeyManager : IHotkeyManager, IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        
        private IntPtr _hookHandle = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;  // Mark as readonly to prevent reassignment
        private readonly Dictionary<HotkeyCombo, Action> _hotkeyActions = new Dictionary<HotkeyCombo, Action>();
        private volatile bool _isListening = false;   // Use volatile for thread safety
        private Thread? _messageLoopThread;
        private bool _disposed = false;
        
        // Required to maintain a reference so the delegate doesn't get garbage collected
        private readonly Dictionary<ModifierKeys, bool> _modifierStates = new Dictionary<ModifierKeys, bool>
        {
            { ModifierKeys.Alt, false },
            { ModifierKeys.Control, false },
            { ModifierKeys.Shift, false },
            { ModifierKeys.Win, false }
        };

        // Constructor to initialize the proc delegate
        public WindowsHotkeyManager()
        {
            _proc = HookCallback;
        }

        private readonly struct HotkeyCombo : IEquatable<HotkeyCombo>
        {
            public ModifierKeys Modifiers { get; }
            public char Key { get; }

            public HotkeyCombo(ModifierKeys modifiers, char key)
            {
                Modifiers = modifiers;
                Key = char.ToUpper(key);
            }

            public bool Equals(HotkeyCombo other) =>
                Modifiers == other.Modifiers && Key == other.Key;

            public override bool Equals(object? obj) =>
                obj is HotkeyCombo combo && Equals(combo);

            public override int GetHashCode() =>
                HashCode.Combine(Modifiers, Key);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, 
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, 
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        
        // Function pointer for the hook procedure
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public void RegisterHotkey(ModifierKeys modifiers, char key, Action callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            
            var combo = new HotkeyCombo(modifiers, key);
            _hotkeyActions[combo] = callback;
            Console.WriteLine($"Registered global hotkey: {modifiers}+{char.ToUpper(key)}");
        }

        public void StartListening()
        {
            // Check if already disposed
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsHotkeyManager));
                
            if (_isListening) return;
            
            // Set the hook with current process module
            using (Process curProcess = Process.GetCurrentProcess())
            {
                ProcessModule? curModule = curProcess.MainModule;
                if (curModule != null)
                {
                    _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                        GetModuleHandle(curModule.ModuleName ?? string.Empty), 0);
                    
                    if (_hookHandle == IntPtr.Zero)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        Console.WriteLine($"Failed to set keyboard hook. Error code: {errorCode}");
                    }
                    else
                    {
                        _isListening = true;
                        Console.WriteLine("Global hotkey listener started");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to get main module handle");
                }
            }
            
            // Create a message loop to keep the hook active
            // This is run on a background thread
            _messageLoopThread = new Thread(() => 
            {
                while (_isListening)
                {
                    Thread.Sleep(10);
                }
            })
            { 
                IsBackground = true 
            };
            _messageLoopThread.Start();
        }

        public void StopListening()
        {
            // Check if already disposed
            if (_disposed) return;
            
            if (!_isListening) return;
            _isListening = false;
            
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
                Console.WriteLine("Global hotkey listener stopped");
            }
            
            // Give the message loop thread time to exit
            if (_messageLoopThread != null && _messageLoopThread.IsAlive)
            {
                try
                {
                    _messageLoopThread.Join(100);
                }
                catch (ThreadStateException) 
                {
                    // Thread may already be terminating
                }
            }
            _messageLoopThread = null;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                // Update modifier key states
                UpdateModifierState(vkCode);
                
                // Get currently active modifiers
                ModifierKeys activeModifiers = GetActiveModifiers();
                
                // Check if this is one of our hotkeys
                char keyChar = (char)vkCode;
                var combo = new HotkeyCombo(activeModifiers, keyChar);
                
                if (_hotkeyActions.TryGetValue(combo, out Action? callback) && callback != null)
                {
                    // Run the callback on a separate thread to avoid blocking the hook
                    ThreadPool.QueueUserWorkItem(_ => 
                    {
                        try
                        {
                            callback();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in hotkey callback: {ex.Message}");
                        }
                    });
                    
                    // Prevent other applications from receiving this key press
                    return (IntPtr)1;
                }
            }
            
            // Call the next hook in the chain
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }
        
        private void UpdateModifierState(int vkCode)
        {
            // VK_MENU (Alt): 0x12, VK_CONTROL: 0x11, VK_SHIFT: 0x10, VK_LWIN/VK_RWIN: 0x5B/0x5C
            if (vkCode == 0x12) _modifierStates[ModifierKeys.Alt] = true;
            else if (vkCode == 0x11) _modifierStates[ModifierKeys.Control] = true;
            else if (vkCode == 0x10) _modifierStates[ModifierKeys.Shift] = true;
            else if (vkCode == 0x5B || vkCode == 0x5C) _modifierStates[ModifierKeys.Win] = true;
        }
        
        private ModifierKeys GetActiveModifiers()
        {
            ModifierKeys result = ModifierKeys.None;
            
            // Check the state of modifier keys using GetAsyncKeyState
            // Alt (VK_MENU: 0x12), Ctrl (VK_CONTROL: 0x11), Shift (VK_SHIFT: 0x10), Win (VK_LWIN: 0x5B)
            if ((GetAsyncKeyState(0x12) & 0x8000) != 0) result |= ModifierKeys.Alt;
            if ((GetAsyncKeyState(0x11) & 0x8000) != 0) result |= ModifierKeys.Control;
            if ((GetAsyncKeyState(0x10) & 0x8000) != 0) result |= ModifierKeys.Shift;
            if ((GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0) 
                result |= ModifierKeys.Win;
                
            return result;
        }

        #region IDisposable Implementation
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
                // Dispose managed resources
                StopListening();
            }
            
            // Dispose unmanaged resources
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            
            _disposed = true;
        }
        
        ~WindowsHotkeyManager()
        {
            Dispose(false);
        }
        
        #endregion
    }
}