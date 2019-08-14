using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using SlateyMP.Framework;
using SlateyMP.Framework.Network;
using SlateyMP.Framework.Util;

namespace SlateyMP.Server.Realm
{
    class Program
    {
        public static MySqlConnection db;

        public static int server_port = 44001;

        static void Main(string[] args)
        {
            Core.Initialize();
            Core.OutputLogo();
            Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] Realm Server Starting...", DateTime.Now);

            var connectionString = Environment.GetEnvironmentVariable("REALM_SERVER_DB");
            var realm_server_address = IPAddress.Parse(Environment.GetEnvironmentVariable("REALM_SERVER_ADDRESS"));
            var realm_server_port = Convert.ToInt32(Environment.GetEnvironmentVariable("REALM_SERVER_PORT"));
            db = new MySqlConnection(connectionString);

            Server.RegisterOpcodes();

            try {
                db.Open();
                try {
                    Core.StartUDPReceiver(realm_server_address, realm_server_port, OnReceive);
                    Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] Server listening on {1}:{2}", DateTime.Now, realm_server_address, realm_server_port);
                    Core.ServerMainLoop();
                }                
                finally {
                    db.Close();
                }
            }
			catch(Exception ex) {
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine("Error!. Unhandled exception occurred:\n" + ex.ToString());
				Console.ForegroundColor = ConsoleColor.Gray;

				Debug.WriteLine(ex.ToString());
			}
        }

        private static void OnReceive(UDPReceiver receiver, SocketAsyncEventArgs e) {
            var remoteEP = (IPEndPoint)e.RemoteEndPoint;

            if(e.BytesTransferred < 2) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Invalid message received", DateTime.Now, remoteEP.Address, remoteEP.Port);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            UInt16 packetID = receiver.ReadUint16();
            var handler = ReceiveOpcodes.GetOpcodeHandler(packetID);
            if (handler == null) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Unknown Opcode {3}", DateTime.Now, remoteEP.Address, remoteEP.Port, packetID);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            var session = Session.GetInstance(remoteEP, s => { s.Data = new ClientInfo(); });
            handler.OnReceive(session, receiver);
        }
    }
}
