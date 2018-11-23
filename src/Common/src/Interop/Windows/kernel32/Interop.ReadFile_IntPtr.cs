using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern int ReadFile(
            IntPtr handle,
            byte[] bytes,
            int numBytesToRead,
            out int numBytesRead,
            IntPtr mustBeZero);
    }
}
