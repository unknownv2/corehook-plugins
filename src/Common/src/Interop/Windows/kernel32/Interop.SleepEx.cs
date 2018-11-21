
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32)]
        internal static extern uint SleepEx(uint milliSeconds, BOOL alertable);
    }
}