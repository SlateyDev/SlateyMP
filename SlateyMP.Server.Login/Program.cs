using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

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
            var configBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configRoot = configBuilder.Build();
            var connectionString = configRoot.GetConnectionString("DB");
            db = new MySqlConnection(connectionString);

            try {
                db.Open();
                try {
                    WorldServerStatusReport();
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
    }
}