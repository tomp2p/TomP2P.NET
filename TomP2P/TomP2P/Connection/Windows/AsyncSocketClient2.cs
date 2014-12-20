﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class AsyncSocketClient2
    {
        private readonly IPEndPoint _localEndPoint;
        // TODO binding for server-response

        private Socket _tcpClient;
        private Socket _udpClient;
        private IPEndPoint _remoteEp;

        public AsyncSocketClient2(IPEndPoint localEndPoint)
        {
            _localEndPoint = localEndPoint;
        }

        /// <summary>
        /// Resolves the server address and connects to it.
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync(string hostName, int hostPort)
        {
            // iterate through all addresses returned from DNS
            IPHostEntry hostInfo = await Dns.GetHostEntryAsync(hostName);
            foreach (IPAddress hostAddress in hostInfo.AddressList)
            {
                _remoteEp = new IPEndPoint(hostAddress, hostPort);
                _tcpClient = new Socket(hostAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    await _tcpClient.ConnectAsync(_remoteEp);
                }
                catch (Exception)
                {
                    // connection failed, close and try next address
                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                    }
                    continue;
                }
                // connection succeeded
                break;
            }

            if (_tcpClient == null || _remoteEp == null)
            {
                throw new Exception("Establishing connection failed.");
            }

            // use same remoteEp for UDP
            _udpClient = new Socket(_remoteEp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            Bind();
        }

        private void Bind()
        {
            // TODO UDP only?
            // since response from server is expected, bind UDP client to wildcard
            // bind
            try
            {
                if (_localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // set dual-mode (IPv4 & IPv6) for the socket listener
                    // see http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                    _udpClient.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    _udpClient.Bind(new IPEndPoint(IPAddress.IPv6Any, _localEndPoint.Port));
                }
                else
                {
                    _udpClient.Bind(_localEndPoint);
                }
            }
            catch (SocketException ex)
            {
                throw new Exception("Exception during client socket binding.", ex);
            }
        }

        public async Task DisconnectAsync()
        {
            // TCP only
            await _tcpClient.DisconnectAsync(false);
        }

        public async Task<int> SendTcpAsync(byte[] buffer)
        {
            return await _tcpClient.SendAsync(buffer, 0, buffer.Length, SocketFlags.None);
            // TODO TCP shutdown/close needed?
        }

        public async Task<int> ReceiveTcpAsync(byte[] buffer)
        {
            return await _tcpClient.ReceiveAsync(buffer, 0, buffer.Length, SocketFlags.None);
            // TODO loop as long as recvBytes == 0?
            // TODO shutdown/close needed?
        }

        public async Task<int> SendUdpAsync(byte[] buffer)
        {
            return await _udpClient.SendToAsync(buffer, 0, buffer.Length, SocketFlags.None, _remoteEp);
        }

        public async Task<int> ReceiveUdpAsync(byte[] buffer)
        {
            return await _udpClient.ReceiveFromAsync(buffer, 0, buffer.Length, SocketFlags.None, _remoteEp);
        }
    }
}
