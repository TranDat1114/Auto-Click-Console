using Auto_Click_Console.Platform.Interfaces;
using Auto_Click_Console.Utils;
using System;
using System.Diagnostics;

namespace Auto_Click_Console.Platform.Linux
{
    public class LinuxAutoClicker : IAutoClicker
    {
        private readonly bool _xdotoolAvailable;
        
        public LinuxAutoClicker()
        {
            // Check if xdotool is available
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = "xdotool",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                if (process.Start())
                {
                    // Add timeout to prevent hanging
                    bool exited = process.WaitForExit(2000);
                    _xdotoolAvailable = exited && process.ExitCode == 0;
                }
                else
                {
                    _xdotoolAvailable = false;
                }
                
                if (!_xdotoolAvailable)
                {
                    Console.WriteLine("Warning: xdotool not found. Auto-clicking will be simulated.");
                    Console.WriteLine("Install with: sudo apt-get install xdotool");
                }
            }
            catch (Exception ex)
            {
                _xdotoolAvailable = false;
                Console.WriteLine($"Warning: Could not check for xdotool: {ex.Message}. Auto-clicking will be simulated.");
            }
        }
        
        public void Click(MouseButton button = MouseButton.Left)
        {
            if (_xdotoolAvailable)
            {
                try
                {
                    // Convert MouseButton enum to xdotool button number (1=left, 3=right, 2=middle)
                    int buttonNum = button switch
                    {
                        MouseButton.Left => 1,
                        MouseButton.Right => 3,
                        MouseButton.Middle => 2,
                        _ => 1
                    };
                    
                    // Use xdotool to simulate a click at the current cursor position
                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "xdotool",
                            Arguments = $"click {buttonNum}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    
                    if (!process.Start())
                    {
                        Console.WriteLine("Failed to start xdotool process");
                    }
                    else
                    {
                        // Optional: wait for the process to finish with a reasonable timeout
                        // process.WaitForExit(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing xdotool: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"{button} Click (simulated - install xdotool for actual clicking)");
            }
        }
    }
}