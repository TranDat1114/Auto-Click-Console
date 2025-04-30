using Auto_Click_Console.Platform.Interfaces;
using Auto_Click_Console.Utils;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Auto_Click_Console.Platform.Fallback
{
    public class FallbackHotkeyManager : IHotkeyManager
    {
        private readonly Dictionary<string, Action> _hotkeyCallbacks = new();
        private bool _isListening = false;
        private Thread? _keyListenerThread;
        
        public void RegisterHotkey(ModifierKeys modifiers, char key, Action callback)
        {
            string hotkeyString = $"{modifiers}+{char.ToUpper(key)}";
            _hotkeyCallbacks[hotkeyString] = callback ?? throw new ArgumentNullException(nameof(callback));
        }
        
        public void StartListening()
        {
            _isListening = true;
            
            _keyListenerThread = new Thread(() =>
            {
                Console.WriteLine("Hotkey listener started (console-based implementation)");
                
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
                        
                        string hotkeyString = $"{modifiers}+{char.ToUpper(keyInfo.KeyChar)}";
                        
                        if (_hotkeyCallbacks.TryGetValue(hotkeyString, out Action? callback))
                        {
                            callback?.Invoke();
                        }
                    }
                    
                    // Small delay to reduce CPU usage
                    Thread.Sleep(10);
                }
            });
            
            _keyListenerThread.IsBackground = true;
            _keyListenerThread.Start();
        }
        
        public void StopListening()
        {
            _isListening = false;
        }
    }
}