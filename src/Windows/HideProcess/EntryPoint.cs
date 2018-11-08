using System;
using System.Runtime.InteropServices;
using CoreHook;

namespace HideProcess
{
    public class HideProcess : IEntryPoint
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        internal delegate int NtQuerySystemInformationT(int query, IntPtr dataPtr, int size, out int returnedSize);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SystemProcessInformation
        {
            internal uint NextEntryOffset;
            internal uint NumberOfThreads;
            private fixed byte Reserved1[48];
            internal Interop.UNICODE_STRING ImageName;
            internal int BasePriority;
            internal IntPtr UniqueProcessId;
            private UIntPtr Reserved2;
            internal uint HandleCount;
            internal uint SessionId;
            private UIntPtr Reserved3;
            internal UIntPtr PeakVirtualSize;  // SIZE_T
            internal UIntPtr VirtualSize;
            private uint Reserved4;
            internal UIntPtr PeakWorkingSetSize;  // SIZE_T
            internal UIntPtr WorkingSetSize;  // SIZE_T
            private UIntPtr Reserved5;
            internal UIntPtr QuotaPagedPoolUsage;  // SIZE_T
            private UIntPtr Reserved6;
            internal UIntPtr QuotaNonPagedPoolUsage;  // SIZE_T
            internal UIntPtr PagefileUsage;  // SIZE_T
            internal UIntPtr PeakPagefileUsage;  // SIZE_T
            internal UIntPtr PrivatePageCount;  // SIZE_T
            private fixed long Reserved7[6];
        }

        /// <summary>
        /// Handle to the ntdll!NtQuerySystemInformation function hook.
        /// </summary>
        IHook QuerySysInfo;

        /// <summary>
        /// The name of the process to hide, for example: notepad.
        /// </summary>
        internal string ProcessName;

        public HideProcess(object context, string arg1) { }

        public void Run(object context, string processName)
        {
            // Save the process name to filter out of the list
            ProcessName = processName;

            // Detour the ntdll.dll!NtQuerySystemInformation function
            string[] functionName = new string[] { "ntdll.dll", "NtQuerySystemInformation" };

            QuerySysInfo = LocalHook.Create(
                LocalHook.GetProcAddress(functionName[0], functionName[1]),
                new NtQuerySystemInformationT(Detour_NtQuerySystemInformation),
                this);

            // Active the detour for all threads
            QuerySysInfo.Enabled = true;
        }

        /// <summary>
        /// Remove a process the list returned by NtQuerySystemInformation.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="dataPtr"></param>
        /// <param name="size"></param>
        /// <param name="returnedSize"></param>
        /// <returns></returns>
        internal static unsafe int Detour_NtQuerySystemInformation(int query, IntPtr dataPtr, int size, out int returnedSize)
        {
            HideProcess This = (HideProcess)HookRuntimeInfo.Callback;

            var status = Interop.NtDll.NtQuerySystemInformation(query, dataPtr, size, out returnedSize);
       
            if (status == 0 && query == Interop.NtDll.NtQuerySystemProcessInformation && dataPtr != IntPtr.Zero && This != null)
            {
                long totalOffset = 0;
                while (true)
                {
                    IntPtr currentPtr = (IntPtr)((long)dataPtr + totalOffset);
                    ref SystemProcessInformation pi = ref *(SystemProcessInformation*)currentPtr;
                    ref SystemProcessInformation nextPi = ref *(SystemProcessInformation*)(IntPtr)((long)currentPtr + pi.NextEntryOffset);

                    if (nextPi.ImageName.Buffer != IntPtr.Zero)
                    {
                        string processName = GetProcessShortName(
                            Marshal.PtrToStringUni(nextPi.ImageName.Buffer,
                            nextPi.ImageName.Length / sizeof(char)));

                        if (processName.Contains(This.ProcessName))
                        {
                            if (nextPi.NextEntryOffset == 0)
                            {
                                pi.NextEntryOffset = 0;
                            }
                            else
                            {
                                pi.NextEntryOffset += nextPi.NextEntryOffset;
                            }
                            nextPi = pi;
                        }
                    }
                    if (pi.NextEntryOffset == 0)
                    {
                        break;
                    }
                    totalOffset += pi.NextEntryOffset;
                }
            }
            return status;
        }

        /// <summary>
        /// Get the name of a process.
        /// </summary>
        /// <param name="name">The image path name.</param>
        /// <returns>The process name</returns>
        private static string GetProcessShortName(string name)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            int slash = -1;
            int period = -1;

            for(int i = 0; i < name.Length; ++i)
            {
                if(name[i] == '\\')
                {
                    slash = i;
                }
                else if(name[i] == '.')
                {
                    period = i;
                }
            }
            if(period == -1)
            {
                // Set index to the end of the string
                period = name.Length - 1;
            }
            else
            {
                string extension = name.Substring(period);

                // Remove the '.exe' extension from the process name,
                // otherwise remove the extension
                if(string.Equals(".exe", extension, StringComparison.OrdinalIgnoreCase))
                {
                    // Set index to the character before the period
                    period--;
                }
                else
                {
                    // Set the index to the end of the string
                    period = name.Length - 1;
                }
            }

            if(slash == -1)
            {
                // Set index to the start of the string
                slash = 0;
            }
            else
            {
                // Set to the index of the next character
                // after the slash
                slash++;
            }

            return name.Substring(slash, period - slash + 1);
        }
    }
}
