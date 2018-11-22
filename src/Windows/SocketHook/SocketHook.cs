using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using CoreHook;

namespace SocketHook
{
    public class SocketHook : IEntryPoint
    {
        IHook WSASendHook;
        IHook WSARecvHook;
        IHook RecvHook;
        IHook SendHook;
        IHook RecvfromHook;
        IHook SendtoHook;

        public SocketHook(IContext context) { }

        public unsafe void Run(IContext context)
        {
            // Create network function hooks
            WSASendHook = HookFactory.CreateHook<DWSASend>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "WSASend"), Detour_WsaSend);
            WSARecvHook = HookFactory.CreateHook<DWSARecv>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "WSARecv"), Detour_WSARecv);
            RecvHook = HookFactory.CreateHook<Drecv>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "recv"), Detour_recv);
            SendHook = HookFactory.CreateHook<Dsend>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "send"), Detour_send);
            RecvfromHook = HookFactory.CreateHook<Drecvfrom>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "recvfrom"), Detour_recvfrom);
            SendtoHook = HookFactory.CreateHook<Dsendto>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "sendto"), Detour_sendto);

            // Enable hooks for all threads
            WSASendHook.Enabled = true;
            WSARecvHook.Enabled = true;
            RecvHook.Enabled = true;
            SendHook.Enabled = true;
            RecvfromHook.Enabled = true;
            SendtoHook.Enabled = true;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate SocketError DWSASend(
            IntPtr socketHandle,
            WSABuffer* buffers,
            int bufferCount,
            out int bytesTransferred,
            SocketFlags socketFlags,
            NativeOverlapped* overlapped,
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
            ref WSABuffer buffer,
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

        internal unsafe delegate int Dsendto(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags,
            [In] byte[] socketAddress,
            [In] int socketAddressSize);

        internal unsafe delegate int Drecvfrom(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags,
            [Out] byte[] socketAddress,
            [In, Out] ref int socketAddressSize);

        private unsafe int Detour_recvfrom(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags,
            [Out] byte[] socketAddress,
            [In, Out] ref int socketAddressSize)
        {
            return Interop.Winsock.recvfrom(socketHandle, pinnedBuffer, len, socketFlags, socketAddress, ref socketAddressSize);
        }

        private unsafe int Detour_sendto(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags,
            [In] byte[] socketAddress,
            [In] int socketAddressSize)
        {
            return Interop.Winsock.sendto(socketHandle, pinnedBuffer, len, socketFlags, socketAddress, socketAddressSize);
        }
    }
}
