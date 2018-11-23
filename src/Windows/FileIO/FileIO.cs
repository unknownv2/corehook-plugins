using System;
using CoreHook;

namespace FileIO
{
    public class FileIO : IEntryPoint
    {
        public FileIO(IContext context) { }
        
        public void Run(IContext contex)
        {

        }

        private int Detour_ReadFile(
            IntPtr handle,
            byte[] bytes,
            int numBytesToRead,
            out int numBytesRead,
            IntPtr mustBeZero)
        {
            return Interop.Kernel32.ReadFile(handle, bytes, numBytesToRead, out numBytesRead, mustBeZero);
        }

        private int Detour_WriteFile(IntPtr handle,
            byte[] bytes,
            int numBytesToWrite,
            out int numBytesWritten,
            IntPtr mustBeZero)
        {
            return Interop.Kernel32.WriteFile(handle, bytes, numBytesToWrite, out numBytesWritten, mustBeZero);
        }
    }
}
