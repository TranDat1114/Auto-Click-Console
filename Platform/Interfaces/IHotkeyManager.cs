using Auto_Click_Console.Utils;
using System;

namespace Auto_Click_Console.Platform.Interfaces
{
    public interface IHotkeyManager
    {
        void RegisterHotkey(ModifierKeys modifiers, char key, Action callback);
        void StartListening();
        void StopListening();
    }
}