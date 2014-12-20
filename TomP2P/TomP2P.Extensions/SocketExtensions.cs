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
                var t = (TaskCompletionSource<int>)ar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
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

        public static Task<int> SendToAsync(this Socket socket, byte[] buffer, int offset, int size,
            SocketFlags socketFlags, EndPoint remoteEp)
        {
            var tcs = new TaskCompletionSource<int>(socket);

            socket.BeginSendTo(buffer, offset, size, socketFlags, remoteEp, ar =>
            {
                var t = (TaskCompletionSource<int>)ar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try
                {
                    t.TrySetResult(s.EndSendTo(ar));
                }
                catch (Exception ex)
                {
                    t.TrySetException(ex);
                }
            }, tcs);
            return tcs.Task;
        }

        public static Task<ReceiveFromOutput> ReceiveFromAsync(this Socket socket, byte[] buffer, int offset, int size,
            SocketFlags socketFlags, EndPoint remoteEp)
        {
            var input = new ReceiveFromInput(socket, remoteEp);
            var tcs = new TaskCompletionSource<ReceiveFromOutput>(input);

            socket.BeginReceiveFrom(buffer, offset, size, socketFlags, ref remoteEp, ar =>
            {
                var t = (TaskCompletionSource<ReceiveFromOutput>)ar.AsyncState;
                var input2 = (ReceiveFromInput)t.Task.AsyncState;
                Socket s = input2.Socket;
                EndPoint ep = input2.RemoteEp;
                try
                {
                    int recv = s.EndReceiveFrom(ar, ref ep);

                    var output = new ReceiveFromOutput(recv, ep); // TODO ref param needed?
                    t.TrySetResult(output);
                }
                catch (Exception ex)
                {
                    t.TrySetException(ex);
                }
            }, tcs);
            return tcs.Task;
        }

        private struct ReceiveFromInput
        {
            private readonly Socket _socket;
            private readonly EndPoint _remoteEp;

            public ReceiveFromInput(Socket socket, EndPoint remoteEp)
            {
                _socket = socket;
                _remoteEp = remoteEp;
            }

            public Socket Socket
            {
                get { return _socket; }
            }

            public EndPoint RemoteEp
            {
                get { return _remoteEp; }
            }
        }

        public struct ReceiveFromOutput
        {
            private readonly int _bytesReceived;
            private readonly EndPoint _remoteEp;

            public ReceiveFromOutput(int bytesReceived, EndPoint remoteEp)
            {
                _bytesReceived = bytesReceived;
                _remoteEp = remoteEp;
            }

            public int BytesReceived
            {
                get { return _bytesReceived; }
            }

            public EndPoint RemoteEp
            {
                get { return _remoteEp; }
            }
        }
    }
}
