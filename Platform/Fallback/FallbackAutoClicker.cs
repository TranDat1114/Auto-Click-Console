using Auto_Click_Console.Platform.Interfaces;
using Auto_Click_Console.Utils;
using System;

namespace Auto_Click_Console.Platform.Fallback
{
    public class FallbackAutoClicker : IAutoClicker
    {
        public void Click(MouseButton button = MouseButton.Left)
        {
            // Just simulate a click with a message
            Console.WriteLine($"{button} Click (simulated - not available on this platform)");
        }
    }
}