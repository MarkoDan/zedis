using System;

namespace ZedisServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting ZedisServer on port 6555...");
            var server = new Zedis.ZedisServer(port: 6555);
            server.Start();
        }
    }
}
