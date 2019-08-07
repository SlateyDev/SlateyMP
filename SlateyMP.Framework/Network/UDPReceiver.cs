using System;
using System.Net;
using System.Net.Sockets;
using SlateyMP.Framework.Util;

namespace SlateyMP.Framework.Network
{
    public class UDPReceiver : ObjectPool<UDPReceiver> {
        private SocketAsyncEventArgs _ae;
        private Socket _receiveSocket;
        public Action<UDPReceiver, SocketAsyncEventArgs> OnReceive;

        private const int Buffer_Size = 1024;
        private byte[] _Buffer = new byte[Buffer_Size];
        public byte[] Buffer {
            get { return _Buffer; }
        }

        public UDPReceiver() {
            _ae = new SocketAsyncEventArgs();
            _ae.SetBuffer(_Buffer, 0, Buffer_Size);
            _ae.RemoteEndPoint = new IPEndPoint(IPAddress.Any, Int32.Parse("0"));
            _ae.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCallback);
        }

        private void ReceiveCallback(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.ReceiveFrom:
					// Start another receiver while we process this one.
                    var streamer = UDPReceiver.CreateObject();
					streamer.OnReceive = OnReceive;
                    streamer.ReceiveFromAsync(_receiveSocket);

                    // Do receive action
                    OnReceive(this, e);
                    
                    // Cleanup and reclaim into pool
                    System.Array.Clear(_Buffer, 0, e.BytesTransferred);
                    _receiveSocket = null;
                    OnReceive = null;
                    ReclaimObject(this);
                    break;
            }
        }

        public void ReceiveFromAsync(Socket s) {
            _receiveSocket = s;
            _receiveSocket.ReceiveFromAsync(_ae);
        }
    }
}
