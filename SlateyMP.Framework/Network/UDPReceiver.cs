using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SlateyMP.Framework.Util;

namespace SlateyMP.Framework.Network
{
    public class UDPReceiver : ObjectPool<UDPReceiver> {
        private SocketAsyncEventArgs _ae;
        private Socket _receiveSocket;
        public Action<UDPReceiver, SocketAsyncEventArgs> OnReceive;
		public Socket ReceiveSocket { get { return _receiveSocket; } }

        public int DataSize { get; private set; }
        public int RemainingBytes { get { return DataSize - _rpos; } }
		public int CurrentReadPos { get { return _rpos; } }

        private const int Buffer_Size = 1024;
        private byte[] _Buffer = new byte[Buffer_Size];
        public byte[] Buffer {
            get { return _Buffer; }
        }

        private static Encoding _converter = new UTF8Encoding();

        public UDPReceiver() {
            _ae = new SocketAsyncEventArgs();
            _ae.SetBuffer(_Buffer, 0, Buffer_Size);
            _ae.RemoteEndPoint = new IPEndPoint(IPAddress.Any, Int32.Parse("0"));
            _ae.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCallback);
        }

        private void ReceiveCallback(object sender, SocketAsyncEventArgs e) {
            switch (e.LastOperation) {
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.ReceiveFrom:
					// Start another receiver while we process this one.
                    var streamer = UDPReceiver.CreateObject();
					streamer.OnReceive = OnReceive;
                    streamer.ReceiveFromAsync(_receiveSocket);

                    DataSize = e.BytesTransferred;
                    // Do receive action
                    OnReceive(this, e);
                    
                    // Cleanup and reclaim into pool
                    System.Array.Clear(_Buffer, 0, e.BytesTransferred);
                    _receiveSocket = null;
					_rpos = 0;
                    OnReceive = null;
                    ReclaimObject(this);
                    break;
            }
        }

        public void ReceiveFromAsync(Socket s) {
            _receiveSocket = s;
            _receiveSocket.ReceiveFromAsync(_ae);
        }

        private int _rpos = 0;

        #region read
		public SByte ReadInt8() {
			return (SByte)_Buffer[_rpos++];
		}

		public Int16 ReadInt16() {
			_rpos += 2;
			return BitConverter.ToInt16(_Buffer, _rpos - 2);
		}

		public Int32 ReadInt32() {
			_rpos += 4;
			return BitConverter.ToInt32(_Buffer, _rpos - 4);
		}

		public Int64 ReadInt64() {
			_rpos += 8;
			return BitConverter.ToInt64(_Buffer, _rpos - 8);
		}

		public Byte ReadUint8() {
			return _Buffer[_rpos++];
		}

		public UInt16 ReadUint16() {
			_rpos += 2;
			return BitConverter.ToUInt16(_Buffer, _rpos - 2);
		}

		public UInt32 ReadUint32() {
			_rpos += 4;
			return BitConverter.ToUInt32(_Buffer, _rpos - 4);
		}

		public UInt64 ReadUint64() {
			_rpos += 8;
			return BitConverter.ToUInt64(_Buffer, _rpos - 8);
		}

		public float ReadFloat() {
			_rpos += 4;
			return BitConverter.ToSingle(_Buffer, _rpos - 4);
		}

		public double ReadDouble() {
			_rpos += 8;
			return BitConverter.ToDouble(_Buffer, _rpos - 8);
		}

		public string ReadFixedString(int length) {
			int offset = _rpos;
			_rpos += length;
			return _converter.GetString(_Buffer, offset, length);
		}

		public string ReadString() {
			int offset = _rpos;
			while (_Buffer[_rpos++] != 0) {
			}

			return _converter.GetString(_Buffer, offset, _rpos - offset - 1);
		}

		public byte[] ReadFixedBlob(ref byte[] buf, int size) {
			Array.Copy(_Buffer, _rpos, buf, 0, size);
			_rpos += size;
			return buf;
		}

		public byte[] ReadBlob() {
			UInt32 size = ReadUint32();
			byte[] buf = new byte[size];

			Array.Copy(_Buffer, _rpos, buf, 0, size);
			_rpos += (int)size;
			return buf;
		}

		public void ReadSkip(UInt32 count) {
			_rpos += (int)count;
		}
		#endregion
    }
}
