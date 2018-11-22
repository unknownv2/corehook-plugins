using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

        /// <summary>
        /// Keep track of the number of times WSASend was called,
        /// regardless of return value.
        /// </summary>
        private long WsaSendBufferCount;

        public SocketHook(IContext context) { }

        /// <summary>
        /// First method called during plugin load.
        /// Can be used to create hooks and initialize
        /// variables.
        /// </summary>
        /// <param name="context">Contains any standard information required for each plugin.</param>
        public unsafe void Run(IContext context)
        {
            // Create network function hooks
            WSARecvHook = HookFactory.CreateHook<DWSARecv>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "WSARecv"), Detour_WSARecv, this);
            WSASendHook = HookFactory.CreateHook<DWSASend>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "WSASend"), Detour_WsaSend, this);
            RecvHook = HookFactory.CreateHook<Drecv>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "recv"), Detour_recv, this);
            SendHook = HookFactory.CreateHook<Dsend>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "send"), Detour_send, this);
            RecvfromHook = HookFactory.CreateHook<Drecvfrom>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "recvfrom"), Detour_recvfrom, this);
            SendtoHook = HookFactory.CreateHook<Dsendto>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "sendto"), Detour_sendto, this);

            // Enable hooks for all threads
            WSASendHook.Enabled = true;
            WSARecvHook.Enabled = true;
            RecvHook.Enabled = true;
            SendHook.Enabled = true;
            RecvfromHook.Enabled = true;
            SendtoHook.Enabled = true;

            ProcessPackets().GetAwaiter().GetResult();
        }

        private async Task ProcessPackets()
        {
            // Ensure we are running in a new thread
            await Task.Yield();
            try
            {
                while (true)
                {
                    Thread.Sleep(500);
                    if (WsaSendBufferCount > 0)
                    {
                        Console.WriteLine($"Sent data using WSASend {WsaSendBufferCount} time(s).");
                    }
                }
            }
            catch
            {

            }
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
            SocketHook This = (SocketHook)HookRuntimeInfo.Callback;
            if (This != null)
            {
                // Increment WSASend send count
                This.WsaSendBufferCount++;
            }
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
