using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using SlateyMP.Framework;
using SlateyMP.Framework.Network;
using SlateyMP.Framework.Util;

namespace SlateyMP.Server.Login
{
    class Program
    {
        public static MySqlConnection db;

        private static string[] WS_STATUS = { "OFFLINE", "STARTING", "ONLINE", "STOPPING" };

		private static object ConvertDBNull(object value, string newval)
		{
			if (Convert.IsDBNull(value))
			{
				return newval;
			}
			else
			{
				return value;
			}
		}

        private static Type MySqlTypeToType(string t) {
            switch(t) {
                case "INT":
                    return typeof(int);
                case "VARCHAR":
                    return typeof(string);
                default:
                    Console.WriteLine(t);
                    return typeof(object);
            }
        }

        public static void MySqlToDataTable(string sql, MySqlConnection conn, DataTable dt) {
            var cmd = new MySqlCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            if(reader.Read()) {
                for(var colIndex = 0; colIndex < reader.FieldCount; colIndex++) {
                    dt.Columns.Add(reader.GetName(colIndex), MySqlTypeToType(reader.GetDataTypeName(colIndex)));
                }
                do {
                    var newRow = dt.NewRow();
                    for(var colIndex = 0; colIndex < reader.FieldCount; colIndex++) {
                        newRow[colIndex] = reader.GetValue(colIndex);
                    }
                    dt.Rows.Add(newRow);
                } while(reader.Read());
            }
            reader.Close();
        }

		private static void WorldServerStatusReport()
		{
			DataTable result1 = new DataTable();
			DataTable result2 = new DataTable();

            MySqlToDataTable("SELECT * FROM realm", db, result1);
            MySqlToDataTable("SELECT * FROM realm WHERE state = 2", db, result2);

			Console.WriteLine();
			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] Known Realm Servers are {1}", DateTime.Now, result1.Rows.Count);
			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] Online Realm Servers are {1}", DateTime.Now, result2.Rows.Count);
			Console.ForegroundColor = ConsoleColor.DarkGreen;
			foreach (DataRow Row in result1.Rows)
			{
				Console.WriteLine("    {3} [{1}] at {0}:{2}", ConvertDBNull(Row["address"], "NULL"), ((string)Row["name"]).PadRight(20), ConvertDBNull(Row["port"], "NULL"), WS_STATUS[(int)Row["state"]].PadRight(10));
			}
			Console.ForegroundColor = ConsoleColor.Gray;
		}
        
        static void Main(string[] args)
        {
            Core.Initialize();
            Core.OutputLogo();
            Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] Login Server Starting...", DateTime.Now);

            var configBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configRoot = configBuilder.Build();
            var connectionString = configRoot.GetConnectionString("DB");
            db = new MySqlConnection(connectionString);

            Server.RegisterOpcodes();

            try {
                db.Open();
                try {
                    WorldServerStatusReport();
                    Core.StartUDPReceiver(IPAddress.Any, Convert.ToInt32("44000"), OnReceive);
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
            Console.WriteLine("Received data");

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