using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using CoreHook;

namespace SocketHook
{
    public class SocketHook : IEntryPoint
    {
        public SocketHook(IContext context) { }
        public void Run(IContext context)
        {

        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate SocketError DWSASend(
               IntPtr socket,
               IntPtr buffer,
               Int32 length,
               out IntPtr numberOfBytesSent,
               SocketFlags flags,
               IntPtr overlapped,
               IntPtr completionRoutine);

        private unsafe SocketError Detour_WsaSend(
            IntPtr socketHandle,
            WSABuffer* buffers,
            int bufferCount,
            out int bytesTransferred,
            SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine)
        {
            return Interop.Winsock.WSASend(socketHandle, buffers, bufferCount, out bytesTransferred, socketFlags, overlapped, completionRoutine);
        }
    }
}
