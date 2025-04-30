using System;
using System.Runtime.InteropServices;

namespace Auto_Click_Console.Utils
{
    public class UIManager
    {
        public void ShowMenu(int clickInterval, bool isRunning)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Cross-Platform Auto Clicker");
            Console.WriteLine("==========================");
            Console.ResetColor();
            Console.WriteLine($"Current click interval: {clickInterval}ms");
            Console.WriteLine($"Status: {(isRunning ? "Running" : "Stopped")}");
            Console.WriteLine($"Platform: {RuntimeInformation.OSDescription}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("1. Configure click interval");
            Console.WriteLine("2. Start clicking (F1)");
            Console.WriteLine("3. Stop clicking (F2)");
            Console.WriteLine("4. Help");
            Console.WriteLine("5. Test hotkey listener (F3)");
            Console.WriteLine("6. Exit");
            Console.ResetColor();
            Console.Write("\nSelect an option: ");
        }

        public void ShowHelpInfo()
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

        public int ConfigureClickInterval(int currentInterval)
        {
            Console.Write("\nEnter click interval in milliseconds: ");
            if (int.TryParse(Console.ReadLine(), out int interval) && interval > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Click interval set to {interval}ms");
                Console.ResetColor();
                return interval;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please enter a positive number.");
                Console.ResetColor();
                return currentInterval;
            }
        }
    }
}