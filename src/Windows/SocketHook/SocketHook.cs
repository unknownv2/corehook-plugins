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

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate SocketError DWSARecv(
            IntPtr socketHandle,
            WSABuffer* buffer,
            int bufferCount,
            out int bytesTransferred,
            ref SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine);

        private static unsafe SocketError Detour_WSARecv(
            IntPtr socketHandle,
            ref WSABuffer buffer,
            int bufferCount,
            out int bytesTransferred,
            ref SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine)
        {
            WSABuffer localBuffer = buffer;
            return Interop.Winsock.WSARecv(socketHandle, &localBuffer, bufferCount, out bytesTransferred, ref socketFlags, overlapped, completionRoutine);
        }

        internal unsafe delegate int Dsend(
             [In] IntPtr socketHandle,
             [In] byte* pinnedBuffer,
             [In] int len,
             [In] SocketFlags socketFlags);

        internal unsafe delegate int Drecv(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags);

        private unsafe int Detour_recv(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags)
        {
            return Interop.Winsock.recv(socketHandle, pinnedBuffer, len, socketFlags);
        }

        private unsafe int Detour_send(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags)
        {
            return Interop.Winsock.send(socketHandle, pinnedBuffer, len, socketFlags);
        }
    }
}
