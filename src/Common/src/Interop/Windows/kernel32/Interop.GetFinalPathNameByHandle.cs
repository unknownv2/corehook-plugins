using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern uint GetFinalPathNameByHandle(
            IntPtr file,
            [Out] char[] filePath,
            uint filePathSize,
            uint flags);
    }
}
