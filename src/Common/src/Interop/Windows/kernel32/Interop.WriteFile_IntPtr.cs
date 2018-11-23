using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern int WriteFile(
            IntPtr handle,
            byte[] bytes,
            int numBytesToWrite,
            out int numBytesWritten,
            IntPtr mustBeZero);
    }
}
