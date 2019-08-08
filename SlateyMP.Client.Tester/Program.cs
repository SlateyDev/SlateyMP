using System;
using System.Threading;

namespace SlateyMP.Client.Tester
{
    class Program
    {
        private static AutoResetEvent _UpdateSignal = new AutoResetEvent(false);
        
        public static void TriggerUpdate() { _UpdateSignal.Set(); }
        
        static void Main(string[] args)
        {
            Console.WriteLine("SlateyMP Client Tester");
            ClientManager.Run();

            _UpdateSignal.WaitOne();
        }
    }
}