using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SlateyMP.Framework.Util;

namespace SlateyMP.Framework.Network
{
    public class UDPTransmitter : ObjectPool<UDPTransmitter> {
        private const int Buffer_Size = 1024;
        private byte[] _Buffer = new byte[Buffer_Size];
        public byte[] Buffer {
            get { return _Buffer; }
        }

        private static Encoding _converter = new UTF8Encoding();

        public int SendTo(Socket s, EndPoint remoteEP) {
            var ret = s.SendTo(_Buffer, 0, _wpos, SocketFlags.None, remoteEP);
            System.Array.Clear(_Buffer, 0, ret);
			_wpos = 0;
            ReclaimObject(this);
            return ret;
        }

		private int _wpos = 0;

		#region write
		public void WriteInt8(SByte v) {
			_Buffer[_wpos++] = (Byte)v;
		}

		public void WriteInt16(Int16 v) {
			WriteInt8((SByte)(v & 0xff));
			WriteInt8((SByte)(v >> 8 & 0xff));
		}

		public void WriteInt32(Int32 v) {
			for (int i = 0; i < 4; i++)
				WriteInt8((SByte)(v >> i * 8 & 0xff));
		}

		public void WriteInt64(Int64 v) {
			byte[] getdata = BitConverter.GetBytes(v);
			for (int i = 0; i < getdata.Length; i++) {
				_Buffer[_wpos++] = getdata[i];
			}
		}

		public void WriteUint8(Byte v) {
			_Buffer[_wpos++] = v;
		}

		public void WriteUint16(UInt16 v) {
			WriteUint8((Byte)(v & 0xff));
			WriteUint8((Byte)(v >> 8 & 0xff));
		}

		public void WriteUint32(UInt32 v) {
			for (int i = 0; i < 4; i++)
				WriteUint8((Byte)(v >> i * 8 & 0xff));
		}

		public void WriteUint64(UInt64 v) {
			byte[] getdata = BitConverter.GetBytes(v);
			for (int i = 0; i < getdata.Length; i++) {
				_Buffer[_wpos++] = getdata[i];
			}
		}

		public void WriteFloat(float v) {
			byte[] getdata = BitConverter.GetBytes(v);
			for (int i = 0; i < getdata.Length; i++) {
				_Buffer[_wpos++] = getdata[i];
			}
		}

		public void WriteDouble(double v) {
			byte[] getdata = BitConverter.GetBytes(v);
			for (int i = 0; i < getdata.Length; i++) {
				_Buffer[_wpos++] = getdata[i];
			}
		}

		public void WriteFixedBlob(byte[] v) {
			for (UInt32 i = 0; i < v.Length; i++) {
				_Buffer[_wpos++] = v[i];
			}
		}

		public void WriteBlob(byte[] v) {
			if (v.Length + 4 > Buffer_Size - _wpos) {
				return;
			}

			WriteUint32((UInt32)v.Length);
            Array.Copy(v, 0, _Buffer, _wpos, v.Length);
            _wpos+=v.Length;
		}

		public void WriteString(string v) {
			if (v.Length + 1 > Buffer_Size - _wpos) {
				return;
			}

			byte[] getdata = Encoding.ASCII.GetBytes(v);
            Array.Copy(getdata, 0, _Buffer, _wpos, getdata.Length);
            _wpos+=getdata.Length;
			_Buffer[_wpos++] = 0;
		}

		public void WriteFixedString(string v) {
			if (v.Length > Buffer_Size - _wpos) {
				return;
			}

			byte[] getdata = Encoding.ASCII.GetBytes(v);
            Array.Copy(getdata, 0, _Buffer, _wpos, getdata.Length);
            _wpos+=getdata.Length;
		}

        public void WriteSkip(UInt32 count) {
			_wpos += (int)count;
		}
		#endregion
    }
}