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
        private delegate ulong GetTickCount64Del();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate Interop.BOOL QueryPerformanceCounterDel(out long performanceCount);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate uint SleepExDel(uint milliSeconds, Interop.BOOL alertable);

        private float _acceleration;

        // Initial time from GetTickCount
        private uint _baseTime;
        // Initial time from GetTickCount64
        private ulong _baseTime64;
        // Intial value of the performance counter
        private long _basePerformanceCount;

        public SpeedHack(IContext context, float arg1) { }

        public void Run(IContext context, float acceleration)
        {
            _acceleration = acceleration;

            // Get current counts to use as a base for modification
            _baseTime = Interop.Kernel32.GetTickCount();
            _baseTime64 = Interop.Kernel32.GetTickCount64();
            Interop.Kernel32.QueryPerformanceCounter(out _basePerformanceCount);

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
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;
            var tickCount = Interop.Kernel32.GetTickCount();
            return (uint)(This._baseTime + ((tickCount - This._baseTime)) * This._acceleration);
        }

        internal ulong Detour_GetTickCount64()
        {
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;
            var tickCount = Interop.Kernel32.GetTickCount64();
            return (ulong)(This._baseTime + ((tickCount - This._baseTime64)) * This._acceleration);
        }

        internal Interop.BOOL Detour_QueryPerformanceCounter(out long performanceCount)
        {
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;
            if(Interop.Kernel32.QueryPerformanceCounter(out performanceCount) == Interop.BOOL.FALSE)
            {
                return Interop.BOOL.FALSE;
            }
            performanceCount = (long)(This._basePerformanceCount + ((performanceCount - This._basePerformanceCount)) * This._acceleration);
            return Interop.BOOL.TRUE;
        }

        internal uint Detour_SleepEx(uint milliSeconds, Interop.BOOL alertable)
        {
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;
            return Interop.Kernel32.SleepEx((uint)(milliSeconds / This._acceleration), alertable);
        }
    }
}
