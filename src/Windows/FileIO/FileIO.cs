using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CoreHook;

namespace FileIO
{
    public class FileIO : IEntryPoint
    {
        /// <summary>
        /// List of files that have been read or written to, with a counter to keep track of the number of accesses.
        /// </summary>
        private Dictionary<string, int> FileList = new Dictionary<string, int>();

        /// <summary>
        ///  The max length of a file path on Windows.
        /// </summary>
        public uint MaxPathLength = 260;

        /// <summary>
        /// Hook handle for the kernel32.dll!ReadFile function.
        /// </summary>
        private IHook _readFileHook;
        /// <summary>
        /// Hook handle for the kernel32.dll!WriteFile function.
        /// </summary>
        private IHook _writeFileHook;

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        internal delegate int ReadFileDelegate(IntPtr handle,
            IntPtr bytes,
            int numBytesToRead,
            out int numBytesRead,
            IntPtr mustBeZero);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        internal delegate int WriteFileDelegate(IntPtr handle,
            IntPtr bytes,
            int numBytesToWrite,
            out int numBytesWritten,
            IntPtr overlapped);

        public FileIO(IContext context) { }

        /// <summary>
        /// Initialize hooks for our file I/O functions.
        /// </summary>
        /// <param name="context"></param>
        public void Run(IContext context)
        {
            _readFileHook = HookFactory.CreateHook<ReadFileDelegate>(LocalHook.GetProcAddress(Interop.Libraries.Kernel32, "ReadFile"), Detour_ReadFile, this);
            _writeFileHook = HookFactory.CreateHook<WriteFileDelegate>(LocalHook.GetProcAddress(Interop.Libraries.Kernel32, "WriteFile"), Detour_WriteFile, this);

            DisplayFileAccess().GetAwaiter().GetResult();
        }

        private async Task DisplayFileAccess()
        {
            // Ensure we are running in a new thread
            await Task.Yield();

            // Enable detours for all threads except the current thread
            _readFileHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
            _writeFileHook.ThreadACL.SetExclusiveACL(new int[] { 0 });

            try
            {
                while (true)
                {
                    Thread.Sleep(500);

                    lock (FileList)
                    {
                        if (FileList.Count > 0)
                        {
                            foreach (var file in FileList)
                            {
                                Console.WriteLine($"{file.Key} was accessed {file.Value} time(s).");
                            }
                        }
                    }
                }
            }
            catch
            {

            }
        }

        private int Detour_ReadFile(
            IntPtr handle,
            IntPtr bytes,
            int numBytesToRead,
            out int numBytesRead,
            IntPtr mustBeZero)
        {
            FileIO This = (FileIO)HookRuntimeInfo.Callback;
            if (This != null)
            {
                // Get the file name from the handle and increment the access count
                char[] filePath = new char[This.MaxPathLength];
                uint filePathLength = Interop.Kernel32.GetFinalPathNameByHandle(handle, filePath, This.MaxPathLength, 0);

                // Check file name and increment the access count if valid
                IncrementFileAccessCount(This.FileList, new string(filePath, 0, (int)filePathLength));
            }
            return Interop.Kernel32.ReadFile(handle, bytes, numBytesToRead, out numBytesRead, mustBeZero);
        }

        private int Detour_WriteFile(IntPtr handle,
            IntPtr bytes,
            int numBytesToWrite,
            out int numBytesWritten,
            IntPtr overlapped)
        {
            FileIO This = (FileIO)HookRuntimeInfo.Callback;
            if (This != null)
            {
                // Get the file name from the handle 
                char[] filePath = new char[This.MaxPathLength];
                uint filePathLength = Interop.Kernel32.GetFinalPathNameByHandle(handle, filePath, This.MaxPathLength, 0);

                // Check file name and increment the access count if valid
                IncrementFileAccessCount(This.FileList, new string(filePath, 0, (int)filePathLength));
            }
            return Interop.Kernel32.WriteFile(handle, bytes, numBytesToWrite, out numBytesWritten, overlapped);
        }

        /// <summary>
        /// Increase the number of times a file was accessed from an I/O function.
        /// </summary>
        /// <param name="fileList">The list of files and their current access count.</param>
        /// <param name="fileName">The name of the file being accessed.</param>
        private void IncrementFileAccessCount(Dictionary<string, int> fileList, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                lock (fileList)
                {
                    fileList[fileName] = fileList.ContainsKey(fileName) ? fileList[fileName] + 1 : 1;
                }
            }
        }
    }
}
