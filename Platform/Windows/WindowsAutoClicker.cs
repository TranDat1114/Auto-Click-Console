using Auto_Click_Console.Platform.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace Auto_Click_Console.Platform.Windows
{
    public class WindowsAutoClicker : IAutoClicker
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        
        public void Click()
        {
            // Simulate a left mouse click
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
    }
}