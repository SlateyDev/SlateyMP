using System.Net;
using System.Net.Sockets;

namespace SlateyMP.Client {
    public static class ClientManager {
        public static void Run() {
            Socket transportSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			transportSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));
            var remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 44000);

            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            transportSocket.SendTo(data, 0, 10, SocketFlags.None, remoteEP);
        }
    }
}