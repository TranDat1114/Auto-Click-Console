using System;
using Auto_Click_Console.Utils;

namespace Auto_Click_Console.Platform.Interfaces
{
    public interface IAutoClicker
    {
        void Click(MouseButton button = MouseButton.Left);
    }
}