using Auto_Click_Console.Platform.Interfaces;
using Auto_Click_Console.Utils;
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
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        private const int MOUSEEVENTF_MIDDLEUP = 0x40;
        
        public void Click(MouseButton button = MouseButton.Left)
        {
            int downFlag;
            int upFlag;
            
            switch (button)
            {
                case MouseButton.Right:
                    downFlag = MOUSEEVENTF_RIGHTDOWN;
                    upFlag = MOUSEEVENTF_RIGHTUP;
                    break;
                case MouseButton.Middle:
                    downFlag = MOUSEEVENTF_MIDDLEDOWN;
                    upFlag = MOUSEEVENTF_MIDDLEUP;
                    break;
                case MouseButton.Left:
                default:
                    downFlag = MOUSEEVENTF_LEFTDOWN;
                    upFlag = MOUSEEVENTF_LEFTUP;
                    break;
            }
            
            // Simulate a mouse click
            mouse_event(downFlag, 0, 0, 0, 0);
            mouse_event(upFlag, 0, 0, 0, 0);
        }
    }
}