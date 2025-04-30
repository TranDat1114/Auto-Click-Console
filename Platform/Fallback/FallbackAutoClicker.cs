using Auto_Click_Console.Platform.Interfaces;
using System;

namespace Auto_Click_Console.Platform.Fallback
{
    public class FallbackAutoClicker : IAutoClicker
    {
        public void Click()
        {
            // Just simulate a click with a message
            Console.WriteLine("Click (simulated - not available on this platform)");
        }
    }
}