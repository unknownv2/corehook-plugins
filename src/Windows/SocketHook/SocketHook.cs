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
        private IHook _wsaSendHook;
        private IHook _wsaRecvHook;
        private IHook _recvHook;
        private IHook _sendHook;
        private IHook _recvfromHook;
        private IHook _sendtoHook;

        /// <summary>
        /// Keep track of the number of times WSASend was called,
        /// regardless of return value.
        /// </summary>
        private long _wsaSendBufferCount;

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
            _wsaRecvHook = HookFactory.CreateHook<WSARecvDelegate>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "WSARecv"), Detour_WSARecv, this);
            _wsaSendHook = HookFactory.CreateHook<WSASendDelegate>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "WSASend"), Detour_WsaSend, this);
            _recvHook = HookFactory.CreateHook<RecvDelegate>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "recv"), Detour_recv, this);
            _sendHook = HookFactory.CreateHook<SendDelegaqte>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "send"), Detour_send, this);
            _recvfromHook = HookFactory.CreateHook<RecvfromDelegate>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "recvfrom"), Detour_recvfrom, this);
            _sendtoHook = HookFactory.CreateHook<SendtoDelegate>(LocalHook.GetProcAddress(Interop.Libraries.Ws2_32, "sendto"), Detour_sendto, this);

            // Enable hooks for all threads
            _wsaSendHook.Enabled = true;
            _wsaRecvHook.Enabled = true;
            _recvHook.Enabled = true;
            _sendHook.Enabled = true;
            _recvfromHook.Enabled = true;
            _sendtoHook.Enabled = true;

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
                    if (_wsaSendBufferCount > 0)
                    {
                        Console.WriteLine($"Sent data using WSASend {_wsaSendBufferCount} time(s).");
                    }
                }
            }
            catch
            {

            }
        }
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private unsafe delegate SocketError WSASendDelegate(
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
                This._wsaSendBufferCount++;
            }
            return Interop.Winsock.WSASend(socketHandle, buffers, bufferCount, out bytesTransferred, socketFlags, overlapped, completionRoutine);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private unsafe delegate SocketError WSARecvDelegate(
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

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        internal unsafe delegate int RecvDelegate(
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

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        internal unsafe delegate int SendDelegaqte(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags);

        private unsafe int Detour_send(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags)
        {
            return Interop.Winsock.send(socketHandle, pinnedBuffer, len, socketFlags);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        internal unsafe delegate int RecvfromDelegate(
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

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        internal unsafe delegate int SendtoDelegate(
            [In] IntPtr socketHandle,
            [In] byte* pinnedBuffer,
            [In] int len,
            [In] SocketFlags socketFlags,
            [In] byte[] socketAddress,
            [In] int socketAddressSize);

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
