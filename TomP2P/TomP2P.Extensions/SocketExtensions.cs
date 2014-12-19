using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    // inspired by http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx

    /// <summary>
    /// Socket extensions that allow to use the new TPL (Task Parallel Library) programming model 
    /// instead of the APM (Asynchronous Programming Model) pattern.
    /// </summary>
    public static class SocketExtensions
    {
        public static Task ConnectAsync(this Socket socket, IPEndPoint endPoint)
        {
            var tcs = new TaskCompletionSource<object>(socket);

            socket.BeginConnect(endPoint, ar =>
            {
                var t = (TaskCompletionSource<object>)ar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try
                {
                    s.EndConnect(ar);
                    t.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    t.TrySetException(ex);
                }
            }, tcs);
            return tcs.Task;
        }

        public static Task DisconnectAsync(this Socket socket, bool reuseSocket)
        {
            var tcs = new TaskCompletionSource<object>(socket);

            socket.BeginDisconnect(reuseSocket, ar =>
            {
                var t = (TaskCompletionSource<object>)ar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try
                {
                    s.EndDisconnect(ar);
                    t.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    t.TrySetException(ex);
                }
            }, tcs);
            return tcs.Task;
        }

        public static Task<Socket> AcceptAsync(this Socket socket)
        {
            var tcs = new TaskCompletionSource<Socket>(socket);

            socket.BeginAccept(ar =>
            {
                var t = (TaskCompletionSource<Socket>)ar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try
                {
                    t.TrySetResult(s.EndAccept(ar));
                }
                catch (Exception ex)
                {
                    t.TrySetException(ex);
                }
            }, tcs);
            return tcs.Task;
        }

        public static Task<int> SendAsync(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(socket);

            socket.BeginSend(buffer, offset, size, socketFlags, ar =>
            {
                var t = (TaskCompletionSource<int>)ar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try
                {
                    t.TrySetResult(s.EndSend(ar));
                }
                catch (Exception ex)
                {
                    t.TrySetException(ex);
                }
            }, tcs);
            return tcs.Task;
        }

        public static Task<int> ReceiveAsync(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(socket);

            socket.BeginReceive(buffer, offset, size, socketFlags, ar =>
            {
                var t = (TaskCompletionSource<int>) ar.AsyncState;
                var s = (Socket) t.Task.AsyncState;
                try
                {
                    t.TrySetResult(s.EndReceive(ar));
                }
                catch (Exception ex)
                {
                    t.TrySetException(ex);
                }
            }, tcs);
            return tcs.Task;
        }
    }
}
