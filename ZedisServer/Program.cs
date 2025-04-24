using System;

namespace ZedisServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var server = new Zedis.ZedisServer();
            server.Start();
        
            
            
        }
    }
}
