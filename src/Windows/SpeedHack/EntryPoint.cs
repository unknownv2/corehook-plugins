using System;
using System.Runtime.InteropServices;
using CoreHook;

namespace SpeedHack
{
    public class SpeedHack : IEntryPoint
    {
        IHook GetTickCount;
        IHook GetTickCount64;
        IHook QueryPerformanceCounter;
        IHook SleepEx;

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate uint GetTickCountDel();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate uint GetTickCount64Del();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate uint QueryPerformanceCounterDel(out long value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate uint SleepExDel(uint milliSeconds, Interop.BOOL alertable);

        private float _speedMultiplier = 0.0f;

        public SpeedHack(IContext context, float arg1) { }
        public void Run(IContext context, float speedMultiplier)
        {
            _speedMultiplier = speedMultiplier;

            // Create detours for implementing the speed hack
            GetTickCount = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "GetTickCount"),
                new GetTickCountDel(Detour_GetTickCount),
                this);

            GetTickCount64 = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "GetTickCount64"),
                new GetTickCount64Del(Detour_GetTickCount64),
                this);

            QueryPerformanceCounter = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "QueryPerformanceCounter"),
                new QueryPerformanceCounterDel(Detour_QueryPerformanceCounter),
                this);

            SleepEx = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "SleepEx"),
                new SleepExDel(Detour_SleepEx),
                this);

            // Enable for all threads except the current thread.
            GetTickCount.ThreadACL.SetExclusiveACL(new int[] { 0 });
            GetTickCount64.ThreadACL.SetExclusiveACL(new int[] { 0 });
            QueryPerformanceCounter.ThreadACL.SetExclusiveACL(new int[] { 0 });
            SleepEx.ThreadACL.SetExclusiveACL(new int[] { 0 });
        }

        internal uint Detour_GetTickCount()
        {

            return 0;
        }
        internal uint Detour_GetTickCount64()
        {

            return 0;
        }
        internal uint Detour_QueryPerformanceCounter(out long value)
        {
            value = 0;
            return 0;
        }
        internal uint Detour_SleepEx(uint milliSeconds, Interop.BOOL alertable)
        {
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;
            return Interop.Kernel32.SleepEx((uint)(milliSeconds / This._speedMultiplier), alertable);
        }
    }
}
