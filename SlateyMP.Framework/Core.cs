using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using SlateyMP.Framework.Network;

namespace SlateyMP.Framework {
    public static class Core {
        private static bool _Initialized;
		private static Thread _Thread;

        public static Version Version { get { return Assembly.GetEntryAssembly().GetName().Version; } }

        public static void Initialize()
        {
            _Thread = Thread.CurrentThread;
            
            if (_Thread != null) {
				_Thread.Name = "Core Thread";
            }

            _Initialized = true;
        }

        public static void CheckInitialized()
        {
            if (!_Initialized) throw new Exception("Core has not been initialized yet");
        }

        public static void OutputLogo()
        {
            CheckInitialized();

            Version ver = Core.Version;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("");
            Console.WriteLine("      ██████  ██▓    ▄▄▄     ▄▄▄█████▓▓█████▓██   ██▓ ███▄ ▄███▓ ██▓███  ");
            Console.WriteLine("    ▒██    ▒ ▓██▒   ▒████▄   ▓  ██▒ ▓▒▓█   ▀ ▒██  ██▒▓██▒▀█▀ ██▒▓██░  ██▒");
            Console.WriteLine("    ░ ▓██▄   ▒██░   ▒██  ▀█▄ ▒ ▓██░ ▒░▒███    ▒██ ██░▓██    ▓██░▓██░ ██▓▒");
            Console.WriteLine("      ▒   ██▒▒██░   ░██▄▄▄▄██░ ▓██▓ ░ ▒▓█  ▄  ░ ▐██▓░▒██    ▒██ ▒██▄█▓▒ ▒");
            Console.WriteLine("    ▒██████▒▒░██████▒▓█   ▓██▒ ▒██▒ ░ ░▒████▒ ░ ██▒▓░▒██▒   ░██▒▒██▒ ░  ░");
            Console.WriteLine("    ▒ ▒▓▒ ▒ ░░ ▒░▓  ░▒▒   ▓▒█░ ▒ ░░   ░░ ▒░ ░  ██▒▒▒ ░ ▒░   ░  ░▒▓▒░ ░  ░");
            Console.WriteLine("    ░ ░▒  ░ ░░ ░ ▒  ░ ▒   ▒▒ ░   ░     ░ ░  ░▓██ ░▒░ ░  ░      ░░▒ ░     ");
            Console.WriteLine("    ░  ░  ░    ░ ░    ░   ▒    ░         ░   ▒ ▒ ░░  ░      ░   ░░       ");
            Console.WriteLine("          ░      ░  ░     ░  ░           ░  ░░ ░            ░            ");
            Console.WriteLine("                                             ░ ░                         ");
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(((AssemblyTitleAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title);
            Console.WriteLine(" version {0}.{1}.{2}.{3}", ver.Major, ver.Minor, ver.Build, ver.Revision);
            var DotNetFrameworkName = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            Console.WriteLine("Engine: Running on {0} Version {1}.{2}.{3}", DotNetFrameworkName, Environment.Version.Major, Environment.Version.Minor, Environment.Version.Build);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Title = String.Format("{0} v{1}", ((AssemblyTitleAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title, Version);
        }

        public static void StartUDPReceiver(IPAddress listenAddress, int listenPort, Action<UDPReceiver, SocketAsyncEventArgs> OnReceive) {
            CheckInitialized();

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            listenSocket.Bind(new IPEndPoint(listenAddress, listenPort));

            var receiver = UDPReceiver.CreateObject();
            receiver.OnReceive = OnReceive;
            receiver.ReceiveFromAsync(listenSocket);
        }

        public static void ServerMainLoop() {
            CheckInitialized();
        }
    }
}