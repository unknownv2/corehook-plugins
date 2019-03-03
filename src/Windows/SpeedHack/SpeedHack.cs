using System;
using System.Runtime.InteropServices;
using CoreHook;

namespace SpeedHack
{
    public class SpeedHack : IEntryPoint
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate uint GetTickCountDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate ulong GetTickCount64Delegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate Interop.BOOL QueryPerformanceCounterDelegate(out long performanceCount);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate uint SleepExDelegate(uint milliSeconds, Interop.BOOL alertable);

        private IHook _getTickCount;
        private IHook _getTickCount64;
        private IHook _queryPerformanceCounter;
        private IHook _sleepEx;

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
            _getTickCount = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "GetTickCount"),
                new GetTickCountDelegate(Detour_GetTickCount),
                this);
            
            _getTickCount64 = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "GetTickCount64"),
                new GetTickCount64Delegate(Detour_GetTickCount64),
                this);
           
            _queryPerformanceCounter = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "QueryPerformanceCounter"),
                new QueryPerformanceCounterDelegate(Detour_QueryPerformanceCounter),
                this);
            
           _sleepEx = LocalHook.Create(
               LocalHook.GetProcAddress("kernel32.dll", "SleepEx"),
               new SleepExDelegate(Detour_SleepEx),
               this);

            // Enable for all threads except the current thread.
            _getTickCount.ThreadACL.SetExclusiveACL(new int[] { 0 });
            _getTickCount64.ThreadACL.SetExclusiveACL(new int[] { 0 });
            _queryPerformanceCounter.ThreadACL.SetExclusiveACL(new int[] { 0 });
            _sleepEx.ThreadACL.SetExclusiveACL(new int[] { 0 });
        }

        private uint Detour_GetTickCount()
        {
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;
            var tickCount = Interop.Kernel32.GetTickCount();
            return (uint)(This._baseTime + ((tickCount - This._baseTime)) * This._acceleration);
        }

        private ulong Detour_GetTickCount64()
        {
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;
            var tickCount = Interop.Kernel32.GetTickCount64();
            return (ulong)(This._baseTime64 + ((tickCount - This._baseTime64)) * This._acceleration);
        }

        private Interop.BOOL Detour_QueryPerformanceCounter(out long performanceCount)
        {
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;

            var result = Interop.Kernel32.QueryPerformanceCounter(out long realPerformanceCount);
            performanceCount = (long)(This._basePerformanceCount + ((realPerformanceCount - This._basePerformanceCount)) * This._acceleration);
            return result;
        }

        private uint Detour_SleepEx(uint milliSeconds, Interop.BOOL alertable)
        {
            SpeedHack This = (SpeedHack)HookRuntimeInfo.Callback;
            return Interop.Kernel32.SleepEx((uint)(milliSeconds / This._acceleration), alertable);
        }
    }
}
